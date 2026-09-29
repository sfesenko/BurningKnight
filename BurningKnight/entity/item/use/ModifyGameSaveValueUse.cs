using BurningKnight.save;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	public partial class ModifyGameSaveValueUse : ItemUse {
		private string id;
		private float amount;
		private bool over;

		public override void Use(Entity entity, Item item) {
			base.Use(entity, item);

			if (item.Used) {
				return;
			}
			
			GameSave.Put(id, over ? amount : GameSave.GetFloat(id) + amount);
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
			
			id = settings["idd"].String("");
			amount = settings["am"].Number(0);
			over = settings["ov"].Bool(false);
		}
	}
}