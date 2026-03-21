namespace bmdl;

using Datamodel;
using System.Collections;
using System.Numerics;

[Convert.Association(["dmx"])]
public class ValveDMX : Convert.Source {
	public override Model Read(string path) {
		Model m = new();
		var dmx = Datamodel.Load(path);
		Load(dmx.Root, m);
		return m;
	}

	public static float Remap(float value, float oldLow, float oldHigh) {
		if (MathF.Abs(oldHigh - oldLow) < 0.0001f)
			return 0;
		return (value - oldLow) / (oldHigh - oldLow);
	}

	private static void Load(Element dmx, Model m) {
		//TODO skeleton
		var model = dmx.Get<Element>("model");
		var meshes = new List<Model.Mesh>();
		foreach (var dag in model.Get<ElementArray>("children")) {
			var dagmesh = dag.Get<Element>("shape");
			foreach (var faceset in dagmesh.Get<ElementArray>("faceSets")) {
				var mesh = new Model.Mesh() {
					Name = dagmesh.Name,
					Material = faceset.Get<Element>("material").Name
				};
				var indices = new List<uint>();
				var indexmap = new Dictionary<int, uint>();
				var count = 0;
				uint vertexcount = 0;
				foreach (var index in faceset.Get<IntArray>("faces")) {
					if (index == -1) {
						count = 0;
						continue;
					}
					if (count++ >= 3) {
						Console.WriteLine("ERROR: bmdl does not support quads or ngons, triangulate your dmx");
						Console.ReadKey();
						return;
					}
					if (!indexmap.TryGetValue(index, out var next))
						next = indexmap[index] = (uint)indices.Count;
					indices.Add(next);
					vertexcount = Math.Max(vertexcount, next);
				}
				mesh.Indices = [..indices];
				mesh.Vertices = new Model.Mesh.Vertex[vertexcount + 1];
				Array.Fill(mesh.Vertices, new());
				var vertexdata = dagmesh.Get<Element>("bindState");
				foreach (var part in vertexdata.Get<StringArray>("vertexFormat")) {
					IList values = null;
					if (part == "position$0" || part == "normal$0")
						values = vertexdata.Get<Vector3Array>(part).ToList();
					else if (part == "texcoord$0")
						values = vertexdata.Get<Vector2Array>(part).ToList();
					else {
						Console.WriteLine($"WARNING: unsupported vertex data {part}");
						continue;
					}
					var dataindices = vertexdata.Get<IntArray>($"{part}Indices");
					for (int i = 0; i < dataindices.Count; i++) {
						if (!indexmap.TryGetValue(i, out var index))
							continue;
						var vertex = mesh.Vertices[index];
						if (part == "position$0")
							vertex.Position = (Vector3)values[dataindices[i]];
						if (part == "normal$0")
							vertex.Normal = (Vector3)values[dataindices[i]];
						if (part == "texcoord$0")
							vertex.TexCoord0 = (Vector2)values[dataindices[i]];
						mesh.Vertices[index] = vertex;
					}
				}
				indexmap.Clear();
				var vertices = mesh.Vertices.ToList();
				for (int i = 0; i < vertices.Count; i++) {
					var vertex = vertices[i];
					var hc = new HashCode();
					hc.Add(vertex.Position);
					hc.Add(vertex.Normal);
					hc.Add(vertex.TexCoord0);
					foreach (var bone in vertex.Bones)
						hc.Add(bone);
					foreach (var weight in vertex.Weights)
						hc.Add(weight);
					var hash = hc.ToHashCode();
					if (indexmap.TryGetValue(hash, out var prev)) {
						for (int j = 0; j < mesh.Indices.Length; j++) {
							if (mesh.Indices[j] == i)
								mesh.Indices[j] = prev;
							if (mesh.Indices[j] > i)
								mesh.Indices[j]--;
						}
						vertices.RemoveAt(i--);
					} else
						indexmap[hash] = (uint)i;
				}
				mesh.Vertices = [..vertices];
				meshes.Add(mesh);
			}
		}
		m.Meshes = [..meshes];
	}
}