using BurningKnight.entity.creature.player;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class GiveKeyUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			entity.GetComponent<ConsumablesComponent>()!.Keys += Amount;
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
	}
}