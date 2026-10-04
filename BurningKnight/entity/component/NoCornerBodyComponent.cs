using System;
using BurningKnight.physics;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.component {
	public class NoCornerBodyComponent : BodyComponent {
		public NoCornerBodyComponent(float x, float y, float w, float h, BodyType type = BodyType.Dynamic, bool sensor = false, bool center = false) {
			// Dormant today (zero call sites), but the next user gets the same
			// degenerate-quad Box2D throw as Rect did: clamp like Rect does.
			w = Math.Max(w, 0.5f);
			h = Math.Max(h, 0.5f);

			if (center) {
				x -= w / 2;
				y -= h / 2;
			}

			Body = Physics.World!.CreateBody(Vector2.Zero, 0, type);
			Body.FixedRotation = true;
			Body.UserData = this;
			Body.LinearDamping = 0;

			float mx = w / 3f;
			float my = h / 3f;

			Body.CreatePolygonFixture(new Vertices(4) {
				new Vector2(x, y + my), new Vector2(x + mx, y), 
				new Vector2(x + w - mx, y), new Vector2(x + w, y + my),
				new Vector2(x + w, y + h - my), new Vector2(x + w - mx, y + h),
				new Vector2(x + mx, y + h), new Vector2(x, y + h - my)
			}, 1f);
		}
	}
}