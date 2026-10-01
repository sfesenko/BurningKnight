using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.assets.achievements {
	public class Achievement(string id)
	{
		public readonly string Id = id;
		public bool Unlocked { get; internal set; }
		public int Max;
		public string Unlock = "";
		public bool Secret;
		public string Group = "";
		public string CompletionDate = "???";

		public void Load(JsonNode root) {
			Max = root["max"].Int(0);
			Unlock = root["unlock"].String("");
			Secret = root["secret"].Bool(false);
			Group = root["group"].String("");
		}

		public void Save(JsonNode root) {
			if (Max > 0) {
				root["max"] = Max;
			}

			if (Unlock.Length > 0) {
				root["unlock"] = Unlock;
			}

			if (Secret) {
				root["secret"] = true;
			}

			if (Group.Length > 0) {
				root["group"] = Group;
			}
		}

		public class UnlockedEvent : Event {
			public Achievement Achievement = null!;
		}

		public class LockedEvent : Event {
			public Achievement Achievement = null!;
		}
	}
}