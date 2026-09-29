using System;
using System.Collections.Generic;
using System.Linq;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.buff;
using BurningKnight.entity.events;
using BurningKnight.level;
using BurningKnight.level.tile;
using BurningKnight.state;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.entity.component;
using Lens.util.file;

namespace BurningKnight.entity.component {
	// The editor half of BuffsComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class BuffsComponent {
		public override void RenderDebug() {
			if (ImGui.InputText("Buff", ref toAdd, 128)) {
				Add(toAdd);
				toAdd = "";
			}
			
			ImGui.SameLine();
				
			if (ImGui.Button("Add")) {
				Add(toAdd);
				toAdd = "";
			}

			if (Buffs.Count == 0) {
				ImGui.BulletText("No buffs");
				return;
			}

			if (ImGui.Button("Remove all")) {
				foreach (var b in Buffs.Values) {
					b.TimeLeft = 0;
				}
			}
			
			foreach (var b in Buffs.Values) {
				if (toAdd.Length > 0 && !BuffRegistry.All.ContainsKey(toAdd)) {
					ImGui.BulletText("Unknown buff");
				}
				
				if (ImGui.TreeNode($"{b.Type}")) {
					ImGui.Text($"{b.TimeLeft} seconds left");
					
					if (ImGui.Button($"Remove##{b.Type}")) {
						b.TimeLeft = 0;
					}
					
					ImGui.SameLine();
					
					if (ImGui.Button($"Renew##{b.Type}")) {
						b.TimeLeft = b.Duration;
					}

					ImGui.Checkbox($"Infinite##{b.Type}", ref b.Infinite);
					ImGui.TreePop();
				}
			}
		}
	}
}
