using System;
using BurningKnight.assets.achievements;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob.jungle;
using BurningKnight.entity.projectile;
using BurningKnight.entity.projectile.controller;
using BurningKnight.entity.projectile.pattern;
using BurningKnight.level;
using BurningKnight.level.entities;
using Lens.entity;
using Lens.util;
using Lens.util.camera;
using Lens.util.math;
using Lens.util.timer;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.creature.mob.boss {
	public partial class QueenBee {
		public class IdleState : SmartState<QueenBee> {
			public override void Update(float dt) {
				if (Self.Target == null) {
					return;
				}

				base.Update(dt);

				if (T >= 0.2f) {
					if (Self.penetrateCount != 0) {
						Become<PrepareToPenetrateState>();
					} else {
						var a = Self.attack = (Self.attack + 1) % (Self.InFirstPhase ? 3 : 4);
						
						if (a == 1) {
							Self.penetrateCount++;
						} else if (a == 2) {
							Become<ToCenterState>();
						} else if (a == 3) {
							Become<MachineGunState>();
						} else if (a == 0) {
							if (Self.InSecondPhase || (Self.InThirdPhase && Rnd.Chance())) {
								Become<CircleState>();
							} else {
								Become<SpamBeesState>();
							}
						}
					}
				}
			}
		}
	}
}
