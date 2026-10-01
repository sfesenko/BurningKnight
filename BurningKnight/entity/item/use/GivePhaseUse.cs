using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	public partial class GivePhaseUse : ItemUse {
		public int Amount;
		public bool Broken;

		public override void Use(Entity entity, Item item) {
			if (!item.Used && (!Broken || Rnd.Chance())) {
				entity.GetComponent<HealthComponent>()!.Phases += (byte) Amount;
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			
			Amount = settings["amount"].Int(1);
			Broken = settings["broken"].Bool(false);
		}
	}
}