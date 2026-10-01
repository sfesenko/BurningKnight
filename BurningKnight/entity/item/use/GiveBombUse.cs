using System;
using BurningKnight.entity.creature.player;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class GiveBombUse: ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			var h = entity.GetComponent<HeartsComponent>();
			var a = Amount;

			if (h!.Bombs < h.BombsMax) {
				var t = Math.Min(Amount, h.BombsMax - h.Bombs);
				h.ModifyBombs(t, null);
				a -= t;
			}

			if (a > 0) {
				entity.GetComponent<ConsumablesComponent>()!.Bombs += a;
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
	}
}