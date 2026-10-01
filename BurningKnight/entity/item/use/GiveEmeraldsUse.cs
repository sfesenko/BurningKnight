using BurningKnight.save;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class GiveEmeraldsUse : ItemUse {
		public int Amount;

		public override void Use(Entity entity, Item item) {
			GlobalSave.Emeralds += Amount;

			entity.Area!.EventListener.Handle(new GaveEvent {
				Amount = Amount
			});
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			Amount = settings["amount"].Int(1);
		}
		
		public class GaveEvent : Event {
			public int Amount;
		}
	}
}