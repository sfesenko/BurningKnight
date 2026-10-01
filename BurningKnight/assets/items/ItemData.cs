using BurningKnight.entity.item;
using BurningKnight.save;
using System.Text.Json.Nodes;

namespace BurningKnight.assets.items {
	public class ItemData {
		public JsonNode? Root;
		public JsonNode? Uses;
		public JsonNode? Renderer;

		public bool AutoPickup;
		public bool Automatic;
		public bool SingleUse;
		public string Animation = null!;
		public string Id = null!;
		public float UseTime;
		public ItemType Type;
		public ItemQuality Quality;
		public Chance Chance = null!;
		public int Pools;
		public bool Single = true;
		public bool Lockable;
		public bool Scourged;
		public int UnlockPrice = 1;

		public WeaponType WeaponType;

		public bool Unlocked => !Lockable || GlobalSave.IsTrue(Id);
	}
}