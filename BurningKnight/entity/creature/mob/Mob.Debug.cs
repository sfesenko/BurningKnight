using System;
using System.Collections.Generic;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.assets.particle;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.drop;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.creature.mob.prefix;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.entities;
using BurningKnight.level.paintings;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.level.variant;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.imgui;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.graphics;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.entity.creature.mob {
	// The editor half of Mob; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Mob {
		public override void RenderImDebug() {
			base.RenderImDebug();
			
			ImGui.Text($"Target: {(Target == null ? "null" : Target.GetType().Name)}");

			if (Target != null) {
				if (ImGui.Button("Jump")) {
					WindowManager.Entities = true;
					AreaDebug.ToFocus = Target;
				}
			}
			
			ImGui.Text($"Prefix: {(Prefix == null ? "null" : Prefix.Id)}");
		}
	}
}
