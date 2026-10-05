using BurningKnight.entity.item;
using Lens.assets;
using Lens.graphics.animation;

namespace BurningKnight.assets;

public static class CommonAse {
	public static AnimationData Items = null!;
	public static AnimationData Ui = null!;
	public static AnimationData Projectiles = null!;
	public static AnimationData Particles = null!;
	public static AnimationData Props = null!;
	
	public static void Load() {
		Items = Animations.Require("items");
		Ui = Animations.Require("ui");
		Projectiles = Animations.Require("projectiles");
		Particles = Animations.Require("particles");
		Props = Animations.Require("props");

		Textures.Missing = Items.RequireSlice("missing");
		Item.UnknownRegion = Items.RequireSlice("unknown");
	}
}