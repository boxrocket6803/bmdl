using System.Numerics;

public class Texture<T> : Texture {
	public int Channels {get; set;}
	public T[] Pixels {get; set;}

	public T[] Sample(Vector2 uv) {
		var x = (int)(Math.Clamp(uv.X, 0, 1) * Width);
		var y = (int)(Math.Clamp(uv.Y, 0, 1) * Height);
		var i = y * Width + x;
		i *= Channels;
		var r = new T[Channels];
		for (var c = 0; c < Channels; c++)
			r[c] = Pixels[i+c];
		return r;
	}

	public static Texture<T> Load(Stream s, int channels) {
		if (s is null)
			return null;
		var r = new BinaryReader(s);
		var t = new Texture<T>() {Channels = channels};
		t.ReadHeader(r);
		t.Pixels = t.ReadData<T>(r, channels);
		return t;
	}
}

public partial class Texture {
	public enum Format {
		rgba8888 = 0,
		rgb888 = 1,
		g8 = 2,
		g32 = 3,
	}

	public Format Type;
	public uint Width;
	public uint Height;
	public Vector2 Size => new(Width, Height);
	public uint Depth;

	private static int GetChannelCount(Format f) {
		switch (f) {
			case Format.rgba8888:
				return 4;
			case Format.rgb888:
				return 3;
		}
		return 1;
	}
	protected void ReadHeader(BinaryReader r) {
		r.ReadBytes(4); //btex
		Type = (Format)r.ReadByte();
		Width = r.ReadUInt16();
		Height = r.ReadUInt16();
		Depth = r.ReadUInt16();
	}
	protected T[] ReadData<T>(BinaryReader r, int channels) {
		var d = new T[Width * Height * Depth * channels];
		var w = 0;
		var c = new object[GetChannelCount(Type)];
		var run = 0;
		for (var z = 0; z < Depth; z++) {
			for (var y = 0; y < Height; y++) {
				for (var x = 0; x < Width; x++) {
					if (run <= 0) {
						for (var ch = 0; ch < c.Length; ch++)
							c[ch] = Type == Format.g32 ? r.ReadUInt32() : r.ReadByte();
						run = r.ReadByte() + 1;
					}
					for (var ch = 0; ch < channels; ch++)
						d[w++] = (T)(ch < c.Length ? c[ch] : (ch < 4 ? 0 : 1));
					run--;
				}
			}
		}
		return d;
	}
}
