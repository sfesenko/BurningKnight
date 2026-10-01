using System;
using BurningKnight.assets;
using BurningKnight.entity.projectile;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace BurningKnight.entity.item.use {
	public partial class SpawnProjectilesUse : ItemUse {
		private int damage;
		private float speed;
		private float range;
		private string slice = null!;
		private int amount;
		
		public override void Use(Entity entity, Item item) {
			var s = range * 0.5f / speed;
			var builder = new ProjectileBuilder(entity, slice) {
				LightRadius = 32f,
				Color = ProjectileColor.Yellow,
				Damage = damage
			};

			if (range > 0.01f) {
				builder.Range = s;
			}

			for (var i = 0; i < amount; i++) {
				var angle = (float) i / amount * Math.PI * 2;
				builder.Shoot(angle, speed).Build();
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			
			damage = settings["damage"].Int(1);
			amount = settings["amount"].Int(1);
			speed = settings["speed"].Number(60);
			range = settings["range"].Number(0);
			slice = settings["texture"].String();
		}
	}
}