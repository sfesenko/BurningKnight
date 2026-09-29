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
		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			stream.WriteString(Id);
			stream.WriteBoolean(Used);
			stream.WriteBoolean(Touched);
			stream.WriteFloat(Delay);
			stream.WriteBoolean(Unknown);
			stream.WriteBoolean(Scourged);
			stream.WriteBoolean(Hide);
		}
		public void ConvertTo(string id) {
			var item = Items.Create(id);

			if (item == null) {
				Log.Error($"Failed to convert item {Id}, such id does not exist!");
				return;
			}

			if (HasComponent<AnimatedItemGraphicsComponent>()) {
				RemoveComponent<AnimatedItemGraphicsComponent>();
			} else if (HasComponent<ItemGraphicsComponent>()) {
				RemoveComponent<ItemGraphicsComponent>();
			}

			Uses = Items.ParseUses(Items.Datas[id].Uses);
			
			foreach (var u in Uses) {
				u.Item = this;
				u.Init();
			}
			
			UseTime = item.UseTime;
			Renderer = item.Renderer;
			Animation = item.Animation;
			AutoPickup = item.AutoPickup;
			Automatic = item.Automatic;
			SingleUse = item.SingleUse;
			Type = item.Type;
			Id = id;
			Used = false;
			Scourged = Scourged || item.Scourged;
			
			if (Renderer != null) {
				Renderer.Item = this;
			}
			
			if (Animation != null) {
				AddComponent(new AnimatedItemGraphicsComponent(Animation));
			} else {
				AddComponent(new ItemGraphicsComponent(Id));
			}
			
			if (HasBody()) {
				RemoveDroppedComponents();
				AddDroppedComponents();
			}
		}
		public override void Load(FileReader stream) {
			base.Load(stream);

			try {
				LoadedSelf = true;
				Id = stream.ReadString();
				Scourged = false;

				if (!Items.Has(Id)) {
					Id = "bk:revolver";
				}

				ConvertTo(Id);
				
				Used = stream.ReadBoolean();
				Touched = stream.ReadBoolean();
				Delay = stream.ReadFloat();
				Unknown = stream.ReadBoolean();

				var v = stream.ReadBoolean();
				Scourged = Scourged || v;

				Hide = stream.ReadBoolean();
			} catch (Exception e) {
				Log.Error(e);
			}
		}
	}
}
