using System;
using BurningKnight.assets;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.level;
using BurningKnight.level.entities;
using BurningKnight.physics;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.stand {
	// The editor half of ItemStand; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ItemStand {
		public override void RenderImDebug() {
			if (ImGui.InputText("Item", ref debugItem, 128, ImGuiInputTextFlags.EnterReturnsTrue)) {
				var item = Item;
				SetItem(Items.CreateAndAdd(debugItem, Area), null);

				if (item != null) {
					item.Done = true;
				}
			}
		}
	}
}
