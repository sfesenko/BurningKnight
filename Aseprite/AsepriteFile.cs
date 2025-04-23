using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace Aseprite;

public enum Modes {
	Indexed = 1,
	Grayscale = 2,
	Rgba = 4
}

public class AsepriteFile {
	private readonly Modes _mode;
	public int Width;
	public int Height;

	public readonly List<AsepriteFrame> Frames = [];
	public readonly List<AsepriteLayer> Layers = [];
	public readonly List<AsepriteTag> Tags = [];
	public readonly List<AsepriteSlice> Slices = [];
	public readonly Dictionary<string, AsepriteAnimation> Animations = new();

	// public Texture2D Texture;
	public readonly Color[] pixelData;

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


	public int TextureWidth => Frames.Count * Width;
	public int TextureHeight => Layers.Count * Height;
	
	public AsepriteFile(string filename) : this(filename, null) {
		int framesCount = Frames.Count;

		int width = Width;
		int height = Height;
		int size = TextureWidth * (TextureHeight + 1);

		var textureData = new Color[size];
		for (int f = 0; f < framesCount; f++) {
			var frame = Frames[f];

			for (var celNo = 0; celNo < frame.Cels.Count; celNo++) {
				var cel = frame.Cels[celNo];
					
				var startX = cel.X;
				var startY = cel.Y;

				for (var celY = 0; celY < cel.Height; celY++)
				{
					for (var celX = 0; celX < cel.Width; celX++)
					{
						Color pixel = cel.Pixels[celX + celY * cel.Width];

						var index = (f * width) + startX + celX + (startY + (celNo * height) + celY) * TextureWidth;
						textureData[index] = pixel;
					}
				}
			}
		}
		
		pixelData = textureData;
	}

