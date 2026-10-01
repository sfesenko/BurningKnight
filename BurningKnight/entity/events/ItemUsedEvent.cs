using BurningKnight.entity.item;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class ItemUsedEvent : Event {
		public Item Item = null!;
		public Entity Who = null!;
		public bool Fake;
	}
}