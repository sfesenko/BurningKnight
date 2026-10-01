using BurningKnight.entity.creature.player;
using BurningKnight.entity.item;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class WeaponSwappedEvent : Event {
		public Player Who = null!;
		public Item Current = null!;
		public Item Old = null!;
	}
}