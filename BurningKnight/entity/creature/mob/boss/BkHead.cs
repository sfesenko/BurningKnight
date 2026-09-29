using System;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.bk;
using BurningKnight.entity.creature.mob.castle;
using BurningKnight.entity.creature.mob.desert;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.cutscene.entity;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.state;
using BurningKnight.ui.dialog;
using BurningKnight.util;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.camera;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.mob.boss {
	public partial class BkHead : Boss {
		public bool CanBeSaved => GetComponent<HealthComponent>().Percent <= 0.2f;
		
		protected override void AddPhases() {
			base.AddPhases();
			HealthBar.AddPhase(0.2f);
		}

		public override void AddComponents() {
			base.AddComponents();
			
			AddComponent(new BkGraphicsComponent("demon"));
			AddComponent(new RectBodyComponent(2, 4, 12, 15, BodyType.Dynamic, true));
			AddComponent(new AimComponent(AimComponent.AimType.Target));

			var b = GetComponent<RectBodyComponent>();
			b.Body.LinearDamping = 2;
			b.KnockbackModifier = 0;
			
			SetMaxHp(600);

			Depth = Layers.FlyingMob;
			Awoken = true;
		}

		protected override void OnTargetChange(Entity target) {
			base.OnTargetChange(target);

			if (target != null) {
				GetComponent<DialogComponent>().StartAndClose("head_0", 2f);

				Timer.Add(() => {
					Become<IdleState>();
				}, 1);
			}
		}

		public override void SelectAttack() {
			base.SelectAttack();
			Become<IdleState>();
		}

		private float t;

		public override void Update(float dt) {
			base.Update(dt);

			t += dt;
			
			if (Target != null && !Died) {
				var force = 40f * dt;
				var a = AngleTo(Target);

				GetComponent<RectBodyComponent>().Velocity += new Vector2((float) Math.Cos(a) * force, (float) Math.Sin(a) * force);
			}
		}

		private int counter;

		private void WarnLaser(float angle, Vector2? offset = null) {
			var builder = new ProjectileBuilder(this, "circle") {
				LightRadius = 32f
			};

			builder.RemoveFlags(ProjectileFlags.BreakableByMelee, ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

			var projectile = builder.Shoot(angle, 20f).Build();

			projectile.Center += MathUtils.CreateVector(angle, 8);

			if (offset != null) {
				projectile.Center += offset.Value;
			}
		}

		public override void PlaceRewards() {
			if (saved) {
				base.PlaceRewards();
			} else {
				ResetCam = false;
			}
			
			Achievements.Unlock("bk:bk_no_more");
		}

		protected override TextureRegion GetDeathFrame() {
			return CommonAse.Particles.GetSlice("old_gobbo");
		}

		private bool saved;

		public void Save() {
			if (saved || Died) {
				return;
			}

			saved = true;
			GetComponent<HealthComponent>().Kill(this);

			Timer.Add(PlaceRewards, 1f);
		}
		
		protected override void CreateGore(DiedEvent d) {
			base.CreateGore(d);

			if (saved) {
				return;
			}
			
			var heinur = new Heinur();
			Area.Add(heinur);
			heinur.Center = Center - new Vector2(0, 32);

			var g = heinur.GetComponent<BkGraphicsComponent>();
			
			g.Scale = Vector2.Zero;
			
			Timer.Add(() => {
				Tween.To(1, 0, x => g.Scale.X = x, 3f);
				Tween.To(1, 0, x => g.Scale.Y = x, 3f);
			}, 1f);

			var dm = new DarkMage();
			Area.Add(dm);

			dm.Center = Center + new Vector2(0, 32);
			dm.GetComponent<AnimationComponent>().Animate();

			AnimationUtil.Poof(dm.Center);
			
			var dmDialog = dm.GetComponent<DialogComponent>();
			var heinurDialog = heinur.GetComponent<DialogComponent>();
			
			foreach (var p in Area.Tagged[Tags.Player]) {
				p.RemoveComponent<PlayerInputComponent>();
			}
			
			Camera.Instance.Targets.Clear();
			Camera.Instance.Follow(dm, 1f);
			Camera.Instance.Follow(heinur, 1f);
			
			dmDialog.Start("dm_5", null, () => Timer.Add(() => {
				dmDialog.Close();
				Camera.Instance.Targets.Clear();
				Camera.Instance.Follow(dm, 1f);
				Camera.Instance.Follow(heinur, 1f);
				
				heinurDialog.Start("heinur_0", null, () => Timer.Add(() => {
					heinurDialog.Close();
					heinur.Attract = true;
					Camera.Instance.Targets.Clear();
					Camera.Instance.Follow(dm, 1f);
					Camera.Instance.Follow(heinur, 1f);

					heinur.Callback = () => {
						Camera.Instance.Targets.Clear();
						Camera.Instance.Follow(dm, 1f);
						Camera.Instance.MainTarget = dm;

						foreach (var p in Area.Tagged[Tags.Player]) {
							p.GetComponent<PlayerGraphicsComponent>().Hidden = true;
							p.RemoveComponent<RectBodyComponent>();
						}
						
						var bk = new bk.BurningKnight() {
							Passive = true
						};
						
						Area.Add(bk);
						bk.Center = Center;

						bk.GetComponent<BkGraphicsComponent>().Animate();
						Camera.Instance.Follow(bk, 1f);
						
						dmDialog.Start("dm_6", null, () => Timer.Add(() => {
							dmDialog.Close();
							Camera.Instance.Targets.Clear();
							Camera.Instance.Follow(bk, 1f);
							
							var nbkDialog = bk.GetComponent<DialogComponent>();
						
							nbkDialog.Start("nbk_0", null, () => Timer.Add(() => {
								nbkDialog.Close();
								Camera.Instance.Targets.Clear();
								Camera.Instance.Follow(bk, 1f);
								Run.Win();
							}, 2f));
						}, 2f));
					};
				}, 1f));
			}, 1f));
		}
	}
}