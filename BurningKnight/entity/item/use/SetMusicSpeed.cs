using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.tween;

namespace BurningKnight.entity.item.use {
	public partial class SetMusicSpeed : ItemUse {
		private float speed;

		public override void Use(Entity entity, Item item)
		{
			var audio = Context.Audio;
			Tween.To(speed, audio.Speed, x => audio.Speed = x, 0.4f);
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			speed = settings["speed"].Number(1f);
		}

	}
}