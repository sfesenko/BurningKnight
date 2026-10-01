using BurningKnight.entity.item.use.parent;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.timer;

namespace BurningKnight.entity.item.use {
	public partial class DoOnTimerUse : DoUsesUse {
		private float time;
		
		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);

			Timer.Add(() => {
				foreach (var u in Uses) {
					u.Item = item;
					u.Use(entity, item);
				}
			}, time);
		}

		protected override void DoAction(Entity entity, Item item, ItemUse use) {
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			time = settings["time"].Number(1f);
		}
	}
}