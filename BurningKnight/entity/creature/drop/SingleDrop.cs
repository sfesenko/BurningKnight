using System.Collections.Generic;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.creature.drop {
	public class SingleDrop : Drop {
		public string Item = null!;

		public SingleDrop() {
			
		}

		public SingleDrop(string id, float chance = 1f) {
			Item = id;
			Chance = chance;
		}

		public override List<string> GetItems() {
			var items = new List<string>();
			
			if (assets.items.Items.ShouldAppear(Item)) {
				items.Add(Item);
			} else {
				Log.Error($"Blocked {Item}");
			}

			return items;
		}

		public override string GetId() {
			return "single";
		}

		public override void Load(JsonNode root) {
			base.Load(root);
			Item = root["item"].String("");
		}

		public override void Save(JsonNode root) {
			base.Save(root);
			root["item"] = Item;
		}

		public static void RenderDebug(JsonNode root) {
			
		}
	}
}