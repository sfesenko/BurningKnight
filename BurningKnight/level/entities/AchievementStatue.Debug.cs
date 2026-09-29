using System;
using BurningKnight.assets.achievements;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.npc;
using BurningKnight.save;
using BurningKnight.ui.dialog;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.entities {
	// The editor half of AchievementStatue; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class AchievementStatue {
		public override void RenderImDebug() {
			base.RenderImDebug();

			if (ImGui.InputText("Id", ref id, 128)) {
				SetupSprite();
				UpdateState();
			}
		}
	}
}
