using System.Collections.Generic;
using Lens.util;
using Microsoft.Xna.Framework;

namespace Lens.graphics.animation;

public class ColorSet {
	public readonly Color[] From;
	public readonly Color[] To;
	public readonly int Id;

	private ColorSet(Color[] from, Color[] to, int id) {
		From = from;
		To = to;
		Id = id;

		if (from.Length != to.Length) {
			Log.Error("Invalid colorset");
		}
	}

	private static readonly List<ColorSet> Colors = [];

	public static ColorSet New(Color[] from, Color[] to) {
		var set = new ColorSet(from, to, Colors.Count);
		Colors.Add(set);

		return set;
	}

}