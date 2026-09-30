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
		public class SwordAttackState : SmartState<BurningKnight> {
			public override void Init() {
				base.Init();

				Timer.Add(() => {
					Self.GetComponent<AudioEmitterComponent>()!.EmitRandomized("mob_fire_static");

					var a = Self.AngleTo(Self.Target);
					
					var p = new ProjectilePattern(KeepShapePattern.Make(0)) {
						Position = Self.Center
					};

					Self.Area.Add(p);
					
					ProjectileTemplate.MakeFast(Self, "small", Self.Center, a, (pr) => {
						pr.RemoveFlags(ProjectileFlags.Reflectable, ProjectileFlags.BreakableByMelee);

						p.Add(pr);
						pr.Color = ProjectileColor.Red;
					}, swordData, () => {
						Timer.Add(() => {
							p.Launch(a, 30);
							Self.GetComponent<AudioEmitterComponent>()!.EmitRandomized("mob_fire_static");

							Become<FightState>();
						}, 0.2f);
					});
				}, 1f);
			}
		}
	}
}
