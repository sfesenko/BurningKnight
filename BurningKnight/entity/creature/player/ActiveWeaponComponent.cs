using System;
using BurningKnight.assets;
using BurningKnight.assets.input;
using BurningKnight.entity;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.item;
using BurningKnight.entity.item.use;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.dialog;
using Lens.assets;
using Lens.entity.component.logic;
using Lens.input;

namespace BurningKnight.entity.creature.player {
	public class ActiveWeaponComponent : WeaponComponent {
		private bool stopped = true;
		private bool wasStickFiring;
		private float timeSinceReady;
		
		public ActiveWeaponComponent() {
			AtBack = false;
		}

		public override void Update(float dt) {
			base.Update(dt);

			var controller = GetComponent<InputComponent>();
			var data = controller!.GamepadEnabled ? controller.GamepadData : null;

			var lamp = GetComponent<LampComponent>()!.Item;
			var useLamp = lamp != null && lamp.Id == "bk:explosive_lamp";
			var Item = useLamp ? lamp : this.Item;

			if ((useLamp || !Disabled) && Item != null) {
				var ready = Item.Delay <= 0.001f;

				if (ready) {
					if (timeSinceReady <= 0.001f && Item.Type == ItemType.Weapon) {
						var t = Item.Data.WeaponType;
						
						if (t == WeaponType.Ranged) {
							foreach (var u in Item!.Uses!) {
								if (u is SimpleShootUse s) {
									if (s.ReloadSfx) {
										Entity.GetComponent<AudioEmitterComponent>()!.EmitRandomizedPrefixed("item_shotgun_reload", 2, 0.8f);
									}

									break;
								}
							}	
						} else if (t == WeaponType.Melee) {
							Entity.GetComponent<AudioEmitterComponent>()!.EmitRandomized("item_sword_cooldown", 0.5f);
						}
					}
					
					timeSinceReady += dt;
				} else {
					timeSinceReady = 0;
				}
				
				// Twin-stick: a deflected right stick aims (see Cursor) and fires, with hysteresis
				// (Cursor.StickFireEnter/Exit) so edge flicker does not stutter the trigger.
				var stickFiring = Cursor.StickFiring(data, wasStickFiring);
				var stickStarted = stickFiring && !wasStickFiring;
				var usePressed = Input.WasPressed(Controls.Use, controller) || stickStarted;
				var useDown = Input.IsDown(Controls.Use, controller) || stickFiring;
				wasStickFiring = stickFiring;

				// Snap the aim on a flick before any shot: this component runs before the cursor
				// entity, so the first shot would otherwise use the previous side.
				if (stickStarted && data != null && Entity.TryGetComponent<CursorComponent>(out var cursorComponent)) {
					cursorComponent.Cursor.SnapToStick(data);

					// Most weapons shoot via AimComponent.RealAim, which the weapon renderer
					// computes a frame late from a smoothed angle — refresh it too.
					if (Entity.TryGetComponent<AimComponent>(out var aim)) {
						aim.RealAim = cursorComponent.Cursor.GamePosition;
					}
				}
				
				var b = GetComponent<BuffsComponent>();
				
				if (b!.Has<FrozenBuff>() || b.Has<CharmedBuff>() || GetComponent<StateComponent>()!.StateInstance is Player.RollState) {
					return;
				}
				
				// Semi-autos keep firing while held: the 0.2s grace after ready is intentional,
				// so holding the trigger past the cooldown still shoots.
				if ((usePressed || (data != null && (
					data.DPadDownCheck || data.DPadLeftCheck || data.DPadUpCheck || data.DPadRightCheck                                                  
				  ))) || ((Item.Automatic || timeSinceReady > 0.2f || (data != null && Input.IsDownOnController(Controls.Use, data))) && useDown && ready)) {
				  
					if (!Entity.TryGetComponent<PlayerInputComponent>(out var d) || d.InDialog) {
						return;
					}

					if (GetComponent<StateComponent>()!.StateInstance is Player.SleepingState) {
						GetComponent<StateComponent>()!.Become<Player.IdleState>();
					}
					
					if (Context.Run.Depth == -2) {
						GetComponent<DialogComponent>()!.Close();
					}

					if (b.Has<InvisibleBuff>()) {
						b.Remove<InvisibleBuff>();
					}

					if (useLamp) {
						GetComponent<ConsumablesComponent>()!.SpawnBomb();
						Item.Delay = 0.5f;
					} else {
						Item.Use((Player) Entity);
					}

					GetComponent<StatsComponent>()!.UsedWeaponInRoom = true;
				}
			} else {
				timeSinceReady = 0;
			}
	
			if ((Input.WasPressed(Controls.Swap, controller) || (Input.Mouse.WheelDelta != 0 && stopped)) && Context.Run.Depth > 0 && GetComponent<WeaponComponent>()!.Item != null) {
				if (!GetComponent<InventoryComponent>()!.Busy) {
					stopped = false;
					Swap();
				}
			}

			stopped = Input.Mouse.WheelDelta == 0;
		}

		protected override bool ShouldReplace(Item item) {
			return item.Type == ItemType.Weapon && (Item == null || Context.Run.Depth < 1 || Entity.GetComponent<WeaponComponent>()!.Item != null);
		}

		protected override void OnItemSet(Item previous) {
			base.OnItemSet(previous);
			
			previous?.PutAway();
			Item?.TakeOut();

			if (Item != null && InGameState.Ready) {
				Audio.PlaySfx(Item.Data.WeaponType.GetSwapSfx());
			}
			
			if (Context.Run.Depth == -2) {
				var dialog = GetComponent<DialogComponent>();
								
				dialog!.Dialog!.Str!.ClearIcons();
				// No keyboard on a handheld: the pad keycap below is the whole hint.
				if (TextInput.Available) {
					dialog.Dialog.Str.AddIcon(CommonAse.Ui.GetSlice(Controls.FindSlice(Controls.Use, false)!)!);
				}

				if (GamepadComponent.Current != null && GamepadComponent.Current.Attached) {
					dialog.Dialog.Str.AddIcon(CommonAse.Ui.GetSlice(Controls.FindSlice(Controls.Use, true)!)!);
				}
								
				dialog.StartAndClose("control_2", 5);
			}
		}
	}
}