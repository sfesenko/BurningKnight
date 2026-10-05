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
			// Fail fast: a null font only crashes later, at the first text render.
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
			var pages = new Dictionary<string, Texture2D>();

			try {
				using var stream = Assets.Source.Open($"{name}.fnt");

				if (stream == null)
				{
					Log.Error($"Font {name} was not found!");
					return null;
				}

				// The parser seeks; a deflated entry cannot — read the descriptor into memory first.
				// Guarded like the pages: corrupt → the clear message, not a raw throw.
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

				foreach (var page in file.Pages)
				{
					using var pageStream = Assets.Source.Open($"{Path.GetDirectoryName(name)}/{page}");

					if (pageStream == null)
					{
						Log.Error($"Font page {page} for {name} was not found!");
						DisposePages(pages);

						return null;
					}

					try {
						pages[page] = Texture2D.FromStream(Engine.GraphicsDevice, pageStream);
					} catch (Exception e) {
						Log.Error($"Failed to decode font page {page} for {name}: {e}");
						DisposePages(pages);

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
			} catch (Exception e) {
				// Open reads the whole archive entry, so a corrupt .fnt throws before any inner
				// guard: keep RequireFont's message and free decoded pages.
				Log.Error($"Failed to load font {name}: {e}");
				DisposePages(pages);

				return null;
			}
		}

		private static void DisposePages(Dictionary<string, Texture2D> pages) {
			foreach (var loaded in pages.Values) {
				try {
					loaded.Dispose();
				} catch {
					// Dying anyway.
				}
			}

			pages.Clear();
		}
	}
}
