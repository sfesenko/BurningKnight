using Lens.entity;
using Lens.entity.component;
using Microsoft.Xna.Framework;

namespace BurningKnight.assets.lighting {
	public partial class LightComponent : Component {
		public Light Light;

		public LightComponent(Entity entity, float radius, Color color) {
			Light = Lights.New(entity, radius, color);
			Light.UpdateCache();
		}

		public LightComponent(Light light) {
			Light = light;
			Lights.Add(light);
			Light.UpdateCache();
		}

		public override void Update(float dt) {
			Light.Update(dt);
		}

		public override void Destroy() {
			base.Destroy();
			Lights.Remove(Light);
		}
		
	}
}