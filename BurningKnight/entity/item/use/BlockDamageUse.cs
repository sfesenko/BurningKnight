using BurningKnight.assets.particle.custom;
using BurningKnight.entity.events;
using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	public partial class BlockDamageUse : ItemUse {
		private float chance;

		public override bool HandleEvent(Event e) {
			if (e is HealthModifiedEvent hme) {
				if (hme.Amount < 0 && hme.Who == Item.Owner && Rnd.Chance(chance)) {
					hme.Amount = 0;

					if (Item.Id == "bk:cats_ear") {
						TextParticle.Add(Item.Owner, "Sick dodge!");
					}
					
					return true;
				}
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			chance = settings["chance"].Number(10);
		}

	}
}