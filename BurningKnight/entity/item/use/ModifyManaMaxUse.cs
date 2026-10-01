using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ModifyManaMaxUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			var m = entity.GetComponent<ManaComponent>();
			m!.ManaMax += Amount * 2;
			m.ModifyMana(Amount * 2);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			
			Amount = settings["amount"].Int(1);
		}
		
	}
}