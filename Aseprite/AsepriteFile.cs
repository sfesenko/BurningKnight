using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework;

namespace Aseprite;

public class AsepriteFileData(
	ushort width,
	ushort height,
	List<AsepriteFrame> frames,
	List<AsepriteLayer> layers,
	List<AsepriteTag> tags,
	List<AsepriteSlice> slices)
{
	public readonly ushort Width = width;
	public readonly ushort Height = height;

	public readonly List<AsepriteFrame> Frames = frames;
	public readonly List<AsepriteLayer> Layers = layers;
	public readonly List<AsepriteTag> Tags = tags;
	public readonly List<AsepriteSlice> Slices = slices;
	public readonly Dictionary<string, AsepriteAnimation> Animations = new();

	public int TextureWidth => Frames.Count * Width;
	public int TextureHeight => Layers.Count * Height;

	public Color[] PixelData => PrepareTextureData();


	private Color[] PrepareTextureData()
	{
		int width = Width;
		int height = Height;
		var textureWidth = TextureWidth;
		var size = textureWidth * (TextureHeight + 1);

		var textureData = new Color[size];
		for (var f = 0; f < Frames.Count; f++) {
			var frame = Frames[f];

			for (var celNo = 0; celNo < frame.Cels.Count; celNo++) {
				var cel = frame.Cels[celNo];
					
				var startX = cel.X;
				var startY = cel.Y;

				for (var celY = 0; celY < cel.Height; celY++)
				{
					for (var celX = 0; celX < cel.Width; celX++)
					{
						var pixel = cel.Pixels[celX + celY * cel.Width];

						var index = (f * width) + startX + celX + (startY + (celNo * height) + celY) * textureWidth;
						textureData[index] = pixel;
					}
				}
			}
		}
		return textureData;
	}
}

public static class AsepriteFile
{
	private const ushort ASEPRITE_MAGIC = 0xA5E0;
	
	public static AsepriteFileData ReadAsepriteFile(string filename) 
	{
		using var reader = new BinaryReader(File.OpenRead(filename));
		// file size
		_ = reader.DWORD();

		// Magic number

		if (reader.WORD() != ASEPRITE_MAGIC) {
			throw new ArgumentException($"File {filename} doesn't appear to be from Aseprite.");
		}
		
		ReadHeader(reader, out var frameCount, out var width, out var height, out var mode);
		return ReadBody(reader, frameCount, width, height, mode);
	}
	
	//
	
	private static void ReadHeader(BinaryReader reader,
		out ushort frameCount,
		out ushort width,
		out ushort height,
		out Modes mode)
	{
		// Basic info
		frameCount = reader.WORD();

		width = reader.WORD();
		height = reader.WORD();

		mode = (Modes) (reader.WORD() / 8);

		// logger?.LogMessage($"Cels are {width}x{height}, mode is {mode}");

		// Ignore a bunch of stuff
		reader.DWORD(); // Flags
		reader.WORD(); // Speed (deprecated)
		reader.DWORD(); // 0
		reader.DWORD(); // 0
		reader.BYTE(); // Palette entry 
		reader.Seek(3); // Ignore these bytes
		reader.WORD(); // Number of colors (0 means 256 for old sprites)
		reader.BYTE(); // Pixel width
		reader.BYTE(); // Pixel height
		reader.Seek(92); // For Future
	}

