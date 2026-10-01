using System;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of MakeProjectilesKillWithBuffUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class MakeProjectilesKillWithBuffUse {
		public static void RenderDebug(JsonNode root) {
			root.InputText("Buff", "buff", "bk:frozen");
		}
	}
}
