namespace bmdl;

using System.Reflection;

public class Convert {
	[AttributeUsage(AttributeTargets.Class)]
	public class AssociationAttribute(string[] extensions) : Attribute {
		public HashSet<string> Types = [.. extensions];
	}
	public class Source {
		public virtual Model Read(string path) {return null;}
	}

	public static bool Supports(string file) {
		foreach (var type in Assembly.GetAssembly(typeof(Source)).GetTypes()) {
			if (!type.IsAssignableTo(typeof(Source)))
				continue;
			var assoc = type.GetCustomAttribute<AssociationAttribute>();
			if (assoc is null)
				continue;
			if (!assoc.Types.Contains(file.Split('.').Last()))
				continue;
			return true;
		}
		return false;
	}
	public static void Main(string[] args) {
		List<string> argacc = [.. args];
		Console.WriteLine("-- bmdl converter utility --");
		if (argacc.Count == 0) {
			Console.WriteLine("cmd args");
			Console.WriteLine(" <input file>");
		}
		while (!File.Exists(argacc.ElementAtOrDefault(0))) {
			if (argacc.Count > 0) {
				Console.WriteLine($"{argacc[0]} is not a valid file");
				argacc.RemoveAt(0);
			}
			Console.Write("input file: ");
			argacc.Insert(0, Console.ReadLine().Trim());
		}
		Write(argacc[0]);
	}
	public static void Write(string input) {
		Source inst = null;
		foreach (var type in Assembly.GetAssembly(typeof(Source)).GetTypes()) {
			if (!type.IsAssignableTo(typeof(Source)))
				continue;
			var assoc = type.GetCustomAttribute<AssociationAttribute>();
			if (assoc is null)
				continue;
			if (!assoc.Types.Contains(input.Split('.').Last()))
				continue;
			Console.WriteLine($"using {type} converter");
			inst = (Source)type.GetConstructor([]).Invoke([]);
			break;
		}
		if (inst is null) {
			Console.WriteLine($"could not find converter source for '{input.Split('.').Last()}'");
			return;
		}
		if (File.Exists($"{input.Split('.').First()}.bmdl"))
			File.Delete($"{input.Split('.').First()}.bmdl");
		using var f = new BinaryWriter(File.OpenWrite($"{input.Split('.').First()}.bmdl"));
		Write(f, inst.Read(input));
	}

	private static void Write(BinaryWriter f, Model m) {
		f.Write("bmdl".ToArray());
		byte flags = 0;
		if (m.Skeleton.Length > 0)
			flags |= (byte)Model.Flags.Skeleton;
		foreach (var mesh in m.Meshes) {
			if ((flags & (byte)Model.Flags.BigIndicies) == 0 && mesh.Indices.Any((i) => i > ushort.MaxValue))
				flags |= (byte)Model.Flags.BigIndicies;
			if ((flags & (byte)Model.Flags.Color) == 0 && mesh.Vertices.Any((v) => v.Color != int.MaxValue))
				flags |= (byte)Model.Flags.Color;
			if ((flags & (byte)Model.Flags.Weights) == 0 && (flags & (byte)Model.Flags.Skeleton) != 0) {
				foreach (var vertex in mesh.Vertices) {
					if (vertex.Bones.All((b) => b == 0) && vertex.Weights.All((w) => w == 0))
						continue;
					flags |= (byte)Model.Flags.Weights;
					break;
				}
			}
		}
		f.Write(flags);
		if ((flags & (byte)Model.Flags.Skeleton) != 0) {
			//TODO write skeleton
		}
		f.Write((byte)m.Meshes.Length);
		foreach (var mesh in m.Meshes) {
			f.Write(mesh.Name);
			f.Write(mesh.Material);
			f.Write(mesh.Indices.Length);
			foreach (var index in mesh.Indices) {
				if ((flags & (byte)Model.Flags.BigIndicies) != 0)
					f.Write(index);
				else
					f.Write((ushort)index);
			}
			f.Write(mesh.Vertices.Length);
			foreach (var vertex in mesh.Vertices) {
				f.Write((Half)vertex.Position.X);
				f.Write((Half)vertex.Position.Y);
				f.Write((Half)vertex.Position.Z);
				f.Write((Half)vertex.Normal.X);
				f.Write((Half)vertex.Normal.Y);
				f.Write((Half)vertex.Normal.Z);
				f.Write((Half)vertex.TexCoord0.X);
				f.Write((Half)vertex.TexCoord0.Y);
				if ((flags & (byte)Model.Flags.Color) != 0)
					f.Write(vertex.Color);
				if ((flags & (byte)Model.Flags.Weights) != 0) {
					f.Write((byte)vertex.Bones.Length);
					foreach (var bone in vertex.Bones)
						f.Write(bone);
					foreach (var weight in vertex.Weights)
						f.Write((Half)weight);
				}
			}
			if ((flags & (byte)Model.Flags.Morphs) != 0) {
				//TODO write morphs
			}
		}
	}
}