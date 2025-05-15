using BurningKnight.entity.component;
using Lens.entity;
using Lens.graphics.animation;

namespace BurningKnight.entity.door {
	public class LevelLock : Lock {
		public static readonly ColorMap Palette = ColorMap.New([
			(assets.Palette.Default[9], assets.Palette.Default[31]),
			(assets.Palette.Default[10], assets.Palette.Default[30]),
			(assets.Palette.Default[11], assets.Palette.Default[29])
		]);
		
		protected override ColorMap GetLockPalette() {
			return Palette;
		}

		public override void AddComponents() {
			Width = 20;
			Height = 40;

			base.AddComponents();
			
			AddComponent(new RoomComponent());
		}

		protected override AnimationComponent CreateGraphicsComponent() {
			return new AnimationComponent("level_lock", GetLockPalette());
		}

		public override bool CanInteract() {
			return false;
		}

		protected override bool TryToConsumeKey(Entity entity) {
			return false;
		}
	}
}