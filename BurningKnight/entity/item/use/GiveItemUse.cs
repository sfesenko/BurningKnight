using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.item.stand;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;
using Lens.util;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.use {
	public partial class GiveItemUse : ItemUse {
		public int Amount;
		public new string Item = null!;
		public bool OnStand;
		public bool Random;
		public bool Animate;
		public bool Hide;
		
		public override void Use(Entity entity, Item item) {
			var id = Random ? Items.Generate(i => i.Type == ItemType.Active || i.Type == ItemType.Weapon || i.Type == ItemType.Artifact) : Item;
			
			if (OnStand) {
				var i = Items.CreateAndAdd(id, entity.Area!);

				if (i == null) {
					Log.Error($"Invalid item {id}");
					return;
				}

				var stand = new ItemStand();
				entity.Area!.Add(stand);
				stand.Center = entity.Center - new Vector2(0, 8);
				stand.SetItem(i, null);
				
				return;
			}
			
			for (var j = 0; j < Amount; j++) {
				var i = Items.CreateAndAdd(id, entity.Area!);

				if (i == null) {
					Log.Error($"Invalid item {id}");
					return;
				}

				// i.Hide = Hide;
				entity.GetComponent<InventoryComponent>()!.Pickup(i, Animate);
			}
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			
			Amount = settings["amount"].Int(1);
			Item = settings["item"].AsString ?? "";
			OnStand = settings["on_stand"].Bool(false);
			Random = settings["random"].Bool(false);
			Animate = settings["animate"].Bool(true);
			Hide = settings["hide"].Bool(false);
		}
	}
}