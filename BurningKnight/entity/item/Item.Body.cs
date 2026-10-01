using System;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.item.use;
using BurningKnight.entity.item.useCheck;
using BurningKnight.level;
using BurningKnight.level.rooms;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item {
	public partial class Item {
		protected virtual BodyComponent CreateBody() {
			var slice = Region;
			return new RectBodyComponent(0, 0, slice.Source.Width, slice.Source.Height);
		}
		public virtual void AddDroppedComponents() {
			var body = CreateBody();

			t = 0;
			
			AddComponent(body);

			body!.Body!.LinearDamping = Type == ItemType.Mana ? 1 : 4;
			body.Body.Friction = 0;
			body.Body.Mass = 0.1f;
			
			AddComponent(new InteractableComponent(Interact) {
				OnStart = OnInteractionStart,
				CanInteract = ShouldInteract
			});
			
			AddComponent(new ShadowComponent(RenderShadow));
			
			AddTag(Tags.LevelSave);
			AddTag(Tags.Item);
			
			AddComponent(new RoomComponent());
			AddComponent(new ExplodableComponent());
			AddComponent(new SupportableComponent());

			CheckMasked();
		}
		public void RandomizeVelocity(float force) {
			var component = GetBody();
			
			if (component == null) {
				return;
			}

			force *= 60f;
			var angle = Rnd.AnglePI();
			
			component.Velocity += new Vector2((float) Math.Cos(angle) * force, (float) Math.Sin(angle) * force);
		}
		protected virtual void RemoveBody() {
			RemoveComponent<RectBodyComponent>();
		}
		public virtual void RemoveDroppedComponents() {
			RemoveComponent<InteractableComponent>();
			RemoveComponent<ShadowComponent>();
			RemoveComponent<LightComponent>();
			RemoveBody();
			
			RemoveTag(Tags.LevelSave);
			RemoveTag(Tags.Item);

			RemoveComponent<RoomComponent>();
			RemoveComponent<ExplodableComponent>();
			RemoveComponent<SupportableComponent>();

			CheckMasked();
		}
		private void RenderShadow() {
			GraphicsComponent!.Render(true);
		}
		protected virtual bool HasBody() {
			return HasComponent<RectBodyComponent>();
		}
		protected virtual BodyComponent? GetBody() {
			return TryGetComponent<RectBodyComponent>(out var b) ? b : null;
		}
	}
}
