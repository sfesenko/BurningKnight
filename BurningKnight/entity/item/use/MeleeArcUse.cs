using System;
using BurningKnight.entity.component;
using BurningKnight.entity.item.util;
using BurningKnight.util;
using Lens.entity;
using Lens.input;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class MeleeArcUse : ItemUse {
		protected float Damage;
		protected float LifeTime;
		protected int W;
		protected int H;
		protected float Angle;
		protected string AttackSound = "item_sword_attack";
		protected string HitSound = "item_sword_hit";
		protected float Knockback;
		
		public override void Use(Entity entity, Item item) {
			entity.GetComponent<AudioEmitterComponent>()!.EmitRandomizedPrefixed(AttackSound, 4);

			var arc = new MeleeArc {
				Owner = entity,
				LifeTime = LifeTime,
				Damage = Damage * (item.Scourged ? 1.5f : 1),
				Width = W,
				Height = H,
				Sound = HitSound,
				Position = entity.Center,
				Knockback = Knockback,
				Angle = entity.AngleTo(entity.GetComponent<AimComponent>()!.RealAim) + Angle
			};

			entity.HandleEvent(new MeleeArc.CreatedEvent {
				Arc = arc,
				Owner = entity,
				By = item
			});
			
			entity.Area.Add(arc);
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);

			Damage = settings["damage"].Number(1);
			W = settings["w"].Int(8);
			H = settings["h"].Int(24);
			LifeTime = settings["time"].Number(0.2f);
			Angle = settings["angle"].Number(0) * (float) Math.PI * 2;
			HitSound = settings["hs"].String("item_sword_hit");
			AttackSound = settings["as"].String("item_sword_attack");
			Knockback = settings["knockback"].Number(0f);
		}
	}
}