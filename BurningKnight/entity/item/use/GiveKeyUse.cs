using BurningKnight.entity.creature.player;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class GiveKeyUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			entity.GetComponent<ConsumablesComponent>()!.Keys += Amount;
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
	}
}