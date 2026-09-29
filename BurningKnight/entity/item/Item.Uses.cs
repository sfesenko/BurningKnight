using System;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.item.use;
using BurningKnight.entity.item.useCheck;
using BurningKnight.level;
using BurningKnight.level.rooms;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item {
	public partial class Item {
		public bool Use(Entity entity, bool avoidCheck = false) {
			if (!avoidCheck && (Type == ItemType.Weapon || Type == ItemType.Active) && !UseCheck.CanUse(entity, this)) {
				return false;
			}

			ForEachUse(use => use.Use(entity, this), true);

			Delay = Math.Abs(UseTime);

			entity.HandleEvent(new ItemUsedEvent {
				Item = this,
				Who = entity,
				Fake = avoidCheck
			});

			Used = true;
			Renderer?.OnUse();

			if (Type == ItemType.Active) {
				((Player) entity).AnimateItemPickup(this, null, false, false);
			}

			return true;
		}
		private void ForEachUse(Action<ItemUse> action, bool skipUsed = false) {
			foreach (var use in Uses) {
				if (skipUsed && use.SingleUse && Used) {
					continue;
				}

				try {
					action(use);
				} catch (Exception e) {
					Log.Error(e);
				}
			}
		}
		public void Pickup() {
			var entity = Owner;

			ForEachUse(use => use.Pickup(entity, this));
		}
		public void Drop() {
			var entity = Owner;

			ForEachUse(use => use.Drop(entity, this));
		}
		public void TakeOut() {
			var entity = Owner;

			ForEachUse(use => use.TakeOut(entity, this));
		}
		public void PutAway() {
			var entity = Owner;

			ForEachUse(use => use.PutAway(entity, this));
		}
	}
}
