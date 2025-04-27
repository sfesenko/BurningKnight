using System;
using Lens.entity;
using Lens.entity.component;

namespace BurningKnight.entity.component {
	public class InteractableComponent(Func<Entity, bool> interact) : Component
	{
		public readonly Func<Entity, bool> Interact = interact;
		public Func<Entity, bool> CanInteract;
		public Action<Entity> OnStart;
		public Action<Entity> OnEnd;
		public Func<Entity> AlterInteraction;
		public Entity CurrentlyInteracting;
		public float OutlineAlpha;

		public override void Update(float dt) {
			base.Update(dt);

			OutlineAlpha += ((CurrentlyInteracting == null ? 0 : 1) - OutlineAlpha) * dt * 8;
		}
	}
}