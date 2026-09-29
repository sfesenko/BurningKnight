using System.Collections.Generic;
using BurningKnight.assets.achievements;
using BurningKnight.assets.particle;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.level;
using BurningKnight.state;
using ImGuiNET;
using Lens.entity;
using Lens.entity.component;
using Lens.util.file;
using Lens.util.math;

namespace BurningKnight.entity.component {
	// The editor half of InventoryComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class InventoryComponent {
		public override void RenderDebug() {
			ImGui.Text($"Total {Items.Count} items");
			
			foreach (var item in Items) {
				ImGui.BulletText(item.Id);
			}
		}
	}
}
