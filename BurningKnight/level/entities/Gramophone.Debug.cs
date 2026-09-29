using System;
using BurningKnight.assets;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using Lens.entity;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.controller;
using BurningKnight.assets.particle.renderer;
using BurningKnight.entity;
using BurningKnight.entity.creature.player;
using BurningKnight.save;
using BurningKnight.state;
using ImGuiNET;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.entities {
	// The editor half of Gramophone; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Gramophone {
		public override void RenderImDebug() {
			base.RenderImDebug();
			ImGui.InputInt("Disk", ref disk);
		}
	}
}
