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
		public override bool HandleEvent(Event e) {
			if (prefix != null && prefix.HandleEvent(e)) {
				e.Handled = true;
			}
			
			if (e is BuffAddedEvent add && add.Buff is CharmedBuff || e is BuffRemovedEvent del && del.Buff is CharmedBuff) {
				// If old target even was a thing, it was from wrong category
				FindTarget();
			} else if (e is CollisionStartedEvent collisionStart) {
				if (collisionStart.Entity.HasComponent<HealthComponent>() && CanHurt(collisionStart.Entity)) {
					CollidingToHurt.Add(collisionStart.Entity);
				}
			} else if (e is CollisionEndedEvent collisionEnd) {
				if (collisionEnd.Entity.HasComponent<HealthComponent>()) {
					CollidingToHurt.Remove(collisionEnd.Entity);
				}
			} else if (e is DiedEvent de) {
				var who = de.From;
				
				if (de.From != null) {
					if (de.From.TryGetComponent<OwnerComponent>(out var o)) {
						who = o.Owner;
					} else if (who is Projectile p) {
						who = p.Owner;
					}
				}

				if (who is Player && who.GetComponent<LampComponent>().Item?.Id == "bk:explosive_lamp") {
					AddDrops(new SimpleDrop(1f, 1, 1, "bk:bomb"));
				}

				if (!de.BlockClear) {
					GetComponent<RoomComponent>().Room?.CheckCleared(who);
				}
			} else if (e is HealthModifiedEvent hme && hme.Amount < 0) {
				if (!(this is bk.BurningKnight) && TryGetComponent<RoomComponent>(out var room) && room.Room != null && room.Room.Tagged[Tags.Player].Count == 0) {
					return true;
				}

				if (!rotationApplied) {
					rotationApplied = true;
					var a = GetAnyComponent<AnimationComponent>();
				
					if (a != null) {
						var w = a.Angle;
						a.Angle += 0.5f;

						var t = Tween.To(w, a.Angle, x => a.Angle = x, 0.2f);
						
						t.Delay = 0.2f;
						t.OnEnd = () => {
							rotationApplied = false;
						};
					}
				}
			} else if (e is TileCollisionStartEvent tce) {
				if (tce.Tile == Tile.Cobweb) {
					var body = GetAnyComponent<BodyComponent>();
					wasSlow = body.Slow;
					body.Slow = true;
				}
			} else if (e is TileCollisionEndEvent tee) {
				if (tee.Tile == Tile.Cobweb) {
					var body = GetAnyComponent<BodyComponent>();

					if (!wasSlow && body.Slow && !GetComponent<BuffsComponent>().Has<SlowBuff>()) {
						body.Slow = false;
					}
				}
			}  
			
			return base.HandleEvent(e);
		}
		public void ModifyDrops(List<Item> drops) {
			if (Rnd.Chance(Run.Scourge * 0.5f)) {
				var c = Rnd.Int(0, 3);
				
				for (var i = 0; i < c; i++) {
					drops.Add(Items.Create("bk:copper_coin"));
				}
			}

			foreach (var p in Area.Tagged[Tags.Player]) {
				if (p.GetComponent<LampComponent>().Item?.Id == "bk:explosive_lamp") {
					drops.Add(Items.Create("bk:bomb"));
					break;
				}
			}
		}
	}
}
