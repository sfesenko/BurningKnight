using Lens;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace BurningKnight.assets {
	public abstract class Font {
		public static BitmapFont Small;
		public static BitmapFont Medium;
		public static SpriteFont Test;
		
		public static void Load() {
			Small = LoadFont("Fonts/small_font");
			Medium = LoadFont("Fonts/large_font");
			// Test = Assets.Content.Load<SpriteFont>("Fonts/fnt");
		}

		private static BitmapFont LoadFont(string name)
		{
			var fontFileName = $"Content/{name}.fnt";
			return BitmapFont.FromFile(Engine.GraphicsDevice, fontFileName);
		}
	}
}