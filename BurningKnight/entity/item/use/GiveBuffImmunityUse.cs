using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class GiveBuffImmunityUse : GiveBuffUse {
		public bool IceImmunity;
		public bool PitImmunity;
		
		public override void Use(Entity entity, Item item) {
			if (IceImmunity) {
				entity.GetComponent<BuffsComponent>()!.IceImmunity = true;
			}

			if (PitImmunity) {
				entity.GetComponent<BuffsComponent>()!.PitImmunity = true;
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			IceImmunity = settings["ice"].Bool(false);
			PitImmunity = settings["pit"].Bool(false);
		}

		public override bool HandleEvent(Event e) {
			if (e is BuffCheckEvent ev && ev.Buff.Type == Buff) {
				return true;
			}
			
			return base.HandleEvent(e);
		}
	}
}