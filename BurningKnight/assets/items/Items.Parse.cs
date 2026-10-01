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
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.file;
using Lens.util.math;

namespace BurningKnight.assets.items {
	public partial class Items {
		private static void ParseItem(string id, JsonNode item) {
			var a = item["animation"];
			var animation = a == null ? null : a.AsString();

			var type = (ItemType) item["type"].Int(0);
			var p = item["auto_pickup"];
			var pickup = p == null ? (type == ItemType.Key || type == ItemType.Bomb || type == ItemType.Heart || type == ItemType.Coin) : p.Bool(false);
			
			var data = new ItemData {
				Id = id,
				UseTime = item["time"].Number(0),
				Type = type,
				Quality = (ItemQuality) item["quality"].AsInteger(),
				Root = item,
				Uses = item["uses"],
				Renderer = (item["renderer"].IsJsonObject() ? item["renderer"] : null),
				Animation = animation!,
				AutoPickup = pickup,
				Single = item["single"].Bool(true),
				Automatic = item["auto"].Bool(false),
				SingleUse = item["single_use"].Bool(false),
				Scourged = item["scourged"].Bool(false),
				Chance = Chance.Parse(item["chance"]),
				Lockable = item["lock"].Bool(false)
			};

			if (data.Type == ItemType.Weapon) {
				data.WeaponType = (WeaponType) item["weapon"].Int(0);
			}

			if (data.Lockable) {
				data.UnlockPrice = item["uprice"].Int();
			}
			
			var pl = item["pool"];
			var pools = 0;

			if (pl == null)
			{
				pools = data.Type switch
				{
					ItemType.Key or ItemType.Coin or ItemType.Bomb or ItemType.Heart => 
						TryToApply(data, pools, ItemPool.Consumable),
					ItemType.Artifact or ItemType.Weapon or ItemType.Active => 
						TryToApply(data, pools, ItemPool.Treasure),
					_ => pools
				};
			} else {
				var pls = pl.Int(0);

				for (var i = 0; i < ItemPool.Count; i++) {
					if (ItemPool.ById[i].Contains(pls)) {
						pools = TryToApply(data, pools, ItemPool.ById[i]);
					}
				}
			}

			data.Pools = pools;

			Datas[id] = data;

			if (!byType.TryGetValue(data.Type, out var all)) {
				all = [];
				byType[data.Type] = all;
			}
			
			all.Add(data);
		}
		public static ItemUse[] ParseUses(JsonNode? data) {
			if (data != null) {
				var uses = new List<ItemUse>();

				if (data.IsString()) {
					var use = ParseItemUse(data.String(), null);

					if (use != null) {
						uses.Add(use);
					}
				} else if (data.IsJsonArray()) {
					foreach (var d in data.AsJsonArray()!) {
						if (d.IsJsonObject()) {
							if (d == null || !d["id"].IsString()) {
								Log.Error("Item has no id");
								continue;
							}
							
							var use = ParseItemUse(d!["id"].String(), d);

							if (use != null) {
								uses.Add(use);
							}
						} else if (d.IsString()) {
							var use = ParseItemUse(d.String(), null);

							if (use != null) {
								uses.Add(use);
							}
						}
					}
				} else if (data.IsJsonObject()) {
					var obj = data.AsJsonObject()!;
					
					if (!obj["id"].IsString()) {
						Log.Error("Item has no id");
					} else {
						var use = ParseItemUse(obj["id"].String(), obj);

						if (use != null) {
							uses.Add(use);
						}
					}
				} else {
					Log.Error("Invalid item use declaration");
				}

				return uses.ToArray();
			}

			return [];
		}
	}
}
