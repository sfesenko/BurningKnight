using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class RerollItemsOnPlayerUse : ItemUse {
		private bool rerollWeapons;
		private bool rerollArtifacts;

		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);

			if (rerollArtifacts) {
				Reroll(entity, entity.GetComponent<InventoryComponent>()!);
			}

			if (rerollWeapons) {
				Reroll(entity.GetComponent<ActiveWeaponComponent>()!);
				Reroll(entity.GetComponent<WeaponComponent>()!);
			}

			entity.HandleEvent(new RerolledEvent {
				Entity = entity
			});
		}

		private void Reroll(ItemComponent component) {
			if (component?.Item == null) {
				return;
			}

			var item = component.Item;
			Reroller.Reroll(item, ItemPool.Treasure, i => item.Type == i.Type);
		}

		private void Reroll(Entity entity, InventoryComponent component) {
			if (entity is Player player) {
				player.InitStats();
			}
			
			foreach (var item in component.Items) {
				Reroller.Reroll(item, ItemPool.Treasure, i => item.Type == i.Type);
				item.Use(entity);
			}
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			
			rerollWeapons = settings["weapons"].Bool(false);
			rerollArtifacts = settings["artifacts"].Bool(true);
		}

		public class RerolledEvent : Event {
			public Entity Entity = null!;
		}
	}
}