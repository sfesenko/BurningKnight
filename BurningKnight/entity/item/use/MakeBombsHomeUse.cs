using BurningKnight.entity.bomb.controller;
using BurningKnight.entity.events;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class MakeBombsHomeUse : ItemUse {
		private float speed;
		
		public override bool HandleEvent(Event e) {
			if (e is BombPlacedEvent bce) {
				bce.Bomb.ExplodeOnTouch = true;
				bce.Bomb.Controller += TargetBombController.Make(null, speed);
			}
			
			return base.HandleEvent(e);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			speed = settings["speed"].Number(1);
		}
	}
}