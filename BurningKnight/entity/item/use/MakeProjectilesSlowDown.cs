using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class MakeProjectilesSlowDown : ItemUse {
		private float amount;
		private float time;

		public override bool HandleEvent(Event e) {
			if (e is ProjectileCreatedEvent pce) {
				ProjectileCallbacks.AttachUpdateCallback(pce.Projectile,  SlowdownProjectileController.Make(amount, time));
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			amount = settings["amount"].Number(1);
			time = settings["time"].Number(1);
		}
	}
}