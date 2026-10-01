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
	public partial class BurningKnight {
		private void CheckCapture() {
			if (InFight) {
				return;
			}
			
			var room = Target?.GetComponent<RoomComponent>()?.Room;

			if (room != null && room.Type == RoomType.Boss) {
				foreach (var p in room.Tagged[Tags.Player]) {
					if (!((Player) p).Teleported) {
						return;
					}
				}

				if (Context.Level!.Biome is LibraryBiome) {
					BeginFight();
					return;
				}
			
				foreach (var mob in room.Tagged[Tags.Boss]) {
					if (mob != this && mob is Boss b && !(b is BurningKnight)) {
						captured = b;
						Become<CaptureState>();

						break;
					}
				}
			}
		}
		public override void PlaceRewards() {
			var head = new BkHead();
			Area!.Add(head);
			head.Center = Center;
			Audio.PlayMusic("Last chance");
		}
		protected override void CreateGore(DiedEvent? d) {
			
		}
		public bool InFight;
		private void AddOrbitals(int count) {
			for (var i = 0; i < count; i++) {
				var orbital = new BkOrbital {
					Id = i
				};
				
				Area!.Add(orbital);
				orbital.Center = Center;
				GetComponent<OrbitGiverComponent>()!.AddOrbiter(orbital);
			}
		}
		private void BeginFight() {
			if (InFight || Passive) {
				return;
			}
			
			var r = GetComponent<RoomComponent>()!.Room;

			if (r == null) {
				return;
			}
			
			GetComponent<DialogComponent>()!.StartAndClose("bk_12", 3);
			AddOrbitals(6);

			InFight = true;
			HasHealthbar = true;

			if (HealthBar == null) {
				HealthBar = new HealthBar(this);
				Engine.Instance.State.Ui.Add(HealthBar);
				AddPhases();
			}
			
			AddTag(Tags.Boss);
			AddTag(Tags.Mob);
			AddTag(Tags.MustBeKilled);

			var a = r.Tagged[Tags.MustBeKilled];

			if (!a.Contains(this)) {
				a.Add(this);
			}
			
			a = r.Tagged[Tags.Mob];

			if (!a.Contains(this)) {
				a.Add(this);
			}
			
			a = r.Tagged[Tags.Boss];

			if (!a.Contains(this)) {
				a.Add(this);
			}

			Become<FightState>();

			GetComponent<HealthComponent>()!.Unhittable = false;
			TouchDamage = 2;
			Center = Target!.GetComponent<RoomComponent>()!.Room!.Center;
		}
		protected override void Become<T>() {
			if (!Passive || typeof(T) == typeof(IdleState)) {
				base.Become<T>();
			}
		}
		protected override void AddPhases() {
			HealthBar!.AddPhase(0.5f);
		}
		private int count;
		public bool Raging => GetComponent<HealthComponent>()!.Percent <= 0.5f;
		private List<Laser> lasers = new List<Laser>();
		private float spinV;
		private int spinDir;
		private void WarnLaser(float angle, Vector2? offset = null) {
			var builder = new ProjectileBuilder(this, Raging ? "big" : "circle") {
				LightRadius = 32f,
				Color = ProjectileColor.Red
			};

			builder.RemoveFlags(ProjectileFlags.BreakableByMelee, ProjectileFlags.Reflectable);

			for (var i = 0; i < 3; i++) {
				Timer.Add(() => {
					var projectile = builder.Shoot(angle, Raging ? 15f : 10f).Build();
					projectile!.Center += MathUtils.CreateVector(angle, 8);

					if (offset != null) {
						projectile.Center += offset.Value;
					}
				}, i * 0.3f);
			}		
		}
		private void StartLasers() {
			lasers.Clear();
			spinV = 0;
			spinDir = Rnd.Chance() ? 1 : -1;

			Timer.Add(() => { 
				GetComponent<AudioEmitterComponent>()!.EmitRandomizedPrefixed("item_laser", 4);
			}, 1f);

			for (var i = 0; i < 4; i++) {
				var angle = AngleTo(Target!) + (i / 4f + 1 / 8f) * (float) Math.PI * 2f;

				WarnLaser(angle);

				Timer.Add(() => {
					var laser = Laser.Make(this, 0, 0, damage: 2, scale: 3, range: 64);
					laser.LifeTime = 10f;
					laser.Position = Center;
					laser.Angle = angle;

					lasers.Add(laser);
				}, 1f);
			}
		}
		private static string[] swordData = {
			" x     ",
			"xxxxxxx",
			" x     ",
		};
	}
}
