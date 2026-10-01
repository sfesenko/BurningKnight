using BurningKnight.entity.item;
using BurningKnight.entity.item.stand;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class ItemPriceCalculationEvent : Event {
		public Item Item = null!;
		public ShopStand Stand = null!;
		public float Percent;
	}
}