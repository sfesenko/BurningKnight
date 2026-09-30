using System.Collections.Generic;
using BurningKnight.assets;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.projectile {
	public static class ProjectileColor {
		public static readonly Color Yellow = Palette.Default[30];
		public static Color BkRed = new Color(1f, 0f, 0f);
		public static readonly Color Red = Palette.Default[0];
		public static readonly Color Green = Palette.Default[35];
		public static readonly Color DarkGreen = Palette.Default[37];
		public static readonly Color Blue = Palette.Default[40];
		public static readonly Color Cyan = Palette.Default[42];
		public static readonly Color Purple = Palette.Default[54];
		public static readonly Color Orange = Palette.Default[28];
		public static readonly Color Pink = Palette.Default[59];
		public static readonly Color Brown = Palette.Default[19];
		public static readonly Color Gray = Palette.Default[6];
		public static Color Black = Color.Black;
		public static Color White = Color.White;

		public static Color[] Rainbow = {
			Red, Orange, Yellow, Green, Cyan, Blue, Purple
		};

		public static Color[] DesertRainbow = {
			Red, Orange, Yellow
		};

		public static Dictionary<string, Color> Colors = new Dictionary<string, Color> {
			{ "yellow", Yellow },
			{ "red", Red },
			{ "green", Green },
			{ "blue", Blue },
			{ "cyan", Cyan },
			{ "purple", Purple },
			{ "orange", Orange },
			{ "pink", Pink },
			{ "brown", Brown },
			{ "black", Black },
			{ "white", White },
		};
	}
}