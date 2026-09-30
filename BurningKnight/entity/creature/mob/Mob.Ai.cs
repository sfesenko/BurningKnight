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
	public partial class Mob {
		protected void FindTarget() {
			List<Entity> targets;

			if (TargetEverywhere) {
				targets = Area!.Tagged[IsFriendly() ? Tags.Mob : Tags.PlayerTarget];
			} else {
				var room = GetComponent<RoomComponent>()!.Room;

				if (room == null) {
					return;
				}
			
				targets = room.Tagged[IsFriendly() ? Tags.Mob : Tags.PlayerTarget];
			}
			
			var closestDistance = float.MaxValue;
			var friendly = IsFriendly();
			
			Entity closest = null;
			
			foreach (var target in targets) {
				if (target == this || target is bk.BurningKnight || ((Creature) target).IsFriendly() == friendly || 
				    (target.TryGetComponent<BuffsComponent>(out var b) && b.Has<InvisibleBuff>())) {
					
					continue;
				}
				
				var d = target.DistanceToSquared(this);

				if (d < closestDistance) {
					closestDistance = d;
					closest = target;
				}
			}

			if (Target != closest) {
				HandleEvent(new MobTargetChange {
					Mob = this,
					New = closest,
					Old = Target 
				});
			}			
			
			// Might be null, thats ok
			Target = closest;
			OnTargetChange(closest);
		}
		private void BuildPath(Vector2 to, bool back = false) {
			var level = Context.Level;
			var fp = level!.ToIndex((int) Math.Floor(CenterX / 16f), (int) Math.Floor(Bottom / 16f));
			var tp = level.ToIndex((int) Math.Floor(to.X / 16f), (int) Math.Floor(to.Y / 16f));

			var p = back ? PathFinder.GetStepBack(fp, tp, level.Passable, prevStepBack) : PathFinder.GetStep(fp, tp, level.Passable);

			if (back) {
				prevStepBack = lastStepBack;
				lastStepBack = p;
			}
			
			if (p == -1) {
				return;
			}
			
			NextPathPoint = new Vec2 {
				X = level.FromIndexX(p) * 16 + 8, 
				Y = level.FromIndexY(p) * 16 + 8
			};
		}
		public bool MoveTo(Vector2 point, float speed, float distance = 8f, bool back = false) {
			if (!back) {
				var ds = DistanceToFromBottom(point);

				if (ds <= distance) {
					return true;
				}
			} else {
				var ds = DistanceToFromBottom(point);

				if (ds >= distance) {
					return true;
				}
			}

			if (NextPathPoint == null) {
				BuildPath(point, back);

				if (NextPathPoint == null) {
					return false;
				}
			}

			var dx = NextPathPoint.X - CenterX;
			var dy = NextPathPoint.Y - Bottom;
			var d = (float) Math.Sqrt(dx * dx + dy * dy);

			if (d <= 2f) {
				NextPathPoint = null;
				return false;
			}

			speed *= Engine.Delta * 60;
			GetAnyComponent<BodyComponent>()!.Velocity = new Vector2(dx / d * speed, dy / d * speed);

			return false;
		}
		protected bool CanSeeTarget() {
			if (Target == null) {
				return false;
			}
			
			var min = 1f;
			var found = false;
			
			Physics.World!.RayCast((fixture, point, normal, fraction) => {
				if (min > fraction && fixture.Body.UserData is BodyComponent b && RayShouldCollide(b.Entity)) {
					min = fraction;
					found = true;
				}
				
				return min;
			}, Center, Target.Center);

			return !found;
		}
		protected void PushFromOtherEnemies(float dt, Func<Creature, bool> filter = null) {
			var room = GetComponent<RoomComponent>()!.Room;
			var body = GetAnyComponent<BodyComponent>();

			if (room == null || body == null) {
				return;
			}
			
			foreach (var m in room.Tagged[Tags.Mob]) {
				if (m == this) {
					continue;
				}
				
				var mob = (Creature) m;
				
				if (filter != null && !filter(mob)) {
					return;
				}

				var dx = DxTo(mob);
				var dy = DyTo(mob);
				var d = MathUtils.Distance(dx, dy);
				var force = dt * 800;
				
				if (d <= 8) {
					var a = MathUtils.Angle(dx, dy) - (float) Math.PI;
					body.Velocity += new Vector2((float) Math.Cos(a) * force, (float) Math.Sin(a) * force);
				}
			}
		}
		protected void PushOthersFromMe(float dt, Func<Creature, bool> filter = null) {
			var room = GetComponent<RoomComponent>()!.Room;

			if (room == null) {
				return;
			}

			foreach (var m in room.Tagged[Tags.Mob]) {
				if (m == this) {
					continue;
				}

				var mob = (Creature) m;

				if (filter != null && !filter(mob)) {
					return;
				}

				var dx = DxTo(mob);
				var dy = DyTo(mob);
				var d = MathUtils.Distance(dx, dy);
				var force = dt * 800;

				if (d <= 12) {
					var a = MathUtils.Angle(dx, dy) - (float) Math.PI;
					var b = mob.GetAnyComponent<BodyComponent>();

					if (b != null) {
						b.Velocity -= new Vector2((float) Math.Cos(a) * force, (float) Math.Sin(a) * force);
					}
				}
			}
		}
	}
}
