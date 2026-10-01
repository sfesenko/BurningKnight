using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class InvokeItemsUse : ItemUse {
		private bool pets;
		private bool orbitals;
		private bool any;

		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);
			
			var inventory = entity.GetComponent<InventoryComponent>();
			var items = new List<string>();

			foreach (var i in inventory!.Items) {
				var use = any;

				if (!use) {
					var data = i.Data;
					use = (pets && ItemPool.Pet.Contains(data.Pools)) || (orbitals && ItemPool.Orbital.Contains(data.Pools));
				}

				if (use) {
					items.Add(i.Id);
				}
			}

			foreach (var i in items) {
				inventory.Pickup(Items.CreateAndAdd(i, entity.Area!), false);
			}
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);

			pets = settings["pets"].Bool(true);
			orbitals = settings["orbitals"].Bool(false);
			any = settings["any"].Bool(false);
		}

	}
}