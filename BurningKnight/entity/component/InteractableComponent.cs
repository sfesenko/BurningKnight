using System;
using Lens.entity;
using Lens.entity.component;

namespace BurningKnight.entity.component {
	public class InteractableComponent(Func<Entity, bool> interact) : Component
	{
		public readonly Func<Entity, bool> Interact = interact;
		public Func<Entity, bool> CanInteract = null!;
		public Action<Entity> OnStart = null!;
		public Action<Entity> OnEnd = null!;
		public Func<Entity> AlterInteraction = null!;
		public Entity? CurrentlyInteracting;
		public float OutlineAlpha;

		public override void Update(float dt) {
			base.Update(dt);

			OutlineAlpha += ((CurrentlyInteracting == null ? 0 : 1) - OutlineAlpha) * dt * 8;
		}
	}
}