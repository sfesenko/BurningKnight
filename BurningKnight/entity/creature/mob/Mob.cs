using System;
using System.Collections.Generic;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.assets.particle;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.drop;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.creature.mob.prefix;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.entities;
using BurningKnight.level.paintings;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.level.variant;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.graphics;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.entity.creature.mob {
	public partial class Mob : Creature, DropModifier {
		public Entity Target;
		public bool HasPrefix => prefix != null;
		public Prefix Prefix => prefix;
		
		protected List<Entity> CollidingToHurt = new List<Entity>();
		protected int TouchDamage = 1;
		protected bool TargetEverywhere;
		
		private Prefix prefix;
		
		public override void AddComponents() {
			base.AddComponents();
			
			AddComponent(new AimComponent(AimComponent.AimType.Target));

			AlwaysActive = true;
			
			AddTag(Tags.Mob);
			AddTag(Tags.MustBeKilled);
			
			SetStats();
			
			AddDrops(new SingleDrop("bk:coin", LevelSave.GenerateShops ? 0.2f : 0.12f));
			AddDrops(new SingleDrop("bk:bomb", 0.03f));

			var h = GetComponent<HealthComponent>();
			h.InvincibilityTimerMax = 0.3f;
			h.PreventDamageInInvincibility = false;

			if (!(this is Boss)) {
				GetComponent<StateComponent>().Pause++;
			}
		}

		public override void PostInit() {
			base.PostInit();

			if (Context.Level?.Variant is SnowLevelVariant || Context.Level?.Biome is IceBiome) {
				GetComponent<BuffsComponent>().AddImmunity<FrozenBuff>();
			}
		}

		protected virtual void SetStats() {
			
		}

		protected void AddAnimation(string name, string layer = null) {
			AddComponent(new MobAnimationComponent(name, layer));
		}
		
		protected virtual void SetMaxHp(int hp) {
			if (Run.Loop > 0 && !(this is DM)) {
				hp *= (this is Boss ? 4 : 1) * (Run.Loop + 1);
			}
		
			var health = GetComponent<HealthComponent>();
			health.InitMaxHealth = hp;
		}

		protected virtual void OnTargetChange(Entity target) {
			if (target == null) {
				GetComponent<StateComponent>().PauseOnChange = true;
			} else {
				GetComponent<StateComponent>().PauseOnChange = false;
				GetComponent<StateComponent>().Pause = 0;
			}
		}
		
		private float lastParticle;

		public override void Update(float dt) {
			base.Update(dt);

			if (prefix != null) {
				prefix.Update(dt);

				lastParticle -= dt;

				if (lastParticle <= 0) {
					lastParticle = Rnd.Float(0.05f, 0.3f);

					for (var i = 0; i < Rnd.Int(0, 3); i++) {
						var part = new ParticleEntity(Particles.Scourge());

						part.Position = Center + Rnd.Vector(-4, 4);
						part.Particle.Scale = Rnd.Float(0.5f, 1.2f);
						Area.Add(part);
						part.Depth = 1;
					}
				}
			}

			if (Target == null) {
				FindTarget();
			} else if (Target.Done || Target.GetComponent<RoomComponent>().Room != GetComponent<RoomComponent>().Room ||
			           (Target is Creature c && c.IsFriendly() == IsFriendly()) || 
			           (Target.TryGetComponent<BuffsComponent>(out var b) && b.Has<InvisibleBuff>())) {

				var old = Target;

				HandleEvent(new MobTargetChange {
					Mob = this,
					New = null,
					Old = old 
				});
				
				FindTarget();
			}

			if (TouchDamage == 0) {
				return;
			}

			var raging = GetComponent<BuffsComponent>().Has<RageBuff>();
			
			for (var i = CollidingToHurt.Count - 1; i >= 0; i--) {
				var entity = CollidingToHurt[i];

				if (entity.Done) {
					CollidingToHurt.RemoveAt(i);
					continue;
				}

				if ((!(entity is Creature c) || c.IsFriendly() != IsFriendly())) {
					if (entity.GetComponent<HealthComponent>().ModifyHealth(-TouchDamage * (raging ? 2 : 1), this, DamageType.Contact)) {
						OnHit(entity);
					}
				}
			}

			if (GetComponent<RoomComponent>().Room == null) {
				Kill(null);
			}
		}

		protected virtual void OnHit(Entity e) {
			
		}

		private bool wasSlow;

		protected virtual bool CanHurt(Entity entity) {
			return !(entity is BreakableProp || entity is Painting || entity is Prop);
		}

		public override bool IsFriendly() {
			return GetComponent<BuffsComponent>().Has<CharmedBuff>();
		}

		private bool rotationApplied;

		public override void AnimateDeath(DiedEvent d) {
			base.AnimateDeath(d);
			CreateGore(d);
		}

		#region Path finding
		protected Vec2 NextPathPoint;
		private int lastStepBack;
		private int prevStepBack;

		public bool FlyTo(Vector2 point, float speed, float distance = 8f) {
			var dx = DxTo(point);
			var dy = DyTo(point);
			var d = (float) Math.Sqrt(dx * dx + dy * dy);

			if (d <= distance) {
				return true;
			}
			
			GetAnyComponent<BodyComponent>().Velocity = new Vector2(dx / d * speed, dy / d * speed);

			return false;
		}
		#endregion

		public override void Load(FileReader stream) {
			base.Load(stream);
			var str = stream.ReadString();

			if (str != null) {
				SetPrefix(str);
			}
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			stream.WriteString(prefix?.Id);
		}

		public void GeneratePrefix() {
			if (!Rnd.Chance(Run.Scourge * 10 + 0.5f)) {
				return;
			}

			var all = PrefixRegistry.Defined.Keys.ToArray();
			SetPrefix(all[Rnd.Int(all.Length)]);
		}

		public void SetPrefix(string id) {
			if (!PrefixRegistry.Defined.TryGetValue(id, out var t)) {
				return;
			}

			try {
				var p = (Prefix) Activator.CreateInstance(t);

				prefix = p;
				
				p.Id = id;
				p.Mob = this;
				p.Init();
			} catch (Exception e) {
				Log.Error(e);
				return;
			}
		}

		public override void RenderDebug() {
			base.RenderDebug();

			if (NextPathPoint != null) {
				Graphics.Batch.DrawLine(CenterX, Bottom, NextPathPoint.X, NextPathPoint.Y, Color.Red);
				Graphics.Batch.DrawLine(CenterX, Bottom, Context.Level.FromIndexX(prevStepBack) * 16 + 8, Context.Level.FromIndexY(prevStepBack) * 16 + 8, Color.Blue);
			}
		}

		private static bool RayShouldCollide(Entity entity) {
			return entity is ProjectileLevelBody;
		}

		protected void TurnToTarget() {
			if (Target != null) {
				GraphicsComponent.Flipped = Target.CenterX < CenterX;
			}
		}
	}
}