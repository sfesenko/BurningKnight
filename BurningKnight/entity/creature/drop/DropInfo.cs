using System;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.creature.drop {
	public struct DropInfo {
		public string Id;
		public Type Type;
		public Action<JsonNode> Render;
	}
}