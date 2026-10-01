using BurningKnight.entity.component;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class SetMaxHpUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			var component = entity.GetComponent<HealthComponent>();
			component!.InitMaxHealth = Amount + 1; // 1 is hidden
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
	}
}