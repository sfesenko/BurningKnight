using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.entity;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.item.use;
using BurningKnight.level.entities;
using BurningKnight.level.rooms;
using BurningKnight.state;
using BurningKnight.ui.str;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.input;
using Lens.util;
using Lens.util.camera;
using Lens.util.tween;
using Microsoft.Xna.Framework;

namespace BurningKnight.ui.inventory {
	public partial class UiInventory : UiEntity {
		public TextureRegion ItemSlot;
		public TextureRegion UseSlot;
		
		private TextureRegion question;		
		private TextureRegion bomb;
		private TextureRegion key;
		private TextureRegion coin;
		private TextureRegion pointer;
		private TextureRegion exitPointer;
		
		private UiString description;
		private UiItem lastItem;

		public static TextureRegion Heart;
		public static TextureRegion HalfHeart;
		public static TextureRegion HeartBackground;
		private TextureRegion changedHeartBackground;
		private static TextureRegion halfHeartBackground;
		private TextureRegion changedHalfHeartBackground;

		public static TextureRegion veganHeart;
		public static TextureRegion veganHalfHeart;
		public static TextureRegion veganHeartBackground;
		private TextureRegion veganchangedHeartBackground;
		private static TextureRegion veganhalfHeartBackground;
		private TextureRegion veganchangedHalfHeartBackground;
		
		public static TextureRegion Mana;
		public static TextureRegion HalfMana;
		public static TextureRegion ManaBackground;
		public static TextureRegion ChangedManaBackground;
		
		public static TextureRegion Bomb;
		public static TextureRegion BombBg;
		public static TextureRegion ChangedBombBg;
		
		public static TextureRegion ShieldBackground;
		private TextureRegion changedShieldBackground;
		private static TextureRegion halfShieldBackground;
		private TextureRegion changedHalfShieldBackground;
		private UiButton more;
		
		public Player Player;

		private int coins;
		private int keys;
		private int bombs;
		
		private Vector2 coinScale = new Vector2(1);
		private Vector2 keyScale = new Vector2(1);
		private Vector2 bombScale = new Vector2(1);

		private List<UiItem> items = new List<UiItem>();
		private UiActiveItemSlot activeSlot;
		private UiWeaponSlot weaponSlot;
		private UiWeaponSlot activeWeaponSlot;

		private bool multiplayer;
		public bool Second;
		public bool ForceUpdate;

		public UiInventory(Player player, bool multiplayer) {
			Player = player;	
			activeSlot = new UiActiveItemSlot(this);

			if (player.HasComponent<WeaponComponent>()) {
				weaponSlot = new UiWeaponSlot(this);
			}

			activeWeaponSlot = new UiWeaponSlot(this) {
				Active = true
			};

			this.multiplayer = multiplayer;
			Second = multiplayer && player.GetComponent<InputComponent>().Index > 0;
		}

