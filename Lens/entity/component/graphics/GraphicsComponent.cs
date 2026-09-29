using Microsoft.Xna.Framework;

namespace Lens.entity.component.graphics {
	public partial class GraphicsComponent : Component {
		public bool Flipped;
		public bool FlippedVerticaly;
		public bool CustomFlip;
		public bool Enabled = true;
		public Vector2 Offset;
		
		public virtual void Render(bool shadow) {
			
		}

	}
}