using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ModifyStatUse : ItemUse {
		private Stat stat;
		private float value;
		
		public override void Use(Entity entity, Item item) {
			switch (stat) {
				case Stat.InvincibilityTime: {
					entity.GetComponent<HealthComponent>()!.InvincibilityTimerMax += value;
					TextParticle.Add(entity, Locale.Get("invincibility_time"));
					
					break;
				}
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			stat = (Stat) settings["stat"].Int(0);
			value = settings["val"].Number(1);
		}
		
		private enum Stat {
			InvincibilityTime,
			
			Total
		}

		private static string[] stats;

		static ModifyStatUse() {
			stats = new string[(int) Stat.Total];

			for (var i = 0; i < (int) Stat.Total; i++) {
				stats[i] = $"{(Stat) i}";
			}
		}
	}
}