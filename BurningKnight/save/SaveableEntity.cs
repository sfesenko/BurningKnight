using System.Linq;
using Lens.assets;
using Lens.entity;
using Lens.entity.component;
using Lens.util.file;

namespace BurningKnight.save {
	public class SaveableEntity : Entity {
		// The context of the world this entity lives in, or the current game's when it is in a
		// plain Area (prefabs, the test harness).
		public GameContext Context => (Area as GameArea)?.Context ?? GameContext.Current;

		// The game's audio, through the context rather than the engine's singleton; the engine's
		// own code keeps its property (it is the device, and there is one per process).
		protected static new Audio Audio => GameContext.Current.Audio;

		public override void AddComponents() {
			base.AddComponents();
			
			AddTag(Tags.LevelSave);
		}

		public virtual void Save(FileWriter stream) {
			stream.WriteInt32((int) X);
			stream.WriteInt32((int) Y);

			if (Components == null) {
				return;
			}
			
			foreach (var component in Components.Values) {
				if (component is SaveableComponent saveable) {
					saveable.Save(stream);
				}
			}
		}

		public virtual void Load(FileReader stream) {
			X = stream.ReadInt32();
			Y = stream.ReadInt32();
			
			if (Components == null) {
				return;
			}

			// So that we can edit the components
			// Tho those new components wont get loaded
			var values = Components.Values.ToArray();
			
			foreach (var component in values) {
				if (component is SaveableComponent saveable) {
					saveable.Load(stream);
				}
			}
		}
	}
}