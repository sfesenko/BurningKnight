using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurningKnight.assets.achievements;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.item;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.use;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;
using Lens.lightJson.Serialization;
using Lens.util;
using Lens.util.file;
using Lens.util.math;

namespace BurningKnight.assets.items {
	public static partial class Items {
		public const string PlaceholderItem = "bk:my_heart";
		
		public static readonly Dictionary<string, ItemData> Datas = new();
		private static Dictionary<ItemType, List<ItemData>> byType = new();
		private static Dictionary<int, List<ItemData>> byPool = new();
		
		public static void Load(FileHandle handle) {
			if (!handle.Exists()) {
				Log.Error($"Item data {handle.FullPath} does not exist!");
				return;
			}

			if (handle.IsDirectory()) {
				foreach (var file in handle.ListFileHandles()) {
					Load(file);
				}

				foreach (var file in handle.ListDirectoryHandles()) {
					Load(file);
				}

				return;
			}
			
			if (handle.Extension != ".json") {
				return;
			}

			var d = handle.ReadAll();
			var num = JsonCounter.Calculate(d);

			Log.Debug($"Item data number is {num}");

			if (num != Assets.ItemData) {
				Assets.DataModified = true;
			}
			
			var root = JsonValue.Parse(d);

			foreach (var item in root.AsJsonObject) {
				ParseItem(item.Key, item.Value);
			}
		}

		private static void OnChanged(object sender, FileSystemEventArgs args) {
			Log.Debug($"Reloading {args.FullPath}");
			Load(new FileHandle(args.FullPath));
		}

		private static int TryToApply(ItemData data, int pool, ItemPool pl) {
			if (!pl.Contains(pool)) {
				if (!byPool.TryGetValue(pl.Id, out var datas)) {
					datas = [];
					byPool[pl.Id] = datas;
				}

				datas.Add(data);
				return pl.Apply(pool);
			}

			return pool;
		}
		
		private static string[] coinIds = {
			"bk:copper_coin",
			"bk:iron_coin",
			"bk:gold_coin",
			"bk:platinum_coin"
		};

		private static float[] coinChances = {
			1f,
			1f / 10f,
			1f / 50f,
			1f / 100f
		};

		private static void CheckRendererForNull(Item item, string name) {
			if (item.Renderer == null) {
				Log.Error($"Unknown renderer {name} in item {item.Id}, did you register it?");
			}
		}

		private static ItemUse ParseItemUse(string id, JsonValue? data) {
			var use = UseRegistry.Create(id);

			if (use == null) {
				Log.Error($"Invalid item use id ({id}), did you register it?");
				return null;
			}

			if (data.HasValue) {
				use.Setup(data.Value);
			}

			return use;
		}

		public static void Destroy() {
			
		}

		public static List<ItemData> GetPool(ItemPool pool) {
			return byPool.TryGetValue(pool.Id, out var b) ? b : new List<ItemData>();
		}

		private static string[] veganProofItems = {
			"bk:chicken", "bk:shawarma", "bk:hotdog"
		};
		
		public static bool ShouldAppear(ItemData t) {
			if (LevelSave.MeleeOnly && t.Type == ItemType.Weapon && t.WeaponType != WeaponType.Melee) {
				return false;
			}
			
			return (Context.Run.Type == RunType.Daily || !t.Lockable || GlobalSave.IsTrue(t.Id)) && (!t.Single || Context.Run.Statistics == null ||
			                                                    (!Context.Run.Statistics.Items.Contains(t.Id) &&
			                                                     !Context.Run.Statistics.Banned.Contains(t.Id))) && t.Id != "bk:the_sword";
		}

		public static readonly List<string> GeneratedOnFloor = [];

		public static List<ItemData> GeneratePool(List<ItemData> types, Func<ItemData, bool>? filter = null, PlayerClass c = PlayerClass.Any)
		{
			return types.Where(t => ShouldAppear(t) && (filter == null || filter(t)) && !GeneratedOnFloor.Contains(t.Id))
				.ToList();
		}

		public static string Generate(ItemType type, Func<ItemData, bool>? filter = null, PlayerClass c = PlayerClass.Any) {
			if (!byType.TryGetValue(type, out var types)) {
				return null;
			}

			return Generate(types, filter, c);
		}

		public static string Generate(ItemPool pool, Func<ItemData, bool>? filter = null, PlayerClass c = PlayerClass.Any) {
			if (!byPool.TryGetValue(pool.Id, out var types)) {
				return null;
			}

			return Generate(types, filter, c);
		}

		public static string Generate(Func<ItemData, bool>? filter = null, PlayerClass c = PlayerClass.Any) {
			return Generate(Datas.Values.ToList(), filter, c);
		}

		public static bool Has(string id) {
			return Datas.ContainsKey(id);
		}

		public static void CheckForCollector() {
			if (Achievements.IsComplete("bk:collector")) {
				return;
			}
			
			foreach (var item in Datas.Values.Where(item => item.Lockable && !item.Unlocked))
			{
				Log.Info($"Collector achievement was not unlocked cuz {item.Id}");
				return;
			}
			
			Achievements.Unlock("bk:collector");
		}
	}
}