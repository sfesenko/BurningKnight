using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class MakeProjectileShrinkUse : ItemUse {
		private float speed;
		
		public override bool HandleEvent(Event e) {
			if (e is ProjectileCreatedEvent pce) {
				ProjectileCallbacks.AttachUpdateCallback(pce.Projectile, ShrinkProjectileController.Make(speed));
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			speed = settings["speed"].Number(1);
		}
	}
}