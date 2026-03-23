using System.Numerics;

public partial class Model {
	[Flags] public enum Flags {
		Skeleton	= 1 << 0,
		Color		= 1 << 1,
		Morphs		= 1 << 2,
		Weights		= 1 << 3,
		BigIndicies = 1 << 4,
	}
	public struct Bone {
		public string Name {get; set;}
		public int Parent {get; set;}
		public Transform Bind {get; set;}
	}
	public struct Mesh() {
		public struct Vertex() {
			public Vector3 Position {get; set;}
			public Vector3 Normal {get; set;}
			public Vector2 TexCoord0 {get; set;}
			public int Color {get; set;} = int.MaxValue;
			public ushort[] Bones {get; set;} = [];
			public float[] Weights {get; set;} = [];
		}
		public struct Morph() {
			public uint[] Vertices {get; set;} = [];
			public Vector3[] Offset {get; set;} = [];
		}
		public string Name {get; set;}
		public string Material {get; set;}
		public uint[] Indices {get; set;} = [];
		public Vertex[] Vertices {get; set;} = [];
		public Morph[] Morphs {get; set;} = [];
	}
	public Bone[] Skeleton {get; set;} = [];
	public Mesh[] Meshes {get; set;} = [];

	public static Model Load(Stream f) {
		if (f is null)
			return null;
		return Load(new BinaryReader(f));
	}
	public static Model Load(BinaryReader f) {
		Model m = new();
		f.ReadBytes(4); //bmdl
		var flags = f.ReadByte();
		if ((flags & (byte)Flags.Skeleton) != 0) {
			//TODO load skeleton
		}
		m.Meshes = new Mesh[f.ReadByte()];
		for (var i = 0; i < m.Meshes.Length; i++) {
			Mesh mesh = default;
			mesh.Name = f.ReadString();
			mesh.Material = f.ReadString();
			mesh.Indices = new uint[f.ReadInt32()];
			for (var j = 0; j < mesh.Indices.Length; j++) {
				if ((flags & (byte)Flags.BigIndicies) != 0)
					mesh.Indices[j] = f.ReadUInt32();
				else
					mesh.Indices[j] = f.ReadUInt16();
			}
			mesh.Vertices = new Mesh.Vertex[f.ReadInt32()];
			for (var j = 0; j < mesh.Vertices.Length; j++) {
				Mesh.Vertex vertex = default;
				vertex.Position = new((float)f.ReadHalf(), (float)f.ReadHalf(), (float)f.ReadHalf());
				vertex.Normal = new((float)f.ReadHalf(), (float)f.ReadHalf(), (float)f.ReadHalf());
				var l = vertex.Normal.Length();
				if (l > 0) vertex.Normal /= l;
				vertex.TexCoord0 = new((float)f.ReadHalf(), (float)f.ReadHalf());
				if ((flags & (byte)Flags.Color) != 0)
					vertex.Color = f.ReadInt32();
				if ((flags & (byte)Flags.Weights) != 0) {
					vertex.Bones = new ushort[f.ReadByte()];
					for (var k = 0; k < vertex.Bones.Length; k++)
						vertex.Bones[k] = f.ReadUInt16();
					vertex.Weights = new float[vertex.Bones.Length];
					for (var k = 0; k < vertex.Weights.Length; k++)
						vertex.Weights[k] = (float)f.ReadHalf();
				}
			}
			if ((flags & (byte)Flags.Morphs) != 0) {
				//TODO write morphs
			}
			m.Meshes[i] = mesh;
		}
		return m;
	}
}