using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using Lens.entity;

namespace BurningKnight.entity.events {
	public class ProjectileCreatedEvent : Event {
		public Projectile Projectile = null!;
		public Entity Owner = null!;
		public Item Item = null!;
	}
}