	private static AsepriteFileData ReadBody(BinaryReader reader, ushort frameCount, ushort width, ushort height, Modes mode)
	{
		List<AsepriteFrame> frames = [];
		List<AsepriteLayer> layers = [];
		List<AsepriteTag> tags = [];
		List<AsepriteSlice> slices = [];
		 // Dictionary<string, AsepriteAnimation> Animations = new();

		// Some temporary holders
		var colorBuffer = new byte[width * height * (int) mode];
		var palette = new Color[256];

		IUserData lastUserData = null!;

		for (var i = 0; i < frameCount; i++) {
			var frame = new AsepriteFrame();
			frames.Add(frame);

			long frameEnd;
			int chunkCount;

			// Frame header
			{
				var frameStart = reader.BaseStream.Position;
				frameEnd = frameStart + reader.DWORD();
				reader.WORD(); // Magic number (always 0xF1FA)

				chunkCount = reader.WORD();
				frame.Duration = reader.WORD() / 1000f;
				reader.Seek(6); // For future (set to zero)
			}

			for (var j = 0; j < chunkCount; j++) {
				long chunkEnd;
				Chunks chunkType;

				// Chunk header
				{
					var chunkStart = reader.BaseStream.Position;
					chunkEnd = chunkStart + reader.DWORD();
					chunkType = (Chunks) reader.WORD();
				}

				switch (chunkType)
				{
					// Layer
					case Chunks.Layer:
					{
						var layer = new AsepriteLayer
						{
							Flag = (AsepriteLayer.Flags) reader.WORD(),
							Type = (AsepriteLayer.Types) reader.WORD(),
							ChildLevel = reader.WORD()
						};

						reader.WORD(); // width
						reader.WORD(); // height

						layer.BlendMode = (AsepriteLayer.BlendModes) reader.WORD();
						layer.Opacity = reader.BYTE() / 255f;
						reader.Seek(3);
						layer.Name = reader.STRING();

						lastUserData = layer;
						layers.Add(layer);
						break;
					}
					case Chunks.Cel:
					{
						// Cell
						var cel = new AsepriteCel();

						var layerIndex = reader.WORD();
						cel.Layer = layers[layerIndex]; // Layer is row (Frame is column)
						cel.X = reader.SHORT();
						cel.Y = reader.SHORT();
						cel.Opacity = reader.BYTE() / 255f;

						var celType = (CelTypes) reader.WORD();
						reader.Seek(7);

						switch (celType)
						{
							case CelTypes.RawCel or CelTypes.CompressedImage:
							{
								cel.Width = reader.WORD();
								cel.Height = reader.WORD();
								
								var byteCount = cel.Width * cel.Height * (int) mode;
								
								if (celType == CelTypes.RawCel) {
									reader.BaseStream.ReadExactly(colorBuffer, 0, byteCount);
								} else {
									reader.Seek(2);
									new DeflateStream(reader.BaseStream, CompressionMode.Decompress)
										.ReadExactly(colorBuffer, 0, byteCount);
								}

								cel.Pixels = new Color[cel.Width * cel.Height];
								ConvertBytesToPixels(colorBuffer, cel.Pixels, palette, mode);
								break;
							}
							case CelTypes.LinkedCel:
							{
								var targetFrame = reader.WORD(); // Frame position to link with

								// Grab the cel from a previous frame
								var targetCel = frames[targetFrame].Cels.First(c => c.Layer == layers[layerIndex]);
								
								cel.Width = targetCel.Width;
								cel.Height = targetCel.Height;
								cel.Pixels = targetCel.Pixels;
								break;
							}
						}

						lastUserData = cel;
						frame.Cels.Add(cel);
						break;
					}
					case Chunks.Palette:
					{
						// Palette

						var size = reader.DWORD();
						var start = reader.DWORD();
						var end = reader.DWORD();
						reader.Seek(8);

						for (var c = 0; c < (end - start) + 1; c++) {
							var hasName = ((uint)reader.WORD()).IsBitSet(0);
							palette[start + c] = new Color(reader.BYTE(), reader.BYTE(), reader.BYTE(), reader.BYTE());
								
							if (hasName) {
								reader.STRING(); // Color name
							}
						}

						break;
					}
					case Chunks.UserData:
					{
						// User data

						if (lastUserData != null) {
							var flags = reader.DWORD();
							if (flags.IsBitSet(0)) {
								lastUserData.UserDataText = reader.STRING();
							}
							else if (flags.IsBitSet(1)) {
								lastUserData.UserDataColor = new Color(reader.BYTE(), reader.BYTE(), reader.BYTE(), reader.BYTE());
							}
						}

						break;
					}
					case Chunks.FrameTags:
					{
						// Tag (animation reference)

						var tagsCount = reader.WORD();
						reader.Seek(8);
							
						for (var t = 0; t < tagsCount; t++) {
							var tag = new AsepriteTag
							{
								From = reader.WORD(),
								To = reader.WORD(),
								LoopDirection = (AsepriteTag.LoopDirections) reader.BYTE()
							};

							reader.Seek(8);
							tag.Color = new Color(reader.BYTE(), reader.BYTE(), reader.BYTE(), (byte) 255);
							reader.Seek(1);
							tag.Name = reader.STRING();

							tags.Add(tag);
						}

						break;
					}
					case Chunks.Slice:
					{
						// Slice

						var slicesCount = reader.DWORD();
						var flags = reader.DWORD();
						// reserved
						reader.DWORD();
						var name = reader.STRING();

						for (var s = 0; s < slicesCount; s++) {
							var slice = new AsepriteSlice
							{
								Name = name,
								Frame = (int) reader.DWORD(),
								OriginX = (int) reader.LONG(),
								OriginY = (int) reader.LONG(),
								Width = (int) reader.DWORD(),
								Height = (int) reader.DWORD()
							};

							// 9 slice
							if (flags.IsBitSet(0)) {
								reader.LONG(); // Center X position (relative to slice bounds)
								reader.LONG(); // Center Y position
								reader.DWORD(); // Center width
								reader.DWORD(); // Center height
							}	
							if (flags.IsBitSet(1)) {
								// Pivot

								slice.Pivot = new Point((int) reader.DWORD(), (int) reader.DWORD());
							}

							lastUserData = slice;
							slices.Add(slice);
						}

						break;
					}
					case Chunks.OldPaletteA:
					case Chunks.OldPaletteB:
					case Chunks.CelExtra:
					case Chunks.Mask:
					case Chunks.Path:
					default:
						// Not implemented {chunkType}
						break;
				}

				reader.BaseStream.Position = chunkEnd;
			}

			reader.BaseStream.Position = frameEnd;
		}
		
		return new AsepriteFileData(width, height, frames, layers, tags, slices);
	}
	
