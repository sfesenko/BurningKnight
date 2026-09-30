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
		Items = Animations.Get("items");
		Ui = Animations.Get("ui");
		Projectiles = Animations.Get("projectiles");
		Particles = Animations.Get("particles");
		Props = Animations.Get("props");

		Textures.Missing = Items!.GetSlice("missing");
		Item.UnknownRegion = Items.GetSlice("unknown");
	}
}