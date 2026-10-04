using System;
using System.Collections.Generic;
using System.IO;
using Lens;
using Lens.assets;
using Lens.util;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Content.BitmapFonts;
using MonoGame.Extended.Graphics;

namespace BurningKnight.assets {
	public abstract class Font {
		public static BitmapFont Small = null!;
		public static BitmapFont Medium = null!;
		public static SpriteFont Test = null!;
		
		public static void Load() {
			// Fail fast with a clear message: a null font only moves the crash
			// to the first text render, far from the cause.
			Small = RequireFont("Fonts/small_font");
			Medium = RequireFont("Fonts/large_font");
			// Test = Assets.Content.Load<SpriteFont>("Fonts/fnt");
		}

		private static BitmapFont RequireFont(string name) {
			return Gpu.Run(() => LoadFont(name)) ?? throw new InvalidOperationException($"Font {name} failed to load.");
		}

		// MonoGame.Extended's own loader resolves the page images through TitleContainer, which
		// only reads plain files relative to the working directory — so it cannot see the content
		// source, and a packaged archive would lose its fonts. Its parser and character types are
		// public, so the font is assembled here from streams the source supplies instead.
		private static BitmapFont? LoadFont(string name)
		{
			using var stream = Assets.Source.Open($"{name}.fnt");

			if (stream == null)
			{
				Log.Error($"Font {name} was not found!");
				return null;
			}

			// The parser seeks and a deflated archive entry cannot, so the descriptor is read
			// into memory first — it is a few tens of kilobytes. Guarded like the pages:
			// a corrupt descriptor must fail with the clear message, not a raw throw.
			BitmapFontFileContent file;

			try {
				using var memory = new MemoryStream();

				stream.CopyTo(memory);
				memory.Position = 0;

				file = BitmapFontFileReader.Read(memory, name);
			} catch (Exception e) {
				Log.Error($"Failed to read font descriptor {name}: {e}");

				return null;
			}
			var pages = new Dictionary<string, Texture2D>();

			foreach (var page in file.Pages)
			{
				using var pageStream = Assets.Source.Open($"{Path.GetDirectoryName(name)}/{page}");

				if (pageStream == null)
				{
					Log.Error($"Font page {page} for {name} was not found!");
					return null;
				}

				try {
					pages[page] = Texture2D.FromStream(Engine.GraphicsDevice, pageStream);
				} catch (Exception e) {
					Log.Error($"Failed to decode font page {page} for {name}: {e}");

					// Already-decoded pages would leak on the fail-fast path; free them.
					foreach (var loaded in pages.Values) {
						try {
							loaded.Dispose();
						} catch {
							// Dying anyway.
						}
					}

					pages.Clear();

					return null;
				}
			}

			var characters = new List<BitmapFontCharacter>();

			foreach (var c in file.Characters)
			{
				var texture = pages[file.Pages[c.Page]];

				characters.Add(new BitmapFontCharacter((int) c.ID,
					new Texture2DRegion(texture, c.X, c.Y, c.Width, c.Height), c.XOffset, c.YOffset, c.XAdvance));
			}

			var font = new BitmapFont(file.FontName, file.Info.FontSize, file.Common.LineHeight, characters);

			foreach (var kerning in file.Kernings)
			{
				if (font.TryGetCharacter((int) kerning.First, out var character))
				{
					character.Kernings.Add((int) kerning.Second, kerning.Amount);
				}
			}

			return font;
		}
	}
}
