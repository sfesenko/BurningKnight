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
		private void AnimateConsumableChange(int amount, int now, ItemType type) {
			if (type == ItemType.Bomb) {
				if (Math.Abs(amount) == 1) {
					bombs = now;
				} else {
					Tween.To(this, new {bombs = now}, 0.05f * Math.Abs(amount));					
				}
				
				Tween.To(0.3f, bombScale.X, x => bombScale.X = x, 0.1f).OnEnd = () =>
					Tween.To(1f, bombScale.X, x => bombScale.X = x, 0.2f);

				Tween.To(2f, bombScale.Y, x => bombScale.Y = x, 0.1f).OnEnd = () =>
					Tween.To(1f, bombScale.Y, x => bombScale.Y = x, 0.2f);
			} else if (type == ItemType.Key) {
				if (Math.Abs(amount) == 1) {
					keys = now;
				} else {
					Tween.To(this, new {keys = now}, 0.05f * Math.Abs(amount));					
				}
				
				Tween.To(0.3f, keyScale.X, x => keyScale.X = x, 0.1f).OnEnd = () =>
					Tween.To(1f, keyScale.X, x => keyScale.X = x, 0.2f);

				Tween.To(2f, keyScale.Y, x => keyScale.Y = x, 0.1f).OnEnd = () =>
					Tween.To(1f, keyScale.Y, x => keyScale.Y = x, 0.2f);
			} else if (type == ItemType.Coin) {
				if (Math.Abs(amount) == 1) {
					coins = now;
				} else {
					Tween.To(this, new {coins = now}, 0.05f * Math.Abs(amount));					
				}
				
				Tween.To(2f, coinScale.Y, x => coinScale.Y = x, 0.1f).OnEnd = () =>
					Tween.To(1f, coinScale.Y, x => coinScale.Y = x, 0.2f);
			}
		}
		private void AddArtifact(Item item) {
			if (item.Hide || multiplayer) {
				return;
			}
			
			UiItem? old = null;

			foreach (var i in items) {
				if (i.Id == item.Id) {
					old = i;
					break;
				}
			}

			if (old == null) {
				var x = Display.UiWidth - 8f;

				if (items.Count > 0) {
					x = items[items.Count - 1].X - 8;
				}
								
				old = new UiItem();
				old.Id = item.Id;
				old.Scourged = item.Scourged;
				Area!.Add(old);
				items.Add(old);

				old.Right = x;
				old.Bottom = Display.UiHeight - 8f;

				if (items.Count > 6) {
					more.Label = $"+{items.Count - 6}";
					more.Enabled = true;
					more.Right = Display.UiWidth - 8;
					more.Bottom = Display.UiHeight - 5f;

					for (var i = 0; i < items.Count - 6; i++) {
						var it = items[i];
						it.X = Display.UiWidth + 32;
					}

					var ps = Display.UiWidth - 8f;

					ps -= more.Width + 8;
					
					for (var i = items.Count - 6; i < items.Count; i++) {
						var it = items[i];
						it.Right = ps;

						ps -= 8 + it.Width;
					}
				}
			} else {
				old.Count++;
			}
		}
		private void RemoveArtifact(Item item) {
			if (multiplayer) {
				return;
			}
			
			UiItem? old = null;
			var j = 0;

			foreach (var i in items) {
				if (i.Id == item.Id) {
					old = i;
					break;
				}		

				j++;
			}

			if (old == null) {
				return;
			}

			if (old.Count > 1) {
				old.Count--;
				return;
			}

			items.Remove(old);
			old.Done = true;

			for (var i = j; i < items.Count; i++) {
				items[i].Right = items[i - 1].X - 8;
			}
		}
	}
}
