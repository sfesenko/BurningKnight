using System;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.room;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.state;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	public partial class DiscoverSecretRoomsUse : ItemUse {
		private float chance;

		public override void Use(Entity entity, Item item) {				
			ExplosionMaker.CheckForCracks(Context.Level!, entity.GetComponent<RoomComponent>()!.Room!, entity);
		}

		public override bool HandleEvent(Event e) {
			if (e is RoomChangedEvent rce) {
				if (!Rnd.Chance(chance)) {
					return base.HandleEvent(e);
				}

				ExplosionMaker.CheckForCracks(Context.Level!, rce.New, rce.Who);
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			chance = settings["chance"].Number(100);
		}
	}
}