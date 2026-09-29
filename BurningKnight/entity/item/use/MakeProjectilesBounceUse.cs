using BurningKnight.entity.events;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class MakeProjectilesBounceUse : ItemUse {
		private int count;

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			count = settings["count"].Int(1);
		}

		public override bool HandleEvent(Event e) {
			if (e is ProjectileCreatedEvent pce) {
				pce.Projectile.Bounce += count;
			}
			
			return base.HandleEvent(e);
		}
	}
}