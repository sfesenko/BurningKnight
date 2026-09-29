using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.custom;
using BurningKnight.debug;
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
using ImGuiNET;
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
	// The editor half of Level; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Level {
		public override void RenderImDebug() {
			base.RenderImDebug();
			ImGui.Text($"Size: {width}x{height} = {Size} (real {width * height})");
			ImGui.Text($"Variant: {Variant.GetType().Name}");
			ImGui.Text($"Tiles: {Tiles.Length}");
			ImGui.Text($"Liquid: {Liquid.Length}");
			ImGui.Text($"Variants: {Variants.Length}");
			ImGui.Text($"LiquidVariants: {LiquidVariants.Length}");
			ImGui.Text($"WallDecor: {WallDecor.Length}");
		}
	}
}
