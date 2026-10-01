using BurningKnight.assets.items;
using BurningKnight.state;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use.parent {
	// The editor half of DoUsesUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class DoUsesUse {
		public static void RenderDebug(JsonNode root) {
			if (!root["uses"].IsJsonArray()) {
				root["uses"] = new JsonArray();
			}
			
			ItemEditor.DisplayUse(root, root["uses"]);
		}
	}
}
