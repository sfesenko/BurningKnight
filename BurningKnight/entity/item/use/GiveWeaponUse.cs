using BurningKnight.assets.items;
using BurningKnight.entity.creature.player;
using Lens.entity;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class GiveWeaponUse : ItemUse {
		public string Item;

		public override void Use(Entity entity, Item item) {
			var i = Items.CreateAndAdd(Item, entity.Area);

			if (i == null) {
				Log.Error($"Invalid item {item}");
				return;
			}

			var o = entity.GetComponent<WeaponComponent>();
			var c = (WeaponComponent) entity.GetComponent<ActiveWeaponComponent>();

			if (o.Item == item) {
				c = o;
			}
			
			var old = c.Item;

			c.Set(i, false);
			old.Done = true;
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			Item = settings["item"].AsString ?? "";
		}
	}
}