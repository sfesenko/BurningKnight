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
	public partial class UiInventory {
		public void RenderArrow(Vector2 target, bool arg = false) {
			var d = Player.DistanceTo(target);
			var spr = arg ? exitPointer : pointer;

			if (d > 64) {
				var dd = d * 0.7f;
				var a = Player.AngleTo(target);
				var m = (float) Math.Cos(Engine.Time * 4f) * 6f + 6f;
				var v = (float) Math.Cos(Engine.Time * 5f) * 0.3f + 0.7f;

				var point = Player.Center + new Vector2((float) Math.Cos(a) * dd, (float) Math.Sin(a) * dd);
				var center = new Vector2(Display.UiWidth, Display.UiHeight) * 0.5f;
					
				point = Camera.Instance.CameraToUi(point);
				point.X = MathUtils.Clamp((float) -Math.Cos(a) * (center.X - 16) + center.X, (float) Math.Cos(a) * (center.X - 16) + center.X, point.X);
				point.Y = MathUtils.Clamp((float) -Math.Sin(a) * (center.Y - 16) + center.Y,  (float) Math.Sin(a) * (center.Y - 16) + center.Y, point.Y);
				point -= MathUtils.CreateVector(a, m);
					
				Graphics.Color = new Color(v, v, v, 1f - MathUtils.Clamp(0, 1, (80 - d) / 16f));
				Graphics.Render(spr, point, a, spr.Center);
				Graphics.Color = ColorUtils.WhiteColor;
			}
		}
		public override void Render() {
			if (Player == null || Player.Done) {
				Done = true;
				return;
			}
			
			Entity target;
			var r = Player.GetComponent<RoomComponent>().Room;
			
			if (!Second && !Engine.Instance.State.Paused && r != null) {
				if (r.Tagged[Tags.MustBeKilled].Count == 1 && r.Type != RoomType.Connection && !(r.Tagged[Tags.MustBeKilled][0] is Boss)) {
					target = r.Tagged[Tags.MustBeKilled][0];

					if (target != null && (!(target is Mob mb) || mb.Target != null) && target is Creature c && c.GetComponent<HealthComponent>().Health >= 1f && r.Contains(c.Center)) {
						RenderArrow(target.Center);
					}
				} else if (Run.Depth > 0 && r.Tagged[Tags.MustBeKilled].Count == 0 && Exit.Instance != null && Player.CheckClear(Engine.Instance.State.Area)) {
					RenderArrow(Exit.Instance.Center, true);
				}
			}

			var show = Run.Depth > 0;
			var hasMana = Player.GetComponent<WeaponComponent>().Item?.Data?.WeaponType == WeaponType.Magic || Player.GetComponent<ActiveWeaponComponent>().Item?.Data?.WeaponType == WeaponType.Magic;

			RenderHealthBar(true);

			if (show && hasMana) {
				RenderMana();
			}

			if ((show || Run.Depth == -2) && Player != null && Player.GetComponent<ConsumablesComponent>() == Player.ForceGetComponent<ConsumablesComponent>()) {
				RenderConsumables(hasMana);
			}
		}
		private void RenderTop() {
			var show = Run.Depth > 0;

			if (show && Player != null) {
				if (UiItem.Hovered != null) {
					var item = UiItem.Hovered;

					if (lastItem != UiItem.Hovered) {
						lastItem = UiItem.Hovered;
						description.Label = item.Description;
						description.FinishTyping();
					}

					var x = MathUtils.Clamp(item.OnTop ? 40 : 4,
						Display.UiWidth - 6 - Math.Max(item.DescriptionSize.X, item.NameSize.X), item.Position.X);

					var y = item.OnTop
						? MathUtils.Clamp(8 + item.NameSize.Y, Display.UiHeight - 6 - item.DescriptionSize.Y, item.Y)
						: MathUtils.Clamp(4, Display.UiHeight - 6 - item.DescriptionSize.Y - item.NameSize.Y - 4, item.Y - 7);

					Graphics.Color = new Color(1f, 1f, 1f, item.TextA);
					Graphics.Print(item.Name, Font.Small, new Vector2(x, y - item.DescriptionSize.Y + 2));

					// Graphics.Print(item.Description, Font.Small, new Vector2(x, y));

					description.X = x;
					description.Y = y - 2;
					Graphics.Color = ColorUtils.WhiteColor;

					description.Tint.A = (byte) (item.TextA * 255f);
					description.RenderString();
				}
			}
		}
		private Vector2 GetHeartPosition(bool pad, int i, bool bg = false) {
			var d = 0;
			var it = Player.GetComponent<ActiveItemComponent>().Item;

			if (pad && it != null && Math.Abs(it.UseTime) > 0.01f) {
				d = 4;
			}

			var a = (pad ? (4 + (4 + ItemSlot.Source.Width + d) * (activeSlot.ActivePosition + 1)) : 6) + 4;
			var c = Second ? Math.Min(HeartsComponent.PerRow, Bump(Player.GetComponent<HealthComponent>().MaxHealth + Player.GetComponent<HeartsComponent>().TotalMax)) : 0;
			
			return new Vector2(
				(bg ? 0 : 1) + (Second ? Display.UiWidth - a - c * 5.5f : a) + (int) (i % HeartsComponent.PerRow * 5.5f),
				(bg ? 0 : 1) + (i / HeartsComponent.PerRow) * 10 + 11
				+ (float) Math.Cos(i / 8f * Math.PI + Engine.Time * 12) * 0.5f * Math.Max(0, (float) (Math.Cos(Engine.Time * 0.25f) - 0.9f) * 10f)
			);
		}
		private void RenderHealthBar(bool pad) {
			var red = Player.GetComponent<HealthComponent>();
			var phases = red.Phases;

			if (Scourge.IsEnabled(Scourge.OfRisk)) {
				Graphics.Render(question, new Vector2(8 + (int) ((4 + ItemSlot.Source.Width) * (activeSlot.ActivePosition + 1)), 11));

				if (phases > 0) {
					Graphics.Print($"x{phases}", Font.Small, new Vector2(8 + question.Width + 4 + (int) ((4 + ItemSlot.Source.Width) * (activeSlot.ActivePosition + 1)), 12));
				}
				
				return;
			}
			
			var hearts = Player.GetComponent<HeartsComponent>();
			var totalRed = red.Health;

			if (lastRed > totalRed) {
				lastRed = totalRed;
			} else if (lastRed < totalRed) { 
				lastRed = Math.Min(totalRed, lastRed + Engine.Delta * 30);
			}

			var r = (int) lastRed;
			var maxRed = red.MaxHealth;
			var hurt = red.InvincibilityTimer > 0;
			var shields = hearts.ShieldHalfs;
			var bombs = (int) hearts.Bombs;
			var dbombs = bombs * 2;
			var mbombs = (int) hearts.BombsMax;
			var mdbombs = mbombs * 2;

			var n = r;
			var jn = maxRed;
			
			if (jn % 2 == 1) {
				jn++;
			}

			var vegan = Settings.Vegan;
			
			for (var i = 0; i < maxRed; i += 2) {
				var region = hurt ? (vegan ? veganchangedHeartBackground : changedHeartBackground) : (vegan ? veganHeartBackground : HeartBackground);

				if (i == maxRed - 1) {
					region = hurt ? (vegan ? veganchangedHalfHeartBackground : changedHalfHeartBackground) : (vegan ? veganhalfHeartBackground : halfHeartBackground);
				}
				
				Graphics.Render(region, GetHeartPosition(pad, i, true));
			}
			
			for (var i = jn; i < maxRed + mdbombs; i += 2) {
				var region = hurt ? ChangedBombBg : BombBg;
				
				Graphics.Render(region, GetHeartPosition(pad, i, true) + new Vector2(0, -1));
			}
			
			for (var i = jn + mdbombs; i < maxRed + shields + mdbombs; i += 2) {
				var region = hurt ? changedShieldBackground : ShieldBackground;

				if (i == maxRed + shields - 1) {
					region = hurt ? changedHalfShieldBackground : halfShieldBackground;
				}
				
				Graphics.Render(region, GetHeartPosition(pad, i, true));
			}

			for (var j = 0; j < n; j++) {
				var h = j % 2 == 0;
				Graphics.Render(h ? (vegan ? veganHalfHeart : HalfHeart) : (vegan ? veganHeart : Heart), GetHeartPosition(pad, j) + (h ? Vector2.Zero : new Vector2(-1, 0)));
			}
			
			for (var j = jn; j < maxRed + dbombs; j += 2) {
				Graphics.Render(Bomb, GetHeartPosition(pad, j) + new Vector2(0, -1));
			}
			
			if (phases > 0) {
				Graphics.Print($"x{phases}", Font.Small, GetHeartPosition(pad, Math.Min(15, maxRed + shields + bombs)) + new Vector2(4, -2));
			}
		}
		private Vector2 GetStarPosition(bool pad, int i, bool bg = false) {
			var d = 0;
			var it = Player.GetComponent<ActiveItemComponent>().Item;

			if (pad && it != null && Math.Abs(it.UseTime) > 0.01f) {
				d = 4;
			}

			var a = (pad ? 4 : 6) + (8 + ItemSlot.Source.Width + d) * (activeSlot.ActivePosition + 1) + 4 +
			        (int) (i % HeartsComponent.PerRow * 11f);
			
			return new Vector2(
				(bg ? 0 : 1) + (Second ? Display.UiWidth - a - 8 : a) - 2,
				(bg ? 0 : 1) + (i / HeartsComponent.PerRow) * 10 + 11 + (Player.GetComponent<HealthComponent>().MaxHealth + Player.GetComponent<HeartsComponent>().Total > HeartsComponent.PerRow ? 10 : 0) + 10
				+ (float) Math.Cos(i / 8f * Math.PI + Engine.Time * 12 - 1) * 0.5f * Math.Max(0, (float) (Math.Cos(Engine.Time * 0.25f - 1) - 0.9f) * 10f)
			);
		}
		private void RenderMana() {
			var manaComponent = Player.GetComponent<ManaComponent>();
			
			var totalMana = manaComponent.Mana;
			
			if (changedTime > 0) {
				changedTime -= Engine.Delta;
			}

			if (lastMana > totalMana) {
				lastMana = totalMana;
				changedTime = 0.5f;
			} else if (lastMana < totalMana) { 
				lastMana = Math.Min(totalMana, lastMana + Engine.Delta * 30);
				changedTime = 0.5f;
			}


			var r = (int) lastMana;
			var maxMana = manaComponent.ManaMax;
			var hurt = changedTime > 0;

			var n = r;
			var jn = maxMana;
			
			for (var i = 0; i < maxMana / 2; i++) {
				Graphics.Render(hurt ? ChangedManaBackground : ManaBackground, GetStarPosition(false, i, true) + (hurt ? new Vector2(-1) : Vector2.Zero));
			}

			for (var j = 0; j < n; j += 2) {
				Graphics.Render(j == r - 1 ? HalfMana : Mana, GetStarPosition(false, j / 2));
			}
		}
		private void RenderConsumables(bool hasMana) {
			var bottomY = 8 + 9 + 8 + (hasMana ? 10 : 0) + (Player.GetComponent<HealthComponent>().MaxHealth + Player.GetComponent<HeartsComponent>().Total > HeartsComponent.PerRow ? 10 : 0) + (int) (12 * (activeSlot.ActivePosition + 1));

			if (Scourge.IsEnabled(Scourge.OfKeys)) {
				Graphics.Render(question, new Vector2(8, bottomY + 1));
				return;
			}
			
			Graphics.Render(coin, new Vector2(Wrap(8 + coin.Center.X), bottomY + 1 + coin.Center.Y), 0, coin.Center, coinScale);
			PrintString($"{coins}", 18, bottomY - 1);
			bottomY += 12;

			Graphics.Render(key, new Vector2(Wrap(7 + key.Center.X), bottomY + key.Center.Y + 2), 0, key.Center, keyScale);
			PrintString($"{keys}", 18, bottomY - 1);
			bottomY += bomb.Source.Height + 2;

			Graphics.Render(bomb, new Vector2(Wrap(8 + bomb.Center.X), bottomY + bomb.Center.Y), 0,
				bomb.Center, bombScale);
			PrintString($"{bombs}", 18, bottomY - 1);
		}
	}
}
