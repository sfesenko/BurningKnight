using BurningKnight.entity.component;
using BurningKnight.entity.item;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class ItemAddedEvent : Event {
		public Item Item = null!;
		public Item Old = null!;
		public ItemComponent Component = null!;
		public Entity Who = null!;
	}
}