	private AsepriteFile(string filename, ContentBuildLogger logger) {
		using (var reader = new BinaryReader(File.OpenRead(filename))) {
				
			#region File helpers

			// Helpers for translating the Aseprite file format reference
			// See: https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md
			byte BYTE() {
				return reader.ReadByte();
			}

			ushort WORD() {
				return reader.ReadUInt16();
			}

			short SHORT() {
				return reader.ReadInt16();
			}

			uint DWORD() {
				return reader.ReadUInt32();
			}

			long LONG() {
				return reader.ReadInt32();
			}

			string STRING() {
				return Encoding.UTF8.GetString(BYTES(WORD()));
			}

			byte[] BYTES(int number) {
				return reader.ReadBytes(number);
			}

			void SEEK(int number) {
				reader.BaseStream.Position += number;
			}

			#endregion

			#region Consume header

			int frameCount;

			{
				DWORD();

				// Magic number
				var magic = WORD();

				if (magic != 0xA5e0) {
					throw new Exception("File doesn't appear to be from Aseprite.");
				}

				// Basic info
				frameCount = WORD();

				Width = WORD();
				Height = WORD();

				_mode = (Modes) (WORD() / 8);

				logger?.LogMessage($"Cels are {Width}x{Height}, mode is {_mode}");

				// Ignore a bunch of stuff
				DWORD(); // Flags
				WORD(); // Speed (deprecated)
				DWORD(); // 0
				DWORD(); // 0
				BYTE(); // Palette entry 
				SEEK(3); // Ignore these bytes
				WORD(); // Number of colors (0 means 256 for old sprites)
				BYTE(); // Pixel width
				BYTE(); // Pixel height
				SEEK(92); // For Future
			}

			#endregion

			#region Actual data

			// Some temporary holders
			var colorBuffer = new byte[Width * Height * (int) _mode];
			var palette = new Color[256];

			IUserData lastUserData = null;

			for (int i = 0; i < frameCount; i++) {
				var frame = new AsepriteFrame();
				Frames.Add(frame);

				long frameEnd;
				int chunkCount;

				// Frame header
				{
					var frameStart = reader.BaseStream.Position;
					frameEnd = frameStart + DWORD();
					WORD(); // Magic number (always 0xF1FA)

					chunkCount = WORD();
					frame.Duration = WORD() / 1000f;
					SEEK(6); // For future (set to zero)
				}

				for (var j = 0; j < chunkCount; j++) {
					long chunkEnd;
					Chunks chunkType;

					// Chunk header
					{
						var chunkStart = reader.BaseStream.Position;
						chunkEnd = chunkStart + DWORD();
						chunkType = (Chunks) WORD();
					}

					switch (chunkType)
					{
						// Layer
						case Chunks.Layer:
						{
							var layer = new AsepriteLayer
							{
								Flag = (AsepriteLayer.Flags) WORD(),
								Type = (AsepriteLayer.Types) WORD(),
								ChildLevel = WORD()
							};

							WORD(); // width
							WORD(); // height

							layer.BlendMode = (AsepriteLayer.BlendModes) WORD();
							layer.Opacity = BYTE() / 255f;
							SEEK(3);
							layer.Name = STRING();

							lastUserData = layer;
							Layers.Add(layer);
							break;
						}
						case Chunks.Cel:
						{
							// Cell
							var cel = new AsepriteCel();

							var layerIndex = WORD();
							cel.Layer = Layers[layerIndex]; // Layer is row (Frame is column)
							cel.X = SHORT();
							cel.Y = SHORT();
							cel.Opacity = BYTE() / 255f;

							var celType = (CelTypes) WORD();
							SEEK(7);

							switch (celType)
							{
								case CelTypes.RawCel or CelTypes.CompressedImage:
								{
									cel.Width = WORD();
									cel.Height = WORD();
								
									var byteCount = cel.Width * cel.Height * (int) _mode;
								
									if (celType == CelTypes.RawCel) {
										reader.BaseStream.ReadExactly(colorBuffer, 0, byteCount);
									} else {
										SEEK(2);
										new DeflateStream(reader.BaseStream, CompressionMode.Decompress)
											.ReadExactly(colorBuffer, 0, byteCount);
									}

									cel.Pixels = new Color[cel.Width * cel.Height];
									ConvertBytesToPixels(colorBuffer, cel.Pixels, palette);
									break;
								}
								case CelTypes.LinkedCel:
								{
									var targetFrame = WORD(); // Frame position to link with

									// Grab the cel from a previous frame
									var targetCel = Frames[targetFrame].Cels.First(c => c.Layer == Layers[layerIndex]);
								
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

							var size = DWORD();
							var start = DWORD();
							var end = DWORD();
							SEEK(8);

							for (int c = 0; c < (end - start) + 1; c++) {
								var hasName = Calc.IsBitSet(WORD(), 0);
								palette[start + c] = new Color(BYTE(), BYTE(), BYTE(), BYTE());
								
								if (hasName) {
									STRING(); // Color name
								}
							}

							break;
						}
						case Chunks.UserData:
						{
							// User data

							if (lastUserData != null) {
								var flags = DWORD();
								if (Calc.IsBitSet(flags, 0)) {
									lastUserData.UserDataText = STRING();
								}
								else if (Calc.IsBitSet(flags, 1)) {
									lastUserData.UserDataColor = new Color(BYTE(), BYTE(), BYTE(), BYTE());
								}
							}

							break;
						}
						case Chunks.FrameTags:
						{
							// Tag (animation reference)

							var tagsCount = WORD();
							SEEK(8);
							
							for (var t = 0; t < tagsCount; t++) {
								var tag = new AsepriteTag
								{
									From = WORD(),
									To = WORD(),
									LoopDirection = (AsepriteTag.LoopDirections) BYTE()
								};

								SEEK(8);
								tag.Color = new Color(BYTE(), BYTE(), BYTE(), (byte) 255);
								SEEK(1);
								tag.Name = STRING();

								Tags.Add(tag);
							}

							break;
						}
						case Chunks.Slice:
						{
							// Slice

							var slicesCount = DWORD();
							var flags = DWORD();
							DWORD();
							var name = STRING();

							for (var s = 0; s < slicesCount; s++) {
								var slice = new AsepriteSlice
								{
									Name = name,
									Frame = (int) DWORD(),
									OriginX = (int) LONG(),
									OriginY = (int) LONG(),
									Width = (int) DWORD(),
									Height = (int) DWORD()
								};

								// 9 slice
								if (Calc.IsBitSet(flags, 0)) {
									LONG(); // Center X position (relative to slice bounds)
									LONG(); // Center Y position
									DWORD(); // Center width
									DWORD(); // Center height
								}	else if (Calc.IsBitSet(flags, 1)) {
									// Pivot

									slice.Pivot = new Point((int) DWORD(), (int) DWORD());
								}

								lastUserData = slice;
								Slices.Add(slice);
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

			#endregion
		}

		if (logger == null) {
			return;
		}
			
		// Log out what we found
		logger.LogMessage("Layers:");
			
		foreach (var layer in Layers) {
			logger.LogMessage($"\t{layer.Name}");
		}

		logger.LogMessage("Animations:");
			
		foreach (var animation in Tags)
		{
			logger.LogMessage(animation.To == animation.From
				? $"\t{animation.Name} => {animation.From + 1}"
				: $"\t{animation.Name} => {animation.From + 1} - {animation.To + 1}");
		}
	}

	private void ConvertBytesToPixels(byte[] bytes, Color[] pixels, Color[] palette)
	{
		var length = pixels.Length;

		switch (_mode)
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
				for (int pixel = 0; pixel < length; pixel++) {
					int index = bytes[pixel];

					if (index > 0) {
						pixels[pixel] = palette[index];						
					}
				}

				break;
			}
		}
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
