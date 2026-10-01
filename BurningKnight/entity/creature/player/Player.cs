using BurningKnight.debug;
using System;
using System.Diagnostics.CodeAnalysis;
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
	public partial class Player : Creature, DropModifier {
		private static Color tint = new Color(50, 234, 60, 200);

		public const int MaxPlayers = 5;

		public static Color[] IndexTints = {
			Palette.Default[59],
			Palette.Default[42],
			Palette.Default[35],
			Palette.Default[55],
			Palette.Default[31]
		};

		public static Vector4[] VectorTints;

		static Player() {
			VectorTints = new Vector4[MaxPlayers];
			
			for (var i = 0; i < MaxPlayers; i++) {
				var color = IndexTints[i];
				VectorTints[i] = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f);
			}
		}
		
		public Color Tint => IndexTints[GetComponent<InputComponent>()!.Index];
		
		public static int Quacks;
		public static bool ToBoss;
		public static bool InBuilding;
		public static Color LightColor = new Color(1f, 0.8f, 0.6f, 1f);
		
		public static readonly string[] StartingWeapons = new string[MaxPlayers];
		public static readonly string[] StartingItems = new string[MaxPlayers];
		public static readonly string[] StartingLamps = new string[MaxPlayers];
		public static List<string> DailyItems = null!;
		public string ProjectileTexture = "rect";

		public bool ItemDamage;
		public bool Sliding;

		public bool Dead;

		public void AnimateItemPickup(Item item, Action? action = null, bool add = true, bool ban = true) {
			if (ban) {
				var banner = new UiDescriptionBanner();
				banner.Show(item);
				Engine.Instance.State.Ui.Add(banner);
			}

			if (item.Type == ItemType.Weapon && Context.Run.Depth == 0) {
				Audio.PlaySfx(item.Data.WeaponType.GetPickupSfx());
			} else {
				GetComponent<AudioEmitterComponent>()!.EmitRandomized("item_pickup");
			}

			if (add || item.Type == ItemType.Lamp || item.Type == ItemType.Active || item.Type == ItemType.ConsumableArtifact || item.Type == ItemType.Weapon || item.Type == ItemType.Hat) {
				GetComponent<InventoryComponent>()!.Busy = true;
				
				Engine.Instance.State.Ui.Add(new ConsumableParticle(item.Region, this, item.Type != ItemType.Active, () => {
					item.Area?.Remove(item);
					item.Done = false;
					
					action?.Invoke();
					GetComponent<InventoryComponent>()!.Busy = false;

					if (item.Type != ItemType.ConsumableArtifact && item.Type != ItemType.Active && item.Type != ItemType.Weapon && item.Type != ItemType.Hat && item.Type != ItemType.Lamp) {
						GetComponent<InventoryComponent>()!.Add(item);
					}
				}));
				
				return;
			}
		}
		
		public override void AddComponents() {
			base.AddComponents();

			AddComponent(new InputComponent());
			AddComponent(new CursorComponent());
			
			InBuilding = false;
			GetComponent<HealthComponent>()!.SaveMaxHp = true;
			
			Height = 11;
			
			// Graphics
			if (Context.Run.Depth != 0) {
				AddComponent(new LightComponent(this, 64, LightColor));
			}

			AddComponent(new PlayerGraphicsComponent {
				Offset = new Vector2(0, -5)
			});
			
			// Inventory
			AddComponent(new LampComponent());
			AddComponent(new InventoryComponent());
			AddComponent(new ActiveItemComponent());
			AddComponent(new ActiveWeaponComponent());
			AddComponent(new WeaponComponent());
			AddComponent(new ConsumablesComponent());
			AddComponent(new HatComponent());
			
			// Stats
			AddComponent(new ManaComponent());
			AddComponent(new HeartsComponent());
			AddComponent(new StatsComponent());

			// Collisions
			AddComponent(new RectBodyComponent(4, Height - 1, 8, 1) {
				CanCollide = false
			});
			
			AddComponent(new SensorBodyComponent(2, 1, Width - 4, Height - 1, BodyType.Dynamic, true));
			GetComponent<SensorBodyComponent>()!.Body!.SleepingAllowed = false;
			
			AddComponent(new InteractorComponent {
				CanInteractCallback = e => !died && !GetComponent<InventoryComponent>()!.Busy
			});

			// Other mechanics
			AddComponent(new OrbitGiverComponent());
			AddComponent(new FollowerComponent());
			AddComponent(new AimComponent(AimComponent.AimType.Cursor));
			AddComponent(new DialogComponent());
			
			AddComponent(new ZComponent());
			
			GetComponent<StateComponent>()!.Become<IdleState>();
			
			AddTag(Tags.Player);
			AddTag(Tags.PlayerTarget);
			AddTag(Tags.PlayerSave);
			RemoveTag(Tags.LevelSave);

			AlwaysActive = true;

			InitStats(true);
			
			Subscribe<NewFloorEvent>();
		}

		public void InitStats(bool fromInit = false) {
			HasFlight = false;
			SuperHot = false;

			Scourge.Clear();

			GetComponent<AimComponent>()!.ShowLaserLine = false;
			GetComponent<OrbitGiverComponent>()!.DestroyAll();
			GetComponent<FollowerComponent>()!.DestroyAll();

			var hp = GetComponent<HealthComponent>();

			if (fromInit) {
				hp!.InitMaxHealth = 6;
			}

			hp!.MaxHealthCap = 32;
			hp.InvincibilityTimerMax = 1f;

			if (CheatWindow.AutoGodMode) {
				Log.Info("Entering god mode for the player");
				hp.Unhittable = true;
			}
		}

		public override void Update(float dt) {
			if (findASpawn) {
				if (FindSpawn()) {
					Teleported = true;
					findASpawn = false;
					
					if (!CheatWindow.AutoGodMode) {
						GetComponent<HealthComponent>()!.Unhittable = false;
					}
				} else {
					Log.Error("Did not find a spawn point!");
				}
			}
			
			base.Update(dt);
			t += dt;

			if (!set && t >= 0.3f) {
				set = true;
				GetComponent<RoomComponent>()!.Room?.Discover();

				if (Context.Run.Depth == 0) {
					CageLock.CheckProgress();
					HatStand.CheckHats();
					Items.CheckForCollector();
					Builder.CheckShortcutUnlocks();

					if (Assets.FailedToLoadAudio) {
						Assets.FailedToLoadAudio = false;
						((InGameState) Engine.Instance.State).TopUi.Add(new UiError("Audio Failed",	"Failed to init audio device"));
					}
				}
			}
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			stream.WriteInt32(lastDepth);
		}

		public override void Load(FileReader stream) {
			base.Load(stream);
			lastDepth = stream.ReadInt32();
		}

		public void ModifyDrops(List<Item> drops) {
			if (!Dead) {
				return;
			}

			var inventory = GetComponent<InventoryComponent>();

			foreach (var i in inventory!.Items) {
				if (i.Id != "bk:no_lamp") {
					drops.Add(Items.Create(i.Id)!);
				}
			}

			foreach (var c in Components.Values) {
				if (c is ItemComponent i && i.Item != null && i.Item.Type != ItemType.Hat && i.Item.Id != "bk:no_lamp") {
					drops.Add(Items.Create(i.Item.Id)!);
				}
			}
		}

		public override void Destroy() {
			base.Destroy();

			if (!GetComponent<HealthComponent>()!.Dead && (Context.Run.LastDepth == -1 || Context.Run.LastDepth == 0)) {
				var index = GetComponent<InputComponent>()!.Index;
				
				StartingWeapons[index] = GetComponent<ActiveWeaponComponent>()!.Item?.Id!;
				StartingItems[index] = GetComponent<ActiveItemComponent>()!.Item?.Id!;
				StartingLamps[index] = GetComponent<LampComponent>()!.Item?.Id!;
			}
		}

		protected override string? GetHurtSfx() {
			return null;
		}

		protected override string? GetDeadSfx() {
			return null;
		}

		[return: MaybeNull]
		public T ForceGetComponent<T>() where T : Component {
			return base.GetComponent<T>();
		}

		[return: MaybeNull]
		public override T GetComponent<T>() {
			var minIndex = 1024;
			var pl = this;
			
			if (InGameState.Multiplayer && typeof(T) == typeof(ConsumablesComponent) && base.GetComponent<InputComponent>()!.Index != 0) {
				foreach (var p in Area!.Tagged[Tags.Player]) {
					var i = p.GetComponent<InputComponent>()!.Index;
					
					if (i == 0) {
						return p.GetComponent<T>();
					}

					if (i < minIndex) {
						minIndex = i;
						pl = (Player) p;
					}
				}
			}
			
			return pl.ForceGetComponent<T>();
		}
	}
}