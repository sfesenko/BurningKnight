using BurningKnight.entity.component;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class SetMaxHpUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			var component = entity.GetComponent<HealthComponent>();
			component!.InitMaxHealth = Amount + 1; // 1 is hidden
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
	}
}