using BurningKnight.state;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of EnableScourgeUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class EnableScourgeUse {
		public static void RenderDebug(JsonNode root) {
			root.InputText("Scourge", "scourge");
		}
	}
}
