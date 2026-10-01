using BurningKnight.entity.component;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public class GiveHeartContainersUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			var component = entity.GetComponent<HealthComponent>();
			component!.MaxHealth += Amount;
			
			if (!item.Used && Amount > 0) {
				component.ModifyHealth(Amount, entity);
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
	}
}