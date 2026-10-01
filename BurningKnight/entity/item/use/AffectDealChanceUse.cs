using BurningKnight.entity.events;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class AffectDealChanceUse : ItemUse {
		private float granny;
		private bool addGranny;
		private float dm;
		private bool addDm;
		private bool both;
		
		public override bool HandleEvent(Event e) {
			if (e is DealChanceCalculateEvent d) {
				if (addGranny) {
					d.GrannyChance += granny;
				} else {
					d.GrannyChance *= granny;
				}
				
				if (addDm) {
					d.DmChance += dm;
				} else {
					d.DmChance *= dm;
				}

				if (both) {
					d.OpenBoth = true;
				}
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);

			both = settings["bt"].Bool(false);
			granny = settings["gr"].Number(0);
			addGranny = settings["agr"].Bool(true);
			dm = settings["dm"].Number(0);
			addDm = settings["adm"].Bool(true);
		}
	}
}