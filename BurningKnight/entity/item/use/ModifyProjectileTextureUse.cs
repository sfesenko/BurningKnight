using BurningKnight.entity.creature.player;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ModifyProjectileTextureUse : ItemUse {
		private string texture = null!;
		
		public override void Use(Entity entity, Item item) {
			if (entity is Player p) {
				p.ProjectileTexture = texture;
			}
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			texture = settings["texture"].String("rect");
		}

	}
}