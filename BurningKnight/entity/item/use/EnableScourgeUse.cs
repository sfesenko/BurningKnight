using BurningKnight.state;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class EnableScourgeUse : ItemUse {
		private string scourge;

		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);
			Scourge.Enable(scourge);

			if (Context.Run.Scourge == 0) {
				Context.Run.AddScourge();
			}
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			scourge = settings["scourge"].String(Scourge.OfUnknown);
		}

	}
}