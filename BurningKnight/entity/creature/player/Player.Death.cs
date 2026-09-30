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
		public void RenderOutline() {
			var component = GetComponent<PlayerGraphicsComponent>();
			var color = component.Tint;
			
			component.Tint = new Color(0f, 0f, 0f, 0.65f);
			component.SimpleRender(false);
			component.Tint = color;
		}
		protected override bool HandleDeath(DiedEvent d) {
			Done = false;
			died = true;
			var ing = (InGameState) Engine.Instance.State;

			ing.Killer.Animation = null;
			ing.Killer.Slice = null;
			ing.Killer.UseSlice = true;

			var b = GetComponent<RectBodyComponent>();
			
			b.Knockback = Vector2.Zero;
			b.Velocity = Vector2.Zero;
			
			if (d.From != null && d.From != this) {
				var anim = d.From.GetAnyComponent<AnimationComponent>();

				if (anim != null) {
					ing.Killer.Animation = Animations.Get(anim.Id)?.CreateAnimation();
					
					if (ing.Killer.Animation != null) {
						ing.Killer.Animation.Tag = "idle";
						ing.Killer.UseSlice = false;

						var c = ing.Killer.Animation.GetCurrentTexture();
						
						ing.Killer.Width = c.Width;
						ing.Killer.Height = c.Height;
					}
				} else {
					var slice = d.From.GetAnyComponent<SliceComponent>();

					if (slice != null) {
						ing.Killer.Slice = slice.Sprite;
						ing.Killer.Width = ing.Killer.Slice.Width;
						ing.Killer.Height = ing.Killer.Slice.Height;
					}
				}
			}

			if (d.From == this) {
				ing.Killer.Slice = CommonAse.Ui.GetSlice("self");
			}

			if (ing.Killer.Slice == null && ing.Killer.Animation == null) {
				ing.Killer.Slice = CommonAse.Items.GetSlice("unknown");
				ing.Killer.Width = ing.Killer.Slice.Width;
				ing.Killer.Height = ing.Killer.Slice.Height;
			}

			ing.Killer.Width *= 2;
			ing.Killer.Height *= 2;
			ing.Killer.RelativeCenterX = Display.UiWidth * 0.75f;

			Log.Info($"Killed by: {(d.From?.GetType().Name ?? "null")}");
			return true;
		}
		private bool died;
		public override void AnimateDeath(DiedEvent d) {
			Dead = true;
			
			base.AnimateDeath(d);
			
			for (var i = 0; i < 6; i++) {
				Area.Add(new ParticleEntity(Particles.Dust()) {
					Position = Center + new Vector2(Rnd.Int(-4, 4), Rnd.Int(-4, 4)), 
					Depth = 30
				});
			}

			GetComponent<OrbitGiverComponent>()!.DestroyAll();
			GetComponent<FollowerComponent>()!.DestroyAll();
			
			var stone = new Tombstone();
			stone.DisableDialog = true;

			if (InGameState.Multiplayer) {
				stone.HasPlayer = true;
				stone.Index = GetComponent<InputComponent>()!.Index;
				stone.WasGamepad = GetComponent<InputComponent>()!.GamepadEnabled;

				if (GetComponent<InputComponent>()!.Index == 0) {
					var minIndex = 1024;
					Player pl = null;

					foreach (var p in Area.Tagged[Tags.Player]) {
						var i = p.GetComponent<InputComponent>()!.Index;

						if (p != this && i < minIndex) {
							minIndex = i;
							pl = (Player) p;
						}
					}

					if (pl != null) {
						var c = ForceGetComponent<ConsumablesComponent>();
						c.Entity = pl;
						Components.Remove(typeof(ConsumablesComponent));
						pl.Components[typeof(ConsumablesComponent)] = c;
						AddComponent(new ConsumablesComponent());
					}
				}
			}

			var pool = new List<string>();

			foreach (var i in GetComponent<InventoryComponent>()!.Items) {
				if (i.Type != ItemType.Hat && i.Id != "bk:no_lamp") {
					pool.Add(i.Id);
				}
			}

			var w = GetComponent<ActiveItemComponent>()!.Item;

			if (w != null) {
				pool.Add(w.Id);
			}

			w = GetComponent<WeaponComponent>()!.Item;

			if (w != null) {
				pool.Add(w.Id);
			}
			
			w = GetComponent<ActiveWeaponComponent>()!.Item;

			if (w != null) {
				pool.Add(w.Id);
			}

			if (pool.Count == 0) {
				pool.Add("bk:coin");
			}

			GlobalSave.Put("next_tomb", pool[Rnd.Int(pool.Count)]);
			GlobalSave.Put("tomb_depth", Context.Run.Depth);
			
			Area.Add(stone);
				
			stone.CenterX = CenterX;
			stone.Bottom = Bottom;

			if (InGameState.EveryoneDied(this)) {
				Context.Camera!.Targets.Clear();
				Context.Camera!.Follow(stone, 0.5f);
			} else {
				((InGameState) Engine.Instance.State).ResetFollowing();
			}
		}
	}
}
