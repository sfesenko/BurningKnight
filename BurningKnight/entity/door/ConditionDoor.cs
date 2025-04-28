using BurningKnight.assets.achievements;
using BurningKnight.entity.creature.npc;
using BurningKnight.save;
using ImGuiNET;
using Lens.entity;
using Lens.util.file;

namespace BurningKnight.entity.door {
	public class ConditionDoor : LockableDoor {
		private static string[] conditions =
		[
			"Played Once",
			"Saved Hat Trader",
			"Saved Weapon Trader",
			"Saved Artifact Trader",
			"Saved Active Trader",
			"Save Boss Rush Guy",
			"Completed 10 Challenges",
			"Completed 20 Challenges",
			"Completed 30 Challenges",
			"Achievement Branch A Complete",
			"Achievement Branch B Complete",
			"Achievement Branch C Complete",
			"Achievement Branch D Complete"
		];

		private bool shouldLock;
		private bool cached;
		private bool lockInDemo;
		private int condition;

		private bool DecideState()
		{
			if (lockInDemo && BK.Demo) {
				return false;
			}

			return condition switch
			{
				0 => GlobalSave.IsTrue("played_once"),
				1 => GlobalSave.IsTrue(ShopNpc.HatTrader),
				2 => GlobalSave.IsTrue(ShopNpc.WeaponTrader),
				3 => GlobalSave.IsTrue(ShopNpc.AccessoryTrader),
				4 => GlobalSave.IsTrue(ShopNpc.ActiveTrader),
				5 => GlobalSave.IsTrue(ShopNpc.Mike),
				6 => GlobalSave.GetInt("challenges_completed") >= 10,
				7 => GlobalSave.GetInt("challenges_completed") >= 20,
				8 => GlobalSave.GetInt("challenges_completed") >= 30,
				9 => Achievements.IsGroupComplete("a"),
				10 => Achievements.IsGroupComplete("b"),
				11 => Achievements.IsGroupComplete("c"),
				12 => Achievements.IsGroupComplete("d"),
				_ => false
			};
		}
		
		public bool ShouldLock() {
			if (cached) return shouldLock;
			shouldLock = !DecideState();
			cached = true;

			return shouldLock;
		}

		public override void PostInit() {
			SkipLock = false;
			Replaced = false;
			base.PostInit();

			Subscribe<Achievement.UnlockedEvent>();
			
			if (!Vertical) {
				// CenterX = (float) (Math.Round(CenterX / 16) * 16) + 8;
			}
		}
		
		protected override Lock CreateLock() {
			//Replaced = false;
			return /*Engine.EditingLevel ? null : */new ConditionLock();
		}

		public override void RenderImDebug() {
			base.RenderImDebug();

			ImGui.Checkbox("Lock in demo", ref lockInDemo);
			ImGui.Combo("Condition", ref condition, conditions, conditions.Length);
		}

		public override void Load(FileReader stream) {
			base.Load(stream);
			condition = stream.ReadByte();
			lockInDemo = stream.ReadBoolean();
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			stream.WriteByte((byte) condition);
			stream.WriteBoolean(lockInDemo);
		}

		public override bool HandleEvent(Event e) {
			if (e is Achievement.UnlockedEvent) {
				cached = false;
				ShouldLock();
			}
			
			return base.HandleEvent(e);
		}
	}
}