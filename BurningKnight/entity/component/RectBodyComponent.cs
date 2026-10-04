using System;
using BurningKnight.physics;
using Lens.physics;
using Lens.util;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	public class RectBodyComponent : BodyComponent {
		public RectBodyComponent(float x, float y, float w, float h, BodyType type = BodyType.Dynamic, bool sensor = false, bool center = false) {
			// Box2D rejects degenerate quads: clamp to a sliver rather than hand
			// native code a zero-area polygon (same reason Resize clamps below).
			w = Math.Max(w, 0.5f);
			h = Math.Max(h, 0.5f);

			if (center) {
				x -= w / 2;
				y -= h / 2;
				Offset = new Vector2(w / 2, h / 2);
			}

			Body = Physics.World!.CreateBody(Vector2.Zero, 0, type);

			Body.FixedRotation = true;
			Body.UserData = this;
			Body.LinearDamping = 0;

			if (type == BodyType.Static) {
				KnockbackModifier = 0;
			}
			
			Body.CreatePolygonFixture(new Vertices(4) {
				new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)
			}, 1f).IsSensor = sensor;
		}

		public override void Resize(float x, float y, float w, float h, bool center = false) {
			// Clamp, don't skip: a silent return would leave Width/Height already
			// shrunk while the fixture keeps its old size (visual/physics mismatch).
			// A degenerate quad fails Box2D validation and would leave the body
			// fixture-less even if the caller catches it.
			w = Math.Max(w, 0.5f);
			h = Math.Max(h, 0.5f);

			var fixture = Body!.FixtureList[0];
			var sensor = fixture.IsSensor;
			
			Body.DestroyFixture(fixture);

			if (center) {
				x -= w / 2;
				y -= h / 2;
				Offset = new Vector2(w / 2, h / 2);
			}
			
			Body.CreatePolygonFixture(new Vertices(4) {
				new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)
			}, 1f).IsSensor = sensor;
		}
	}
}