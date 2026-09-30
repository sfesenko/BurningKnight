using BurningKnight.assets.achievements;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.fx;
using BurningKnight.level.entities.exit;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.util.file;
using Lens.physics;

namespace BurningKnight.level.entities {
	public partial class Exit : SaveableEntity, PlaceableEntity {
		public int To;
		public static Exit Instance;
		
		public override void Init() {
			base.Init();
			Depth = Layers.Entrance;
			Instance = this;
		}

		public override void Destroy() {
			base.Destroy();

			if (Instance == this) {
				Instance = null;
			}
		}

		protected virtual bool CanUse() {
			return true;
		}
		
		protected virtual bool Interact(Entity entity) {
			if (!CanUse()) {
				AnimationUtil.ActionFailed();
				return true;
			}
			
			entity.RemoveComponent<PlayerInputComponent>();
			entity.GetComponent<HealthComponent>()!.Unhittable = true;
			
			if (Context.Run.Depth == Context.Run.ContentEndDepth || (Context.Run.Type == RunType.BossRush && Context.Run.Depth == 5)) {
				if (Context.Run.Type == RunType.Regular) {
					Context.Run.ActualDepth = -1;
					Context.Run.Depth = 1;
					Context.Run.Loop++;
					
					Achievements.Unlock("bk:loop");
				} else {
					Context.Run.Win();
				}
			} else {
				InGameState.TransitionToBlack(entity.Center, () => {
					Context.Run.NumPlayers = 0;
					Descend();
				});
			}

			Audio.PlaySfx("player_descending");			
			return true;
		}
		
		protected virtual void Descend() {
			if (Context.Run.Depth == -2) {
				Achievements.Unlock("bk:tutorial");
				GlobalSave.Put("finished_tutorial", true);
				Context.Run.Depth = 0;
			} else if (To == 1 || this is BossRushExit) {
				Context.Run.NumPlayers = Area.Tagged[Tags.Player].Count;
				Context.Run.StartNew();
				// Caves secret location
			} else if (Context.Run.Depth == 13) {
				Context.Run.Depth = 4;
			} else {
				Context.Run.Depth = To;
			}
		}

		protected virtual string GetFxText() {
			return Locale.Get(Context.Run.Depth == 0 ? "new_run" : "descend");
		}

		public override void AddComponents() {
			base.AddComponents();

			Width = 16;
			Height = 14;

			if (To != 13) {
				To = Context.Run.Depth + 1;
			}

			AddComponent(new InteractableComponent(Interact) {
				OnStart = entity => {
					if (entity is LocalPlayer && Context.Run.Depth != -2) {
						Engine.Instance.State.Ui.Add(new InteractFx(this, GetFxText()));
					}
				},
				
				CanInteract = CanInteract
			});
			
			AddComponent(new RectBodyComponent(0, 0, Width, Height, BodyType.Static, true));
		}

		protected virtual bool CanInteract(Entity e) {
			return true;
		}

		public override void PostInit() {
			base.PostInit();
			AddComponent(new InteractableSliceComponent("props", GetSlice()));
		}

		protected virtual string GetSlice() {
			return To == 13 ? "emerald_exit" : "exit";
		}
		
		public override void Load(FileReader stream) {
			base.Load(stream);
			To = stream.ReadInt16();
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			stream.WriteInt16((short) To);
		}

	}
}