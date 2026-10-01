using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ModifyManaUse : ItemUse {
		public int Amount;
		public bool SetToMin;
		public bool SetToMax;

		public override void Use(Entity entity, Item item) {
			if (SetToMin) {
				entity.GetComponent<ManaComponent>()!.SetMana(1);
				return;
			}

			if (SetToMax) {
				var h = entity.GetComponent<ManaComponent>();
				h!.ModifyMana(h.ManaMax);
				
				return;
			}

			entity.GetComponent<ManaComponent>()!.ModifyMana(Amount);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			
			Amount = settings["amount"].Int(1);
			SetToMin = settings["to_min"].Bool(false);
			SetToMax = settings["to_max"].Bool(false);
		}
		
	}
}