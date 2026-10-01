using BurningKnight.entity.component;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class SetKnockbackModifierUse : ItemUse {
		private float mod;

		public override void Use(Entity entity, Item item) {
			entity!.GetAnyComponent<BodyComponent>()!.KnockbackModifier = mod;
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			mod = settings["mod"].Number(0);
		}
	}
}