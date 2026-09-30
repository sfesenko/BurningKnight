using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.fx;
using BurningKnight.save;
using BurningKnight.state;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.util.camera;
using Lens.util.file;
using Lens.physics;

namespace BurningKnight.level.entities {
	public partial class HiddenExit : SaveableEntity, PlaceableEntity {
		internal string id;
		
		private bool Interact(Entity entity) {
			foreach (var e in Area.Tagged[Tags.HiddenEntrance]) {
				if (e is HiddenEntrance h && h.id == id) {
					var state = (InGameState) Engine.Instance.State;
					Audio.PlaySfx("player_descending");			

					InGameState.TransitionToBlack(Center, () => {
						foreach (var p in Area.Tagged[Tags.Player]) {
							p.Center = e.Center;
						}

						state.ResetFollowing();
						Context.Camera!.Jump();
						InGameState.TransitionToOpen();
					});
					
					return true;
				}
			}
			
			return true;
		}

		private bool CanInteract(Entity e) {
			return true;
		}

		public override void AddComponents() {
			base.AddComponents();

			Width = 12;
			Height = 15;

			AddComponent(new InteractableComponent(Interact) {
				OnStart = entity => {
					if (entity is LocalPlayer) {
						Engine.Instance.State.Ui.Add(new InteractFx(this, Locale.Get("ascend")));
					}
				},
				
				CanInteract = CanInteract
			});
			
			AddTag(Tags.HiddenEntrance);
			
			AddComponent(new RectBodyComponent(0, 0, Width, Height, BodyType.Static, true));
			AddComponent(new InteractableSliceComponent("props", "ladder"));
			AddComponent(new ShadowComponent());
		}

		public override void Load(FileReader stream) {
			base.Load(stream);
			id = stream.ReadString();
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			if (id == null) {
				id = "";
			}
			
			stream.WriteString(id);
		}

	}
}