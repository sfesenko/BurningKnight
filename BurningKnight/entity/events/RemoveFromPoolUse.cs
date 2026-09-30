using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.item;
using BurningKnight.entity.item.use;
using BurningKnight.save;
using BurningKnight.state;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.events {
	public partial class RemoveFromPoolUse : ItemUse {
		private List<string> items = new List<string>();

		public override void Use(Entity entity, Item item) {
			if (item.Used) {
				return;
			}

			foreach (var i in items) {
				Context.Run.Statistics.Banned.Add(i);
			}
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			items.Clear();

			foreach (var i in settings["items"].AsJsonArray) {
				items.Add(i.String(""));
			}
		}

	}
}