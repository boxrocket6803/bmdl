using System.Reflection;

public class Convert {
	[AttributeUsage(AttributeTargets.Class)]
	public class AssociationAttribute(string[] extensions) : Attribute {
		public HashSet<string> Types = [.. extensions];
	}
	public class Source {
		public virtual ushort Width {get; set;}
		public virtual ushort Height {get; set;}
		public virtual ushort Depth {get; set;} = 1;
		public virtual void Read(string path) { }
		public virtual byte[] LoadByte(int channels) => null;
		public virtual uint[] LoadUint(int channels) => null;
		public Texture CreateTexture(Texture.Format format) => new() {
			Width = Width,
			Height = Height,
			Depth = Depth,
			Type = format
		};
	}

	public static void Main(string[] args) {
		List<string> argacc = [.. args];
		Console.WriteLine("-- btex converter utility --");
		if (argacc.Count == 0)
			Console.WriteLine("cmd args");
			Console.WriteLine(" <input file> <btex format>");
		if (argacc.Count < 2) {
			Console.WriteLine("btex formats");
			foreach (var format in Enum.GetValues<Texture.Format>())
				Console.WriteLine($"  {format}");
		}
		while (!File.Exists(argacc.ElementAtOrDefault(0))) {
			if (argacc.Count > 0) {
				Console.WriteLine($"{argacc[0]} is not a valid file");
				argacc.RemoveAt(0);
			}
			Console.Write("input file: ");
			argacc.Insert(0, Console.ReadLine().Trim());
		}
		while (!Enum.IsDefined(typeof(Texture.Format), argacc.ElementAtOrDefault(1) ?? "")) {
			if (argacc.Count > 1) {
				Console.WriteLine($"{argacc[1]} is not a valid format");
				argacc.RemoveAt(1);
			}
			Console.Write("btex format: ");
			var f = Console.ReadLine().Trim();
			argacc.Insert(1, f);
		}
		Write(argacc[0], Enum.Parse<Texture.Format>(argacc[1]));
	}
	public static void Write(string input, Texture.Format format) {
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
		inst.Read(input);
		var t = inst.CreateTexture(format);
		using var f = new BinaryWriter(File.OpenWrite($"{input.Split('.').First()}.btex"));
		Header(f, t);
		switch (t.Type) {
			case Texture.Format.rgba8888:
				WriteByteData(f, t, inst.LoadByte(4), 4);
				break;
			case Texture.Format.rgb888:
				WriteByteData(f, t, inst.LoadByte(3), 3);
				break;
			case Texture.Format.g8:
				WriteByteData(f, t, inst.LoadByte(1), 1);
				break;
			case Texture.Format.g32:
				WriteUintData(f, t, inst.LoadUint(1), 1);
				break;
		}
	}
	private static void Header(BinaryWriter f, Texture t) {
		f.Write("btex".ToArray());
		f.Write((byte)t.Type);
		f.Write((ushort)t.Width);
		f.Write((ushort)t.Height);
		f.Write((ushort)t.Depth);
	}
	public static void WriteByteData(BinaryWriter f, Texture t, byte[] data, int channels) {
		var read = 0;
		var last = -1;
		byte run = 0;
		for (var z = 0; z < t.Depth; z++) {
			for (var y = 0; y < t.Height; y++) {
				for (var x = 0; x < t.Width; x++) {
					var color = new byte[channels];
					var hc = new HashCode();
					for (var c = 0; c < channels; c++) {
						color[c] = data[read++];
						hc.Add(color[c]);
					}
					var hash = hc.ToHashCode();
					if (run >= 255 || hash != last) {
						if (last != -1)
							f.Write(run);
						run = 0;
						foreach (var c in color)
							f.Write(c);
						last = hash;
					} else
						run++;
				}
			}
		}
	}
	public static void WriteUintData(BinaryWriter f, Texture t, uint[] data, int channels) {
		var read = 0;
		var last = -1;
		byte run = 0;
		for (var z = 0; z < t.Depth; z++) {
			for (var y = 0; y < t.Height; y++) {
				for (var x = 0; x < t.Width; x++) {
					var color = new uint[channels];
					var hc = new HashCode();
					for (var c = 0; c < channels; c++) {
						color[c] = data[read++];
						hc.Add(color[c]);
					}
					var hash = hc.ToHashCode();
					if (run >= 255 || hash != last) {
						if (last != -1)
							f.Write(run);
						run = 0;
						foreach (var c in color)
							f.Write(c);
						last = hash;
					} else
						run++;
				}
			}
		}
		if (last != -1)
			f.Write(run);
	}
}