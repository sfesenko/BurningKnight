using BurningKnight.debug;
using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.controller;
using BurningKnight.assets.particle.custom;
using BurningKnight.assets.particle.renderer;
using BurningKnight.entity.bomb;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.bk;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.door;
using BurningKnight.entity.events;
using BurningKnight.entity.fx;
using BurningKnight.entity.item;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.projectile;
using BurningKnight.entity.room;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.entities;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui;
using BurningKnight.ui.dialog;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component;
using Lens.entity.component.logic;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.input;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.player {
	public partial class Player {
		public override bool ShouldCollide(Entity entity) {
			if (HasFlight && entity is HalfWall) {
				return false;
			}
			
			return !(entity is Player || entity is HalfProjectileLevel || entity is ProjectileLevelBody || ((entity is ItemStand || entity is Bomb) && InAir())) && base.ShouldCollide(entity);
		}
		public bool HasFlight;
		public bool SuperHot;
		public override bool InAir() {
			return HasFlight || base.InAir() || GetComponent<StateComponent>()!.StateInstance is RollState;
		}
		public override bool HasNoHealth(HealthModifiedEvent? e = null) {
			return base.HasNoHealth(e) && GetComponent<HeartsComponent>()!.Total == 0;
		}
		public override bool HasNoHealth(PostHealthModifiedEvent? e = null) {
			return base.HasNoHealth(e) && GetComponent<HeartsComponent>()!.Total == 0;
		}
		public override bool HandleEvent(Event e) {
			if (e is LostSupportEvent) {
				if (GetComponent<HealthComponent>()!.Unhittable) {
					return true;
				}

				if (!GetComponent<BuffsComponent>()!.PitImmunity) {
					GetComponent<HealthComponent>()!.ModifyHealth(-1, Context.Level);
				}
				

				for (var i = 0; i < 4; i++) {
					var part = new ParticleEntity(Particles.Dust());
						
					part.Position = Center;
					part.Particle.Scale = Rnd.Float(0.4f, 0.8f);
					Area!.Add(part);
				}
			} else if (e is RoomChangedEvent c) {
				if (c.New == null || Context.Level == null || Context.Camera == null) {
					return base.HandleEvent(e);
				}
				
				if (c.New.Tagged[Tags.MustBeKilled].Count > 0) {
					Audio.PlaySfx("level_door_shut");

					foreach (var p in Area!.Tagged[Tags.Player]) {
						if (p.GetComponent<RoomComponent>()!.Room != c.New) {
							AnimationUtil.Poof(p.Center);
							p.Center = Center;
							AnimationUtil.Poof(p.Center);
						}
					}
				}

				((InGameState) Engine.Instance.State).ResetFollowing();

				var pr = Engine.Instance.StateRenderer;

				if (c.Old != null) {
					if (Scourge.IsEnabled(Scourge.OfLost)) {
						c.Old.Hide();
					}
					
					if (c.Old.Type == RoomType.DarkMarket || c.Old.Type == RoomType.Hidden) {
						pr.EnableClip = false;
						c.Old.Hide(true);
						InBuilding = false;
						((InGameState) Engine.Instance.State).UpdateRainVolume();
					}
				}

				if (c.New.Type == RoomType.DarkMarket) {
					Achievements.Unlock("bk:dark_market");
				}
				
				if (c.New.Type == RoomType.DarkMarket || c.New.Type == RoomType.Hidden) {
					pr.EnableClip = true;
					pr.ClipPosition = new Vector2(c.New.X + 16, c.New.Y + 16);
					pr.ClipSize = new Vector2(c.New.Width - 16, c.New.Height - 32);
					InBuilding = true;
					((InGameState) Engine.Instance.State).UpdateRainVolume();
				} else {
					pr.EnableClip = false;
				}

				if (c.New.Type == RoomType.Shop) {
					Audio.PlaySfx("level_door_bell");
				}
				
				c.New.Discover();
				var level = Context.Level;

				if (InGameState.Ready) {
					switch (c.New.Type) {
						case RoomType.Secret:
						case RoomType.Special:
						case RoomType.Shop:
						case RoomType.SubShop:
						case RoomType.Treasure: {
							foreach (var door in c.New.Doors) {
								if (door.TryGetComponent<LockComponent>(out var component) && component.Lock is GoldLock) {
									if (!(c.New.Type == RoomType.Shop && ((door!.Rooms[0] != null && door.Rooms[0].Type == RoomType.SubShop) ||
									                                    (door.Rooms[1] != null && door.Rooms[1].Type == RoomType.SubShop)))) {
									
										component.Lock.SetLocked(false, this);
									} 
									
								}
							}

							break;
						}

						case RoomType.OldMan:
						case RoomType.Granny: {
							if (c.New.Type == RoomType.OldMan) {
								GetComponent<StatsComponent>()!.SawDeal = true;
							}
							
							c.New.OpenHiddenDoors();
							
							foreach (var r in Area!.Tagged[Tags.Room]) {
								var room = (Room) r;

								if (room.Type == (c.New.Type == RoomType.OldMan ? RoomType.Granny : RoomType.OldMan)) {
									room.CloseHiddenDoors();
									break;
								}
							}
							
							break;
						}
					}
					
					if (c.New.Type == RoomType.Secret) {
						ExplosionMaker.CheckForCracks(level, c.New, this);
					}
				}
				
				if (c.Old != null) {
					if (c.Old.Type == RoomType.OldMan) {
						var found = false;

						foreach (var p in c.Old.Tagged[Tags.Player]) {
							if (p != this && p is Player) {
								found = true;

								break;
							}
						}

						if (!found) {
							c.Old.CloseHiddenDoors();
						}
					} else if (c.Old.Type == RoomType.Treasure && Context.Run.Type != RunType.BossRush && !Rnd.Chance(5)) {
						var found = false;

						foreach (var p in c.Old.Tagged[Tags.Player]) {
							if (p != this && p is Player) {
								found = true;

								break;
							}
						}

						if (!found) {
							foreach (var door in c.Old.Doors) {
								var x = (int) Math.Floor(door.CenterX / 16);
								var y = (int) Math.Floor(door.Bottom / 16);
								var t = level.Get(x, y);

								if (level.Get(x, y).Matches(TileFlags.Passable)) {
									var index = level.ToIndex(x, y);

									level.Set(index, level.Biome is IceBiome ? Tile.WallB : Tile.WallA);
									level.UpdateTile(x, y);
									level.ReCreateBodyChunk(x, y);
									level.LoadPassable();

									Context.Camera!.Shake(10);
								}
							}

							c.Old.ApplyToEachTile((x, y) => {
								if (Context.Level!.Get(x, y).IsWall()) {
									return;
								}

								Timer.Add(() => {
									var part = new TileParticle();

									part.Top = Context!.Level!.Tileset.WallTopADecor;
									part.TopTarget = Context.Level!.Tileset.WallTopADecor;
									part.Side = Context.Level!.Tileset.FloorSidesD[0];
									part.Sides = Context.Level!.Tileset.WallSidesA[2];
									part.Tile = Tile.WallA;

									part.X = x * 16;
									part.Y = y * 16;
									part.Target.X = x * 16;
									part.Target.Y = y * 16;
									part.TargetZ = -8f;

									Area!.Add(part);
								}, Rnd.Float(0.5f));
							});

							foreach (var d in c.Old.Doors) {
								d.Done = true;
							}

							c.Old.Done = true;
						}
					}
				}

				// Darken the lighting in evil rooms
				if (c.New.Type == RoomType.OldMan || c.New.Type == RoomType.Spiked) {
					Tween.To(0.7f, Lights.RadiusMod, x => Lights.RadiusMod = x, 0.3f);
				} else if (c.Old != null && (c.Old.Type == RoomType.OldMan || c.Old.Type == RoomType.Spiked)) {
					Tween.To(1f, Lights.RadiusMod, x => Lights.RadiusMod = x, 0.3f);
				}
			} else if (e is HealthModifiedEvent hm) {
				if (hm.Amount < 0) {
					if ((hm.From is Mob m && m.HasPrefix) || (hm.From is creature.bk.BurningKnight k && k.InFight) || hm.From is BkOrbital) {
						hm.Amount = Math.Min(hm.Amount, -2);
					} else if (hm.Type != DamageType.Custom && hm.Type != DamageType.Explosive) {
						hm.Amount = Math.Max(-1, hm.Amount);
					}
				}			
			} else if (e is PostHealthModifiedEvent h) {
				if (h.Amount < 0 && !h.PressedForBomb) {
					HandleEvent(new PlayerHurtEvent {
						Player = this
					});

					var hp = GetComponent<HealthComponent>()!.Health + GetComponent<HeartsComponent>()!.Total;

					if (hp > 0) {
						if (h.HealthType == HealthType.Shield) {
							Audio.PlaySfx("player_shield_hurt", 1f);
						} else {
							Audio.PlaySfx(hp < 2 ? "player_low_hp_hurt" : "player_hurt", 1f);
						}
					}

					if (Settings.Blood) {
						var cl = GetBloodColor();

						if (Rnd.Chance(30)) {
							for (var i = 0; i < Rnd.Int(1, 3); i++) {
								Area!.Add(new SplashParticle {
									Position = Center - new Vector2(2.5f),
									Color = cl
								});
							}
						}

						Area!.Add(new SplashFx {
							Position = Center,
							Color = ColorUtils.Mod(cl)
						});
					}
				}
			} else if (e is RoomClearedEvent rce) {
				Context.Camera!.Unfollow(rce.Room);
				Audio.PlaySfx("level_room_cleared", 0.25f + Audio.Db3);

				if (Context.Run.Depth > 0 && !alerted && CheckClear(Area)) {
					alerted = true;
					AnimationUtil.Confetti(Center);
					Audio.PlaySfx("level_cleared");
				}
			} else if (e is NewLevelStartedEvent) {
				GetComponent<HealthComponent>()!.Unhittable = true;
			} else if (e is ProjectileCreatedEvent pce) {
				if (Flying || HasFlight) {
					pce.Projectile.AddFlags(ProjectileFlags.FlyOverStones);
				}
			} else if (e is FlagCollisionStartEvent fcse) {
				if (fcse.Flag == Flag.Burning) {
					GetComponent<HealthComponent>()!.ModifyHealth(-1, Context.Level);
				}
			} else if (e is RevivedEvent re) {
				AnimationUtil.TeleportAway(this, () => {
					FindSpawn();
					Context.Camera!.Jump();
					AnimationUtil.TeleportIn(this);
				});
			} else if (e is CollisionStartedEvent cse) {
				if (ItemDamage && cse.Entity is Item) {
					GetComponent<HealthComponent>()!.ModifyHealth(-1, cse.Entity, DamageType.Custom);
				}
			}
			
			return base.HandleEvent(e);
		}
		private bool alerted;
		public static bool CheckClear(Area area) {
			foreach (var r in area.Tagged[Tags.Room]) {
				var room = (Room) r;

				if ((room.Type == RoomType.Regular) && !room.Cleared) {
					return false;
				}
			}
			
			return true;
		}
		public override bool ShouldCollideWithDestroyableInAir() {
			return !HasFlight;
		}
		public override bool IgnoresProjectiles() {
			return GetComponent<StateComponent>()!.StateInstance is RollState;
		}
	}
}
