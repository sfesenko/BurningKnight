using BurningKnight.entity.bomb.controller;
using BurningKnight.entity.events;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeBombsHomeUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeBombsHomeUse {
		public static void RenderDebug(JsonNode root) {
			var speed = root["speed"].Number(1);

			if (ImGui.InputFloat("Speed", ref speed)) {
				root["speed"] = speed;
			}
		}
	}
}
