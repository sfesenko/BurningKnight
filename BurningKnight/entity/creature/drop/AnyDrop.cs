using System.Collections.Generic;

namespace BurningKnight.entity.creature.drop {
	public class AnyDrop : OneOfDrop {
		public AnyDrop() {
			
		}
		
		public AnyDrop(params Drop[] drops) : base(drops) {
			
		}

		public override List<string> GetItems() {
			var items = new List<string>();

			foreach (var d in Drops) {
				items.AddRange(d.GetItems());
			}
			
			return items;
		}

		public override string GetId() {
			return "any";
		}
	}
}