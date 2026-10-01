using System;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class MakeProjectilesKillWithBuffUse : ItemUse {
		private Type buff = null!;
		
		public override bool HandleEvent(Event e) {
			if (e is ProjectileCreatedEvent pce) {
				ProjectileCallbacks.AttachHurtCallback(pce.Projectile, (p, w) => {
					if (w is Mob m && w.GetComponent<BuffsComponent>()!.Buffs.ContainsKey(buff)) {
						m.Kill(Item);
					}
				});
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			var b = settings["buff"].String("bk:frozen");

			if (BuffRegistry.All.TryGetValue(b, out var i)) {
				buff = i.Buff;
			}
		}

	}
}