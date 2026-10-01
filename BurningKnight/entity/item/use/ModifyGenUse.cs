using BurningKnight.entity.projectile;
using BurningKnight.save;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class ModifyGenUse : ItemUse {
		private bool xlLevel;
		private float chestRewardChance;
		private float mobDest;
		private float mimicChance;
		private bool genMarket;
		private bool shops;
		private bool treasure;
		private bool melee;

		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);

			if (xlLevel) {
				LevelSave.XL = true;
			}

			LevelSave.ChestRewardChance += chestRewardChance;
			LevelSave.MobDestructionChance += mobDest;
			LevelSave.MimicChance += mimicChance;
			LevelSave.GenerateMarket = genMarket;
			LevelSave.GenerateShops = shops;
			LevelSave.GenerateTreasure = treasure;
			LevelSave.MeleeOnly = melee;
		}

		public override void Setup(JsonNode settings) {
			base.Setup(settings);
			xlLevel = settings["xl"].Bool(false);
			chestRewardChance = settings["crc"].Number(0);
			mobDest = settings["md"].Number(0);
			mimicChance = settings["mimic"].Number(0);
			genMarket = settings["gm"].Bool(false);
			shops = settings["sp"].Bool(false);
			treasure = settings["tr"].Bool(false);
			melee = settings["ml"].Bool(false);
		}

	}
}