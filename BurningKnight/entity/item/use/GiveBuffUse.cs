using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class GiveBuffUse : ItemUse {
		protected string Buff = null!;
		protected float Time;

		public override void Use(Entity entity, Item item) {
			var b = BuffRegistry.Create(Buff);

			if (b == null) {
				Log.Error($"Unknown buff {Buff}");
				return;
			}

			if (Time < 0) {
				b.Infinite = true;
			} else {
				b.TimeLeft = b.Duration = Time;
			}

			if (entity.TryGetComponent<BuffsComponent>(out var buffs)) {
				buffs.Add(b);
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			
			Time = settings["time"].Number(1);
			Buff = settings["buff"].AsString() ?? "";
		}
	}
}