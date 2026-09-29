using System;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.state;
using BurningKnight.ui.dialog;
using BurningKnight.util;
using ImGuiNET;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.util.file;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.stand {
	// The editor half of ShopStand; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ShopStand {
		public override void RenderImDebug() {
			base.RenderImDebug();

			if (ImGui.InputInt("Price", ref Price)) {
				CalculatePriceSize();
			}
		}
	}
}
