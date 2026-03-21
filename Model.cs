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
}