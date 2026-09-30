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
		public class CutsceneState : SmartState<BurningKnight> {
			public override void Init() {
				base.Init();

				var bkDialog = Self.GetComponent<DialogComponent>();
				var playerDialog = Self!.Target.GetComponent<DialogComponent>();
				
				Start(bkDialog, "bkw_0", Self.Target, () => {
					Start(bkDialog, "bkw_1", Self.Target, () => {
						bkDialog!.Close();
						
						Start(playerDialog, "bkw_2", Self.Target, () => {
							playerDialog!.Close();
							
							Start(bkDialog, "bkw_3", Self.Target, () => {
								Become<FollowState>();
								bkDialog.OnEnd();
								GlobalSave.Put("bk_who", true);

								Self.Target.GetComponent<HealthComponent>()!.Unhittable = false;
							});	
						});	
					});	
				});
			}
		
			private void Start(DialogComponent d, string id, Entity to, Action callback = null) {
				d.Start(id, to);

				if (callback != null) {
					d!.Dialog.ShowArrow = true;
					d.Dialog.OnEnd = () => {
						Timer.Add(callback, 0.1f);
						return true;
					};
				}
			}
		}
	}
}
