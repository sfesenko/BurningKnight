using System;
using BurningKnight.assets.achievements;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob.jungle;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.level;
using BurningKnight.level.entities;
using Lens.entity;
using Lens.util;
using Lens.util.camera;
using Lens.util.math;
using Lens.util.timer;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.mob.boss {
	public partial class QueenBee : Boss {
		public bool InSecondPhase {
			get {
				var p = GetComponent<HealthComponent>()!.Percent;
				return p > 0.33f && p <= 0.66f;
			}
		}

		public bool InThirdPhase => GetComponent<HealthComponent>()!.Percent <= 0.33f;
		public bool InFirstPhase => GetComponent<HealthComponent>()!.Percent > 0.66f;
		public int Phase => (InThirdPhase ? 3 : (InSecondPhase ? 2 : 1));

		private int lastPhase = 1;
		private float lastParticle;
		private int penetrateCount;
		private int attack;
		private bool changingPhase;

		public override void AddComponents() {
			base.AddComponents();

			Width = 23;
			Height = 19;
			
			AddComponent(new SensorBodyComponent(1, 4, 21, 14));

			var body = new RectBodyComponent(2, 17, 19, 1);
			AddComponent(body);

			body!.Body.LinearDamping = 3;

			AddAnimation("bigbee");
			SetMaxHp(340);

			Flying = true;
			Depth = Layers.FlyingMob;
		}

		protected override void AddPhases() {
			base.AddPhases();
			
			HealthBar!.AddPhase(0.33f);
			HealthBar.AddPhase(0.66f);
		}
		
		public override void Update(float dt) {
			base.Update(dt);

			lastParticle -= dt;

			if (lastParticle <= 0) {
				lastParticle = 0.1f;

				if (!IsFriendly()) {
					var s = GraphicsComponent!.Flipped ? -1 : 1;
					
					Area!.Add(new FireParticle {
						Offset = new Vector2(6 * s, -5.5f),
						Owner = this,
						Size = 0.5f,
						Depth = Depth + 1
					});

					Area.Add(new FireParticle {
						Offset = new Vector2(9 * s, -5.5f),
						Owner = this,
						Size = 0.5f,
						Depth = Depth + 1
					});

					if (Died) {
						return;
					}

					var p = Phase;

					if (lastPhase != p) {
						lastPhase = p;
						changingPhase = true;
						Become<ToCenterState>();
					}
				}
			}
		}

		public void ModifyBuilder(ProjectileBuilder builder) {
			if (InThirdPhase) {
				builder.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);
			}
		}
		
		public override void SelectAttack() {
			base.SelectAttack();
			Become<IdleState>();
		}

		protected override void OnTargetChange(Entity target) {
			base.OnTargetChange(target);

			if (target == null) {
				Become<FriendlyState>();
			}
		}
		
		public override bool ShouldCollide(Entity entity) {
			if (entity is Prop) {
				return false;
			}
			
			return base.ShouldCollide(entity) && !(entity is Level);
		}

		public override void PlaceRewards() {
			base.PlaceRewards();
			Achievements.Unlock("bk:sting_operation");
		}

		public override string GetScream() {
			return "queen_bee_scream";
		}
	}
}