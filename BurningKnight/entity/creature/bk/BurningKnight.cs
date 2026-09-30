using System;
using System.Collections.Generic;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.creature.mob.castle;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.rooms;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui;
using BurningKnight.ui.dialog;
using BurningKnight.util;
using Lens;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Lens.physics;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace BurningKnight.entity.creature.bk {
	public partial class BurningKnight : Boss {
		private static Color tint = new Color(234, 50, 60, 200);
		private Boss captured = null!;
		private bool raging;
		private int timesRaged;

		public bool Passive;
		public bool Hidden => GetComponent<StateComponent>()!.StateInstance is HiddenState;

		public override void AddComponents() {
			base.AddComponents();

			AddTag(Tags.BurningKnight);
			AddTag(Tags.PlayerSave);

			RemoveTag(Tags.Boss);
			RemoveTag(Tags.Mob);
			RemoveTag(Tags.LevelSave);
			RemoveTag(Tags.MustBeKilled);

			Width = 22;
			Height = 27;
			Flying = true;
			TargetEverywhere = true;
			AlwaysActive = true;
			AlwaysVisible = true;
			HasHealthbar = false;
			Depth = Layers.Bk;
			TouchDamage = 0;
			
			var b = new RectBodyComponent(6, 8, 10, 15, BodyType.Dynamic, true) {
				KnockbackModifier = 0
			};

			AddComponent(b);
			b!.Body.LinearDamping = 3;

			AddComponent(new BkGraphicsComponent("old_burning_knight"));

			var health = GetComponent<HealthComponent>();
			health!.Unhittable = true;
			SetMaxHp(300);
			// health.AutoKill = false;

			GetComponent<StateComponent>()!.Become<IdleState>();
			AddComponent(new OrbitGiverComponent());

			AddComponent(new LightComponent(this, 64, new Color(1f, 0.2f, 0.1f, 0.5f)));

			var buffs = GetComponent<BuffsComponent>();

			buffs!.AddImmunity<FrozenBuff>();
			buffs.AddImmunity<BurningBuff>();
			buffs.AddImmunity<BleedingBuff>();

			Subscribe<RoomChangedEvent>();
			Subscribe<ItemTakenEvent>();
			Subscribe<Dialog.EndedEvent>();
			Subscribe<ShopNpc.SavedEvent>();
			Subscribe<ShopKeeper.EnragedEvent>();
			Subscribe<DiedEvent>();
			Subscribe<SecretRoomFoundEvent>();
			Subscribe<DefeatedEvent>();
			Subscribe<NewLevelStartedEvent>();

			GetComponent<DialogComponent>()!.Dialog.Voice = 25;
			AddComponent(new AimComponent(AimComponent.AimType.Target));
		}

		public override void PostInit() {
			base.PostInit();
			
			Timer.Add(() => {
				GetComponent<AudioEmitterComponent>()!.Emit("mob_bk_hovering_loop", 0.3f, looped: true, tween: true);
				GetComponent<AudioEmitterComponent>()!.Emit(Context.Run.Depth == 10 ? "mob_bk_fight_loop" : "mob_bk_flame_loop",  0.3f, looped: true, tween: true);
			}, 2f);
		}

		public override void Destroy() {
			base.Destroy();
			GetComponent<AudioEmitterComponent>()!.StopAll();
		}

		protected override void OnTargetChange(Entity target) {
			if (Hidden) {
				return;
			}

			if (!Awoken && target != null) {
				Awoken = true;
				Become<FollowState>();
			} else if (target == null) {
				Become<IdleState>();
				Awoken = false;
			}

			base.OnTargetChange(target);
		}

		private void FreeSelf() {
			if (!Hidden) {
				return;
			}

			var graphics = GetComponent<BkGraphicsComponent>();
			graphics!.Alpha = 0;

			Center = captured.Center;
			GetComponent<HealthComponent>()!.Unhittable = true;

			Tween.To(1, graphics.Alpha, x => graphics.Alpha = x, 0.3f).OnEnd = () => {
				GetComponent<AudioEmitterComponent>()!.Emit("mob_bk_roar_4", 0.8f);
									
				Timer.Add(() => {
					if (captured.Done) {
						Become<ChaseState>();

						// YOU CAN'T DEFEAT THE BURNING KNIGHT!!!
						GetComponent<DialogComponent>()!.StartAndClose("bk_2", 5);
					} else {
						captured = null;
						Become<FollowState>();
						CheckForScourgeRage();
					}
				}, 1f);
			};
		}

		public override bool HandleEvent(Event e) {
			if (e is DefeatedEvent bde) {
				if (bde.Boss == captured) {
					FreeSelf();
				}

				if (bde.Boss == this) {
					return base.HandleEvent(e);
				}

				return false;
			}

			if (Hidden) {
				return base.HandleEvent(e);
			}

			if (e is RoomChangedEvent rce) {
				if (!InFight) {
					var p = rce.Who is Player;
					var bs = rce.Who is BurningKnight;

					if ((p || bs) && rce.New != null) {
						var t = rce.New.Type;

						if (t == RoomType.Boss) {
							CheckCapture();
						} else if (p) {
							if (t == RoomType.Treasure) {
								foreach (var item in rce.New.Tagged[Tags.Item]) {
									if (item is SingleChoiceStand stand && stand.Item != null) {
										GetComponent<DialogComponent>()!.StartAndClose("bk_0", 5);

										break;
									}
								}
							} else if (t == RoomType.Granny) {
								// GRANNY, CAN YOU JUST DIE, PLEASE??
								GetComponent<DialogComponent>()!.StartAndClose("bk_9", 3);
							} else if (t == RoomType.OldMan) {
								// MY MASTER, I BROUGHT THE GOBLIN
								GetComponent<DialogComponent>()!.StartAndClose("bk_10", 5);
							}
						}
					}
				}
			} else if (e is ItemTakenEvent ite) {
				if (!InFight && ite.Stand is SingleChoiceStand && ite.Who is Player) {
					GetComponent<DialogComponent>()!.StartAndClose("bk_1", 5);
					var state = GetComponent<StateComponent>();

					if (!(state!.StateInstance is HiddenState)) {
						Timer.Add(() => {
							timesRaged++;
							GetComponent<AudioEmitterComponent>()!.Emit("mob_bk_roar_1", 0.8f);
							state.Become<AttackState>();
						}, 3);
					}
				}
			} else if (e is Dialog.EndedEvent dse) {
				if (!InFight && dse.Owner is ShopKeeper && dse.Dialog.Id == "shopkeeper_18") {
					// What a joke
					Timer.Add(() => { GetComponent<DialogComponent>()!.StartAndClose("bk_4", 5); }, 1);
				}
			} else if (e is ShopNpc.SavedEvent) {
				// I WOULDN'T BOTHER EVEN TALKING TO THEM
				Timer.Add(() => { GetComponent<DialogComponent>()!.StartAndClose("bk_5", 5); }, 2f);
			} else if (e is ShopKeeper.EnragedEvent skee) {
				if (skee!.ShopKeeper.GetComponent<RoomComponent>()!.Room.Explored) {
					// KILL HIM, EDWARD!
					GetComponent<DialogComponent>()!.StartAndClose("bk_6", 5);
				}
			} else if (e is DiedEvent de) {
				if (de.Who is ShopKeeper) {
					// EDWARD, NOOOOOO!
					GetComponent<DialogComponent>()!.StartAndClose("bk_7", 5);
					return false;
				} else if (de.Who == this) {
					return base.HandleEvent(e);
				} else {
					return false;
				}
			} else if (e is SecretRoomFoundEvent) {
				// OH COMON, STOP EXPLODING MY CASTLE!
				GetComponent<DialogComponent>()!.StartAndClose("bk_8", 5);
			} else if (e is NewLevelStartedEvent) {
				if (!InFight) {
					CheckForScourgeRage();
					var state = GetComponent<StateComponent>()!.StateInstance;

					if (raging && !(state is AttackState || state is ChaseState || state is FlyAwayAttackingState)) {
						raging = false;
						sayNoRage = true;
					}
				}
			}

			return base.HandleEvent(e);
		}

		
		private bool sayNoRage;

		public override void Load(FileReader stream) {
			base.Load(stream);
			raging = stream.ReadBoolean();
			timesRaged = stream.ReadInt32();
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			stream.WriteBoolean(raging);
			stream.WriteInt32(timesRaged);
		}

		private float lastFadingParticle;

		public override void Update(float dt) {
			base.Update(dt);

			if (!Placed) {
				Done = false;
			}

			if (Hidden) {
				return;
			}

			if (lasers.Count > 0) {
				spinV += dt;

				foreach (var l in lasers) {
					if (l.Done) {
						lasers.Clear();
						break;
					}

					l.Angle += spinV * dt * 0.2f * spinDir;
				}
			}

			lastFadingParticle -= dt;

			if (lastFadingParticle <= 0 && !(GetComponent<StateComponent>()!.StateInstance is FlameAttack)) {
				lastFadingParticle = 0.2f;

				var particle = new FadingParticle(GetComponent<BkGraphicsComponent>()!.Animation.GetCurrentTexture(), tint);
				Area!.Add(particle);

				particle.Depth = Depth - 1;
				particle.Center = Center;

				var room = GetComponent<RoomComponent>()!.Room;

				if (room != null && room.Type == RoomType.Boss) {
					CheckCapture();
				} else if (Target != null) {
					room = Target.GetComponent<RoomComponent>()!.Room;

					if (room != null && room.Type == RoomType.Boss) {
						CheckCapture();
					}
				}
			}
		}

		public bool ForcedRage;

		public void CheckForScourgeRage() {
			if (InFight) {
				return;
			}
			
			var s = GetComponent<StateComponent>()!.StateInstance;

			if (s is ChaseState || s is AttackState || s is HiddenState) {
				return;
			}
			
			if (ForcedRage || Context.Run.Scourge >= 10) {
				Become<ChaseState>();
			}
		}

		private void CheckForScourgeRageFree() {
			if (InFight) {
				return;
			}
			
			if (Target == null) {
				Become<IdleState>();
			}
			
			var s = GetComponent<StateComponent>()!.StateInstance;

			if (s is IdleState || s is ChaseState || s is FollowState || s is HiddenState || s is FlyAwayAttackingState || s is AttackState) {
				return;
			}
			
			if (!ForcedRage && Context.Run.Scourge < 10) {
				Become<FollowState>();
			}
		}

		/*
		 * The actual boss battle
		 */
	}
}