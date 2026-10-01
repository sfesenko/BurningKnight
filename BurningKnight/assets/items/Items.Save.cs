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
		public static void Load() {
			Load(FileHandle.FromRoot("items.json"));
		}
		public static void Save() {
			var root = new JsonObject();

			foreach (var item in Datas.Values) {
				var data = new JsonObject();

				data["id"] = item.Id;

				if (item.Animation != null) {
					data["animation"] = item.Animation;
				}

				if (Math.Abs(item.UseTime) > 0.01f) {
					data["time"] = item.UseTime;
				}

				if (item.Type != ItemType.Artifact) {
					data["type"] = (int) item.Type;
				}

				if (Math.Abs(item.Chance.Any - 1f) > 0.01f) {
					data["chance"] = item.Chance.ToJson();
				}

				if (item.Single) {
					data["single"] = item.Single;
				}

				if (item.Scourged) {
					data["scourged"] = true;
				}

				if (item.Quality != ItemQuality.Wooden) {
					data["quality"] = (int) item.Quality;
				}

				if (item.AutoPickup) {
					data["auto_pickup"] = item.AutoPickup;
				}

				if (item.Automatic) {
					data["auto"] = item.Automatic;
				}

				if (item.SingleUse) {
					data["single_use"] = item.SingleUse;
				}

				data["pool"] = item.Pools;
				data["uses"] = item.Uses;

				if (item.Renderer.IsJsonObject()) {
					data["renderer"] = item.Renderer;
				}

				if (item.Lockable) {
					data["lock"] = item.Lockable;
					data["uprice"] = item.UnlockPrice;
				}

				if (item.Type == ItemType.Weapon) {
					data["weapon"] = (int) item.WeaponType;
				}
				
				root[item.Id] = data;
			}
			
			var file = Assets.WriteContent("items.json");

			if (file == null) {
				return;
			}

			root.Write(file);
			file.Close();

			Locale.Save();
		}
	}
}
