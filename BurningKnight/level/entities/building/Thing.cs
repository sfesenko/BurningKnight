using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.physics;
using BurningKnight.state;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component.graphics;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.physics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.level.entities.building {
	public partial class Thing : Prop, CollisionFilterEntity {
		private TextureRegion shadow = null!;
		// A shadow-rendering mode nothing sets; the branches below are kept for it.
#pragma warning disable CS0649
		private bool separateShadow;
#pragma warning restore CS0649
		private bool hasShadow = true;
		private string file = "";
		private string sprite = "";
		private bool hasBody = true;
		private Rectangle collider;

		private bool displayCollider = true;
		
		public override void AddComponents() {
			base.AddComponents();
			AddComponent(new ShadowComponent(RenderShadow));
		}

		public override void PostInit() {
			base.PostInit();

			if (file != null && sprite != null) {
				AddComponent(new SliceComponent(file, sprite));
			}

			if (!Engine.EditingLevel && hasBody) {
				AddComponent(new RectBodyComponent(collider.X, collider.Y, collider.Width, collider.Height, BodyType.Static));
			}
		}

		private void UpdateSprite() {
			var f = Animations.Get(file);

			if (f == null) {
				return;
			}
			
			var sp = f.GetSlice(sprite);

			if (!HasComponent<SliceComponent>()) {
				AddComponent(new SliceComponent(file, sprite));
			} else {
				GetComponent<SliceComponent>()!.Sprite = sp;
			}

			if (sp != null) {
				Width = sp.Width;
				Height = sp.Height;
			}
			
			if (separateShadow) {
				shadow = f.GetSlice($"{sprite}_shadow");
			}
		}

		private void RenderShadow() {
			if (!hasShadow) {
				return;
			}

			if (separateShadow) {
				Graphics.Render(shadow, Position + new Vector2(0,  Height + shadow.Height), 0, Vector2.Zero, MathUtils.InvertY);
			} else {
				GraphicsComponent?.Render(true);
			}
		}

		public virtual bool ShouldCollide(Entity entity) {
			return !(entity is Creature c && c.InAir());
		}

		public override void Render() {
			base.Render();

			if (Engine.EditingLevel && displayCollider && hasBody) {
				Graphics.Batch.DrawRectangle(new RectangleF(X + collider.X, Y + collider.Y, collider.Width, collider.Height), Color.Red);
			}
		}


		public override void Load(FileReader stream) {
			base.Load(stream);

			hasShadow = stream.ReadBoolean();
			hasBody = stream.ReadBoolean();

			if (hasBody) {
				collider.X = stream.ReadInt16();
				collider.Y = stream.ReadInt16();
				collider.Width = stream.ReadInt16();
				collider.Height = stream.ReadInt16();
			}

			file = stream.ReadString();
			sprite = stream.ReadString();
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			stream.WriteBoolean(hasShadow);
			stream.WriteBoolean(hasBody);

			if (hasBody) {
				stream.WriteInt16((short) collider.X);
				stream.WriteInt16((short) collider.Y);
				stream.WriteInt16((short) collider.Width);
				stream.WriteInt16((short) collider.Height);
			}
			
			stream.WriteString(file);
			stream.WriteString(sprite);
		}
	}
}