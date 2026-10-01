using System;
using BurningKnight.assets;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.state;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component;
using Lens.entity.component.logic;
using Lens.graphics;
using Lens.util.camera;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BurningKnight.ui.dialog {
	// The editor half of DialogComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class DialogComponent {
		public override void RenderDebug() {
			base.RenderDebug();

			if (ImGui.InputText("Say", ref toSay, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
				Start(toSay);
				toSay = "";
			}

			ImGui.InputInt("Voice", ref Dialog!.Voice);

			if (ImGui.Button("Test")) {
				Start("Quick brown fox jumped over lazy dog");
			}

			ImGui.SameLine();

			if (ImGui.Button("Close")) {
				Close();
			}
		}
	}
}
