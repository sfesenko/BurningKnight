using System;
using BurningKnight.state;
using Lens.entity;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public partial class UseOnEventUse : ItemUse {
		private string type;
		private Type typeInstance;
		private string use;
		private JsonValue options;

		public override void Setup(JsonValue settings) {
			base.Setup(settings);

			type = settings["tp"].String("");
			use = settings["use"].String("");
			options = settings["us"];

			if (options == JsonValue.Null) {
				options = new JsonObject();
			}

			try {
				typeInstance = Type.GetType(type, true, false);
			} catch (Exception e) {
				Log.Error(e);
			}
		}

		public override bool HandleEvent(Event e) {
			if (e.GetType() == typeInstance) {
				var u = UseRegistry.Create(use);

				if (u == null) {
					Log.Error($"{use} is invalid item use id");
				} else {
					u.Item = Item;
					u.Setup(options);
					u.Use(Item.Owner, Item);
				}
			}
			
			return base.HandleEvent(e);
		}
	}
}