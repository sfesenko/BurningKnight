using BurningKnight.entity.component;
using BurningKnight.entity.events;
using Lens.entity;

namespace BurningKnight.entity.buff {
	public class BrokenArmorBuff : Buff {
		public const string Id = "bk:broken_armor";
		
		public BrokenArmorBuff() : base(Id) {
			Duration = 10;
		}

		public override void Init() {
			base.Init();
			Entity.GetComponent<BuffsComponent>()!.Remove<ArmoredBuff>();
		}

		public override void HandleEvent(Event e) {
			if (e is HealthModifiedEvent hme) {
				if (hme.Amount < 0) {
					hme.Amount *= 2;
				}
			}
			
			base.HandleEvent(e);
		}

		public override string GetIcon() {
			return "broken_armor";
		}
	}
}