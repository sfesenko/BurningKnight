using BurningKnight.entity.events;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ModifyShootUse : ItemUse {
		private int amount;

		public override bool HandleEvent(Event e) {
			if (e is PlayerShootEvent pse) {
				pse.Times += amount;
				pse.Accurate = true;
			}
		
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			amount = settings["amount"].Int(1);
		}

	}
}