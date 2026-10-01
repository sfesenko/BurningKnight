using BurningKnight.physics;
using Lens.entity;
using Lens.physics;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.use {
	public class AddHitboxUse : ItemUse {
		private IPhysicsBody? body;

		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);
			
			body = Physics.World!.CreateBody(Vector2.Zero, 0, BodyType.Dynamic);

			body.FixedRotation = true;
			body.UserData = this;
			body.LinearDamping = 0;

			var r = Item.Region;
			
			var w = r.Width;
			var h = r.Height;
			var x = -r.Width / 2;
			var y = 0;
			
			body.CreatePolygonFixture(new Vertices(4) {
				new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)
			}, 1f).IsSensor = true;

			body.Position = entity.Center;
		}

		public override void Destroy() {
			base.Destroy();

			if (body != null) {
				Physics.RemoveBody(body);
				body = null;
			}
		}
	}
}