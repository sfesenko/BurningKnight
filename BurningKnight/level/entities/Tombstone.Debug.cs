using System;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.fx;
using BurningKnight.entity.item;
using BurningKnight.state;
using BurningKnight.ui.dialog;
using BurningKnight.ui.inventory;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.input;
using Lens.util.camera;
using Lens.util.file;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.entities {
	// The editor half of Tombstone; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Tombstone {
		public override void RenderImDebug() {
			base.RenderImDebug();

			var has = Item != null;

			if (ImGui.Checkbox("Has item", ref has)) {
				Item = has ? "" : null;
				UpdateSprite();
			}

			if (has) {
				ImGui.InputText("Item##itm", ref Item, 128);
			}
		}
	}
}
