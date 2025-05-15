using BurningKnight.entity.creature.player;
using Lens.entity;
using Lens.graphics.animation;

namespace BurningKnight.entity.door {
	public class GoldLock : Lock {
		
		protected override ColorMap GetLockPalette() {
			return LevelLock.Palette;
		}

		protected override bool CanInteract(Entity entity) {
			return entity.TryGetComponent<ConsumablesComponent>(out var component) && component.Keys > 0;
		}

		protected override bool TryToConsumeKey(Entity entity) {
			if (!entity.TryGetComponent<ConsumablesComponent>(out var component)) {
				return false;
			}

			if (component.Keys > 0) {
				component.Keys--;
				Audio.PlaySfx("item_key");
				return true;
			}
			
			return false;
		}
	}
}