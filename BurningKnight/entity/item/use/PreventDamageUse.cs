using BurningKnight.entity.events;
using BurningKnight.entity.room.controllable.spikes;
using BurningKnight.level;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class PreventDamageUse : ItemUse {
		private bool spikes;
		private bool lava;
		private bool chasm;
		private bool bombs;
		private bool contact;

		public override bool HandleEvent(Event e) {
			if (e is HealthModifiedEvent ev) {
				if ((spikes && ev.From is Spikes)
				    || (lava && ev.From is Level)
				    || (chasm && ev.From is Chasm)
				    || (bombs && ev.Type == DamageType.Explosive)
				    || (contact && ev.Type == DamageType.Contact)) {
					
					return true;
				}
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);

			lava = settings["lv"].Bool(false);
			spikes = settings["sp"].Bool(false);
			chasm = settings["cs"].Bool(false);
			bombs = settings["bms"].Bool(false);
			contact = settings["cnt"].Bool(false);
		}
	}
}