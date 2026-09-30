using BurningKnight.debug;
using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.fx;
using BurningKnight.entity.room;
using BurningKnight.level.biome;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.level.variant;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace BurningKnight.level {
	public partial class Level {
		public void ReCreateBodyChunk(int x, int y) {
			if (Components == null) {
				return;
			}

			MarkForBodyUpdate(x, y);
			MarkForBodyUpdate(x - 1, y);
			MarkForBodyUpdate(x + 1, y);
			MarkForBodyUpdate(x, y - 1);
			MarkForBodyUpdate(x, y + 1);
		}
		private void MarkForBodyUpdate(int x, int y) {
			GetComponent<LevelBodyComponent>()!.ReCreateBodyChunk(x, y);
			Chasm.GetComponent<ChasmBodyComponent>()!.ReCreateBodyChunk(x, y);
			HalfWall.GetComponent<HalfWallBodyComponent>()!.ReCreateBodyChunk(x, y);
			HalfProjectile.GetComponent<HalfProjectileBodyComponent>()!.ReCreateBodyChunk(x, y);
			ProjectileLevelBody.GetComponent<ProjectileBodyComponent>()!.ReCreateBodyChunk(x, y);		
		}
		public void RecreateBody() {
			GetComponent<LevelBodyComponent>()!.CreateBody();
			Chasm.GetComponent<ChasmBodyComponent>()!.CreateBody();
			HalfWall.GetComponent<HalfWallBodyComponent>()!.CreateBody();
			HalfProjectile.GetComponent<HalfProjectileBodyComponent>()!.CreateBody();
			ProjectileLevelBody.GetComponent<ProjectileBodyComponent>()!.CreateBody();		
		}
		public void CreateBody() {
			if (Components == null) {
				return;
			}
			
			if (Chasm == null) {
				Area.Add(HalfWall = new HalfWall {
					Level = this
				});
				
				Area.Add(HalfProjectile = new HalfProjectileLevel {
					Level = this
				});
				
				Area.Add(Chasm = new Chasm {
					Level = this
				});

				Area.Add(ProjectileLevelBody = new ProjectileLevelBody {
					Level = this
				});
			}
			
			Chasm.GetComponent<ChasmBodyComponent>()!.CreateBody();			
			HalfWall.GetComponent<HalfWallBodyComponent>()!.CreateBody();
			HalfProjectile.GetComponent<HalfProjectileBodyComponent>()!.CreateBody();
			ProjectileLevelBody.GetComponent<ProjectileBodyComponent>()!.CreateBody();
			GetComponent<LevelBodyComponent>()!.CreateBody();
		}
		public void CreateDestroyableBody() {
			if (Components == null) {
				return;
			}
		
			// Destroyable.GetComponent<DestroyableBodyComponent>().CreateBody();
		}
	}
}
