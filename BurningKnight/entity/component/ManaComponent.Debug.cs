using BurningKnight.assets;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.controller;
using BurningKnight.assets.particle.custom;
using BurningKnight.assets.particle.renderer;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	// The editor half of ManaComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ManaComponent {
		public override void RenderDebug() {
			var v = (int) mana;

			if (ImGui.InputInt("Mana", ref v)) {
				mana = (byte) v;
			}

			v = manaMax;

			if (ImGui.InputInt("Max Mana", ref v)) {
				manaMax = (byte) v;
			}

			if (ImGui.Button("Reset")) {
				ModifyMana(manaMax - mana);
			}
		}
	}
}
