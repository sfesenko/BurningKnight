using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.item.use.parent;
using BurningKnight.state;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	public partial class DoWithUse : DoWithTagUse {
		protected ItemUse[] Uses;
		protected float chance;

		protected override void DoAction(Entity entity, Item item, List<Entity> entities) {
			foreach (var e in entities) {
				if (chance < 100f && !Rnd.Chance(chance)) {
					continue;
				}
				
				foreach (var u in Uses) {
					u.Use(e, item);
				}
			}
		}
		
		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			
			if (!settings["uses"].IsJsonArray) {
				settings["uses"] = new JsonArray();
			}
			
			Uses = Items.ParseUses(settings["uses"]);
			chance = settings["chance"].Number(100);
		}
	}
}