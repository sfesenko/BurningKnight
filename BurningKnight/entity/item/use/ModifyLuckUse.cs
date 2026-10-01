using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.state;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ModifyLuckUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			Context.Run.Luck += Amount;
			TextParticle.Add(entity, Locale.Get("luck"), Amount, true, Amount < 0);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
	}
}