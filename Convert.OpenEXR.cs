using TinyEXR;

[Convert.Association(["exr"])]
public class OpenEXR : Convert.Source {
	private float[] Data;

	public override void Read(string path) {
		Exr.LoadEXR(path, out Data, out var w, out var h);
		Width = (ushort)w;
		Height = (ushort)h;
	}
	public override byte[] LoadByte(int channels) {
		var load = new byte[Width * Height * channels];
		var loadw = 0;
		var ch = 0;
		foreach (var pixel in Data) {
			if (ch < channels)
				load[loadw++] = (byte)(pixel * byte.MaxValue);
			ch++;
			if (ch > 3)
				ch = 0;
		}
		return load;
	}
	public override uint[] LoadUint(int channels) {
		var load = new uint[Width * Height * channels];
		var loadw = 0;
		var ch = 0;
		foreach (var pixel in Data) {
			if (ch < channels)
				load[loadw++] = (uint)(pixel * uint.MaxValue);
			ch++;
			if (ch > 3)
				ch = 0;
		}
		return load;
	}
}