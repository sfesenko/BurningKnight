using BurningKnight.assets.particle.custom;
using BurningKnight.state;
using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ScourgeUse : ItemUse {
		private int amount;
		
		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);

			for (var i = 0; i < amount; i++) {
				Context.Run.AddScourge();
			}
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			amount = settings["amount"].Int(1);
		}
		
	}
}