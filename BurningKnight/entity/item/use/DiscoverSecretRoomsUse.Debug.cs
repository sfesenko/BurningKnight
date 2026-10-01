using System;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.room;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.state;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of DiscoverSecretRoomsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class DiscoverSecretRoomsUse {
		public static void RenderDebug(JsonNode root) {
			var chance = root["chance"].Number(100);

			if (ImGui.InputFloat("Chance", ref chance)) {
				root["chance"] = chance;
			}
		}
	}
}
