using System;
using BurningKnight.assets;
using BurningKnight.state;
using Lens.graphics;
using Lens.lightJson;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.renderer {
	public partial class ItemRenderer {
		public Item Item;
		public Vector2 Origin;
		public Vector2 Nozzle;
		public bool Hidden;

		public virtual void Render(bool atBack, bool paused, float dt, bool shadow, int offset) {
			
		}

		public virtual void Setup(JsonValue settings) {
			Origin.X = settings["ox"].Number(0);
			Origin.Y = settings["oy"].Number(0);
			Nozzle.X = settings["nx"].Number(0);
			Nozzle.Y = settings["ny"].Number(0);
		}

		public virtual void OnUse() {
			
		}

#if DEBUG
		// The renderer editor (ItemRenderer.Debug.cs) is Debug-only, and so is its flag.
		private static bool snapGrid = true;
#endif
	}
}