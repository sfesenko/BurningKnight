using BurningKnight.entity.component;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.physics {
	public class Physics {
		public static IPhysicsWorld World;
		public static bool RenderDebug = false;

		public static void Init() {
			World = PhysicsFactory.CreateWorld(Vector2.Zero);

			World.PreSolve += PreSolve;
			World.BeginContact += BeginContact;
			World.EndContact += EndContact;
		}

		public static void RemoveBody(IPhysicsBody body) {
			World?.RemoveBody(body);
		}

		public static void PreSolve(IContact contact) {
			var a = contact.FixtureA.Body.UserData;
			var b = contact.FixtureB.Body.UserData;

			if (a is BodyComponent ac && b is BodyComponent bc) {
				if (ac.Entity.TryGetComponent<CollisionFilterComponent>(out var af)) {
					var v = af.Invoke(bc.Entity);

					if (v == CollisionResult.Disable) {
						contact.Enabled = false;
					} else if (v == CollisionResult.Enable) {
						return;
					}
				}
				
				if (bc.Entity.TryGetComponent<CollisionFilterComponent>(out var bf)) {
					var v = bf.Invoke(ac.Entity);

					if (v == CollisionResult.Disable) {
						contact.Enabled = false;
					} else if (v == CollisionResult.Enable) {
						return;
					}
				}
				
				if (!ac.ShouldCollide(bc.Entity) || !bc.ShouldCollide(ac.Entity)) {
					contact.Enabled = false;
				}
			}
		}
		
		public static void BeginContact(IContact contact) {
			var a = contact.FixtureA.Body.UserData;
			var b = contact.FixtureB.Body.UserData;
			
			if (a is BodyComponent ac && b is BodyComponent bc) {
				ac.OnCollision(bc.Entity, contact.FixtureB);
				bc.OnCollision(ac.Entity, contact.FixtureA);
			}
		}

		public static void EndContact(IContact contact) {
			var a = contact.FixtureA.Body.UserData;
			var b = contact.FixtureB.Body.UserData;

			if (a is BodyComponent ac && b is BodyComponent bc) {
				ac.OnCollisionEnd(bc.Entity, contact.FixtureB);
				bc.OnCollisionEnd(ac.Entity, contact.FixtureA);
			}
		}

		public static void Update(float dt) {
			World?.Step(dt);
		}

		public static void Render() {
			if (RenderDebug) {
				World?.RenderDebug();
			}
		}

		public static void Destroy() {
			World?.Clear();
			World = null;
		}
	}
}
