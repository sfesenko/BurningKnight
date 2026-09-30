using BurningKnight.entity.component;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class SetKnockbackModifierUse : ItemUse {
		private float mod;

		public override void Use(Entity entity, Item item) {
			entity!.GetAnyComponent<BodyComponent>()!.KnockbackModifier = mod;
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			mod = settings["mod"].Number(0);
		}
	}
}