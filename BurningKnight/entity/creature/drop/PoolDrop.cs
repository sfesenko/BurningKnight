using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.item;
using BurningKnight.util;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.creature.drop {
	public partial class PoolDrop : Drop {
		public ItemPool Pool = null!;
		public int Min;
		public int Max;

		public PoolDrop() {
			
		}
		
		public PoolDrop(ItemPool pool, float chance = 1f, int min = 1, int max = 1) {
			Pool = pool;
			Chance = chance;
			Min = min;
			Max = max;
		}

		public override List<string> GetItems() {
			var list = new List<string>();

			for (var i = 0; i < Rnd.Int(Min, Max + 1); i++) {
				list.Add(Items.Generate(Pool)!);
			}

			return list;
		}

		public override string GetId() {
			return "pool";
		}

		public override void Load(JsonNode root) {
			base.Load(root);

			Min = root["min"].Int(1);
			Max = root["max"].Int(1);
			Pool = ItemPool.ById[root["pool"].Int(0)];
		}

		public override void Save(JsonNode root) {
			base.Save(root);

			root["min"] = Min;
			root["max"] = Max;
			root["pool"] = Pool.Id;
		}

	}
}