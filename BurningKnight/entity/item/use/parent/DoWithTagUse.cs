using System.Collections.Generic;
using BurningKnight.entity.component;
using Lens.entity;
using Lens.lightJson;
using Lens.util.math;

namespace BurningKnight.entity.item.use.parent {
	public abstract partial class DoWithTagUse : ItemUse {
		private bool self;
		private bool sameRoom;
		private bool all;
		private int tag;

		public override void Use(Entity entity, Item item) {
			var list = new List<Entity>();

			if (self) {
				list.Add(entity);
			}

			var tags = (sameRoom ? entity.GetComponent<RoomComponent>()!.Room.Tagged : entity.Area.Tagged);

			for (var i = 0; i < BitTag.Total; i++) {
				if ((tag & 1 << i) != 0) {
					var l = tags[i];
					
					if (all) {
						list.AddRange(l);
					} else if (l.Count > 0) {
						list.Add(l[Rnd.Int(l.Count)]);
					}
				}
			}
			
			DoAction(entity, item, list);
		}

		protected abstract void DoAction(Entity entity, Item item, List<Entity> entities);

		public override void Setup(JsonValue settings) {
			base.Setup(settings);
				
			self = settings["self"].Bool(false);
			sameRoom = settings["same_room"].Bool(true);
			all = settings["all"].Bool(true);
			tag = settings["tag"].Int(1);
		}
		
	}
}