		public override void Init() {
			base.Init();

			description = new UiString(Font.Small);
			((InGameState) Engine.Instance.State).TopUi.Add(description);
			((InGameState) Engine.Instance.State).TopUi.Add(new RenderTrigger(RenderTop, 10));
			description.DisableRender = true;
			
			Area.Add(activeSlot);

			if (weaponSlot != null) {
				Area.Add(weaponSlot);
			}

			Area.Add(activeWeaponSlot);

			var anim = Animations.Get("ui");

			ItemSlot = anim.GetSlice("item_slot");
			UseSlot = new TextureRegion();
			UseSlot.Set(ItemSlot);
			
			question = anim.GetSlice("question");
			bomb = anim.GetSlice("bomb");
			key = anim.GetSlice("key");
			coin = anim.GetSlice("coin");
			pointer = anim.GetSlice("pointer");
			exitPointer = anim.GetSlice("exit_pointer");

			Heart = anim.GetSlice("heart");
			HalfHeart = anim.GetSlice("half_heart");
			HeartBackground = anim.GetSlice("heart_bg");
			changedHeartBackground = anim.GetSlice("heart_hurt_bg");
			halfHeartBackground = anim.GetSlice("half_heart_bg");
			changedHalfHeartBackground = anim.GetSlice("half_heart_hurt");
			
			veganHeart = anim.GetSlice("vegan");
			veganHalfHeart = anim.GetSlice("half_vegan");
			veganHeartBackground = anim.GetSlice("vegan_bg");
			veganchangedHeartBackground = anim.GetSlice("vegan_hurt_bg");
			veganhalfHeartBackground = anim.GetSlice("half_vegan_bg");
			veganchangedHalfHeartBackground = anim.GetSlice("half_vegan_hurt_bg");
			
			Bomb = anim.GetSlice("bmb");
			BombBg = anim.GetSlice("bmb_bg");
			ChangedBombBg = anim.GetSlice("bmb_hurt");
			
			Mana = anim.GetSlice("mana");
			HalfMana = anim.GetSlice("half_mana");
			ManaBackground = anim.GetSlice("mana_bg");
			ChangedManaBackground = anim.GetSlice("mana_hurt_bg");
			
			ShieldBackground = anim.GetSlice("shield_bg");
			changedShieldBackground = anim.GetSlice("shield_hurt");
			halfShieldBackground = anim.GetSlice("half_shield_bg");
			changedHalfShieldBackground = anim.GetSlice("half_shield_hurt");
			
			if (Player != null) {
				var component = Player.GetComponent<ConsumablesComponent>();

				coins = component.Coins;
				keys = component.Keys;
				bombs = component.Bombs;

				var area = Player.Area;

				Subscribe<ConsumableAddedEvent>(area);
				Subscribe<ConsumableRemovedEvent>(area);
				Subscribe<ItemUsedEvent>(area);
				Subscribe<ItemAddedEvent>(area);
				Subscribe<ItemRemovedEvent>(area);
				Subscribe<RerollItemsOnPlayerUse.RerolledEvent>(area);

				more = new UiButton();
				more.Font = Font.Small;

				more.Click = (b) => {
					var state = (InGameState) Engine.Instance.State;
					state.OnPauseCallback = state.GoToInventory;
					state.Paused = true;
				};

				more.Enabled = false;
				Area.Add(more);
				more.Right = Display.UiWidth - 8;
				more.Bottom = Display.UiHeight - 5;
				
				var inventory = Player.GetComponent<InventoryComponent>();

				foreach (var item in inventory.Items) {
					AddArtifact(item);
				}
			}
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (ForceUpdate) {
				ForceUpdate = false;
				UpdateConsumables();
			}
		}

		public void UpdateConsumables() {
			var c = Player.GetComponent<ConsumablesComponent>();

			bombs = c.Bombs;
			keys = c.Keys;
			coins = c.Coins;
		}

		public override bool HandleEvent(Event e) {
			switch (e) {
				case ConsumableAddedEvent add: {
					AnimateConsumableChange(add.Amount, add.TotalNow, add.Type);
					break;
				}

				case ConsumableRemovedEvent rem: {
					AnimateConsumableChange(rem.Amount, rem.TotalNow, rem.Type);
					break;
				}

				case RerollItemsOnPlayerUse.RerolledEvent re: {
					if (re.Entity == Player) {
						foreach (var i in items) {
							i.Done = true;
						}						
						
						items.Clear();
						var inventory = Player.GetComponent<InventoryComponent>();

						foreach (var item in inventory.Items) {
							AddArtifact(item);
						}
					}

					break;
				}
				
				case ItemAddedEvent iae: {
					if (iae.Who == Player) {
						var item = iae.Item;
								
						if (item.Type == ItemType.Artifact || item.Type == ItemType.Scourge) {
							AddArtifact(item);
						}
					}
					
					break;
				}

				case ItemRemovedEvent ire: {
					if (ire.Owner == Player) {
						RemoveArtifact(ire.Item);
					}

					break;
				}
			}

			return base.HandleEvent(e);
		}
		
		private int Bump(int value) {
			return value % 2 == 0 ? value : value + 1;
		}

		private float lastRed;
		
		private float lastMana;
		private float changedTime;

		private float Wrap(float v) {
			return Second ? Display.UiWidth - v : v;
		}

		private void PrintString(string s, float x, float y) {
			Graphics.Print(s, Font.Small, new Vector2(Second ? Display.UiWidth - x - Font.Small.MeasureString(s).Width - 1 : 18, y));
		}
	}
}