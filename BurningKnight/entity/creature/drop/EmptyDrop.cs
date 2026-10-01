using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.creature.drop {
	public class EmptyDrop : Drop {
		public EmptyDrop(float chance = 1) {
			Chance = chance;
		}
		
		public override List<string> GetItems() {
			return [];
		}

		public override string GetId() {
			return "empty";
		}

		public override void Load(JsonNode root) {
			
		}

		public override void Save(JsonNode root) {
			
		}

		public static void RenderDebug(JsonNode root) {
			
		}
	}
}