	private static void ConvertBytesToPixels(byte[] bytes, Color[] pixels, Color[] palette, Modes mode)
	{
		var length = pixels.Length;

		switch (mode)
		{
			case Modes.Rgba:
			{
				for (int pixel = 0, b = 0; pixel < length; pixel++, b += 4) {
					pixels[pixel].R = (byte) (bytes[b + 0] * bytes[b + 3] / 255);
					pixels[pixel].G = (byte) (bytes[b + 1] * bytes[b + 3] / 255);
					pixels[pixel].B = (byte) (bytes[b + 2] * bytes[b + 3] / 255);
					pixels[pixel].A = bytes[b + 3];
				}

				break;
			}
			case Modes.Grayscale:
			{
				for (int pixel = 0, b = 0; pixel < length; pixel++, b += 2) {
					pixels[pixel].R = pixels[pixel].G = pixels[pixel].B = (byte) (bytes[b + 0] * bytes[b + 1] / 255);
					pixels[pixel].A = bytes[b + 1];
				}

				break;
			}
			case Modes.Indexed:
			{
				for (var pixel = 0; pixel < length; pixel++) {
					int index = bytes[pixel];

					if (index > 0) {
						pixels[pixel] = palette[index];						
					}
				}

				break;
			}
		}
	}
	
	private enum Chunks {
		OldPaletteA = 0x0004,
		OldPaletteB = 0x0011,
		Layer = 0x2004,
		Cel = 0x2005,
		CelExtra = 0x2006,
		Mask = 0x2016,
		Path = 0x2017,
		FrameTags = 0x2018,
		Palette = 0x2019,
		UserData = 0x2020,
		Slice = 0x2022
	}

