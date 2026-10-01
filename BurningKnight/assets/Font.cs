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
			Small = Gpu.Run(() => LoadFont("Fonts/small_font"))!;
			Medium = Gpu.Run(() => LoadFont("Fonts/large_font"))!;
			// Test = Assets.Content.Load<SpriteFont>("Fonts/fnt");
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
			// into memory first — it is a few tens of kilobytes.
			using var memory = new MemoryStream();

			stream.CopyTo(memory);
			memory.Position = 0;

			var file = BitmapFontFileReader.Read(memory, name);
			var pages = new Dictionary<string, Texture2D>();

			foreach (var page in file.Pages)
			{
				using var pageStream = Assets.Source.Open($"{Path.GetDirectoryName(name)}/{page}");

				pages[page] = Texture2D.FromStream(Engine.GraphicsDevice, pageStream);
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
