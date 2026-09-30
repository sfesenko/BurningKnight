using BurningKnight.entity.item;
using BurningKnight.entity.item.stand;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class ItemBoughtEvent : Event {
		public Item Item = null!;
		public Entity Who = null!;
		public ItemStand Stand = null!;
	}
}