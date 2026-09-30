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
		private int lastDepth = -3;
		public void FindSpawnPoint() {
			if (Context.Run.StartedNew && Context.Run.Depth > 0) {
				var index = GetComponent<InputComponent>().Index;
				
				if (StartingLamps[index] != null) {
					var i = Items.CreateAndAdd(StartingLamps[index], Area);
					i.Scourged = false;
					GetComponent<LampComponent>().Set(i, false);
					Log.Debug($"Starting lamp: {StartingLamps[index]}");
				}
				
				if (StartingWeapons[index] == null || !ItemPool.StartingWeapon.Contains(Items.Datas[StartingWeapons[index]].Pools)) {
					StartingWeapons[index] = Items.Generate(ItemPool.StartingWeapon, item => Item.Unlocked(item.Id));
				}

				if (StartingWeapons[index] != null) {
					var i = Items.CreateAndAdd(StartingWeapons[index], Area);
					i.Scourged = false;

					var l = GetComponent<LampComponent>().Item;

					if (l != null && l.Id == "bk:sharp_lamp" && i.Data.WeaponType != WeaponType.Melee) {
						StartingWeapons[index] = Items.Generate(ItemPool.StartingWeapon, item => Item.Unlocked(item.Id));
						i.Done = true;
						i = Items.CreateAndAdd(StartingWeapons[index], Area);
					}
					
					GetComponent<ActiveWeaponComponent>().Set(i, false);
					Log.Debug($"Starting weapon: {StartingWeapons[index]}");
				}
				
				if (StartingItems[index] != null) {
					var i = Items.CreateAndAdd(StartingItems[index], Area);
					i.Scourged = false;
					GetComponent<ActiveItemComponent>().Set(i, false);
					
					Log.Debug($"Starting item: {StartingItems[index]}");
				}

				if (DailyItems != null) {
					if (Context.Run.Type == RunType.Daily) {
						var inventory = GetComponent<InventoryComponent>();
						
						foreach (var id in DailyItems) {
							Log.Info($"Giving {id}");
							inventory.Pickup(Items.CreateAndAdd(id, Area), false);
						}
					}
					
					DailyItems = null;
				}
			}

			findASpawn = true;

			if (lastDepth == Context.Run.Depth) {
				Log.Info("Old depth is the same as the current one");
				return;
			}

			lastDepth = Context.Run.Depth;
			
			if (Context.Run.Depth > 1 && !GetComponent<StatsComponent>().TookDamageOnLevel) {
				Achievements.Unlock("bk:dodge_overlord");
			}
			
			HandleEvent(new NewLevelStartedEvent());
		}
		private bool set;
		private float t;
		private bool findASpawn;
		public bool Teleported;
		private bool FindSpawn() {
			if (/*BK.Version.Dev || */ToBoss) {
				ToBoss = false;
				
				foreach (var r in Area.Tagged[Tags.Room]) {
					var rm = (Room) r;

					if (rm.Type == RoomType.Boss) {
						Center = r.Center + new Vector2(0, 32) + Rnd.Vector(-0.5f, 0.5f);
						rm.Discover();
						Log.Debug("Teleported to boss room");
						return true;
					}
				}
			}
			
			foreach (var cc in Area.Tagged[Tags.Checkpoint]) {
				Center = cc.Center + Rnd.Vector(-0.5f, 0.5f);
				Log.Debug("Teleported to spawn point");
				return true;
			}

			foreach (var cc in Area.Tagged[Tags.Entrance]) {
				Center = cc.Center + new Vector2(0, 4) + Rnd.Vector(-0.5f, 0.5f);
				Log.Debug("Teleported to entrance");
				return true;
			}

			foreach (var r in Area.Tagged[Tags.Room]) {
				var rm = (Room) r;

				if (rm.Type == RoomType.Entrance) {
					Center = r.Center + Rnd.Vector(-0.5f, 0.5f);
					rm.Discover();
					Log.Debug("Teleported to entrance room");
					return true;
				}
			}

			foreach (var r in Area.Tagged[Tags.Room]) {
				var rm = (Room) r;

				if (rm.Type == RoomType.Exit) {
					Log.Debug("Teleported to exit room");
					Center = new Vector2(rm.CenterX, rm.Bottom - 1.4f * 16) + Rnd.Vector(-0.5f, 0.5f);
					rm.Discover();

					return true;
				}
			}
			
			
			foreach (var r in Area.Tagged[Tags.Room]) {
				var rm = (Room) r;

				Log.Debug("Teleported to random room");
				Center = new Vector2(rm.CenterX, rm.Bottom - 1.4f * 16) + Rnd.Vector(-0.5f, 0.5f);
				rm.Discover();

				return true;
			}

			Log.Error("Failed to teleport!");
			AnimationUtil.Poof(Center, 20);
			return false;
		}
	}
}