	private enum CelTypes {
		RawCel = 0,
		LinkedCel = 1,
		CompressedImage = 2
	}
	
	private enum Modes {
		Indexed = 1,
		Grayscale = 2,
		Rgba = 4
	}
}

internal static class AsepriteUtils
{
	// Helpers for translating the Aseprite file format reference
	// See: https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md
	
	public static void Seek(this BinaryReader reader, int number)
	{
		reader.BaseStream.Position += number;
	}

	public static byte BYTE(this BinaryReader reader) {
		return reader.ReadByte();
	}

	public static ushort WORD(this BinaryReader reader) {
		return reader.ReadUInt16();
	}

	public static short SHORT(this BinaryReader reader) {
		return reader.ReadInt16();
	}

	public static uint DWORD(this BinaryReader reader) {
		return reader.ReadUInt32();
	}

	public static long LONG(this BinaryReader reader) {
		return reader.ReadInt32();
	}

	public static string STRING(this BinaryReader reader) {
		return Encoding.UTF8.GetString(reader.BYTES(reader.WORD()));
	}

	public static byte[] BYTES(this BinaryReader reader, int number) {
		return reader.ReadBytes(number);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBitSet(this uint b, int pos) {
		return (b & (1 << pos)) != 0;
	}
}

// UserData are extended chunks that get attached
// to other chunks
public interface IUserData {
	string UserDataText { get; set; }
	Color UserDataColor { get; set; }
}

// A layer stores just the meta info for a row of cels
public class AsepriteLayer : IUserData {
	[Flags]
	public enum Flags {
		Visible = 1,
		Editable = 2,
		LockMovement = 4,
		Background = 8,
		PreferLinkedCels = 16,
		Collapsed = 32,
		Reference = 64
	}

	public enum Types {
		Normal = 0,
		Group = 1
	}

	public enum BlendModes {
		Normal = 0,
		Multiply = 1,
		Screen = 2,
		Overlay = 3,
		Darken = 4,
		Lighten = 5,
		ColorDodge = 6,
		ColorBurn = 7,
		HardLight = 8,
		SoftLight = 9,
		Difference = 10,
		Exclusion = 11,
		Hue = 12,
		Saturation = 13,
		Color = 14,
		Luminosity = 15,
		Addition = 16,
		Subtract = 17,
		Divide = 18
	}

	public Flags Flag;
	public Types Type;

	public bool Visible;
	public string Name;
	public float Opacity;
	public BlendModes BlendMode;
	public int ChildLevel;

	string IUserData.UserDataText { get; set; }
	Color IUserData.UserDataColor { get; set; }
}

// A frame is a column of cels
public class AsepriteFrame {
	public float Duration;
	public readonly List<AsepriteCel> Cels = [];
}

// Tags are animation references
public class AsepriteTag {
	public enum LoopDirections {
		Forward = 0,
		Reverse = 1,
		PingPong = 2
	}

	public string Name;
	public LoopDirections LoopDirection;
	public int From;
	public int To;
	public Color Color;
}

public struct AsepriteSlice : IUserData {
	public int Frame;
	public string Name;
	public int OriginX;
	public int OriginY;
	public int Width;
	public int Height;
	public Point? Pivot;

	string IUserData.UserDataText { get; set; }
	Color IUserData.UserDataColor { get; set; }
}
// Cels are just pixel grids
public class AsepriteCel : IUserData {
	public AsepriteLayer Layer;

	public Color[] Pixels;

	public int X;
	public int Y;
	public int Width;
	public int Height;
	public float Opacity;

	public string UserDataText { get; set; }
	public Color UserDataColor { get; set; }
}

public class AsepriteAnimation {
	public int FirstFrame;
	public int LastFrame;
	public string Name;
	public AsepriteTag.LoopDirections Directions;
}
