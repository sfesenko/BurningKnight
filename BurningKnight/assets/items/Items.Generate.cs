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
	public partial class Items {
		public static Item Create(string id) {
			if (id == null) {
				return null;
			}
			
			if (id == "bk:coin") {
				id = coinIds[Rnd.Chances(coinChances)];
			}
			
			if (!Datas.TryGetValue(id, out var data)) {
				Log.Error($"Unknown item {id}");
				return null;
			}

			return Create(data);
		}
		public static Item Create(ItemData data) {
			var item = new Item {
				UseTime = data.UseTime,
				Id = data.Id,
				Type = data.Type,
				AutoPickup = data.AutoPickup,
				Animation = data.Animation,
				Automatic = data.Automatic,
				SingleUse = data.SingleUse,
				Scourged = data.Scourged,
				Uses = ParseUses(data.Uses)
			};
			
			if (data.Renderer != JsonValue.Null) {
				if (data.Renderer.IsString) {
					var name = data.Renderer.AsString;
					item.Renderer = RendererRegistry.Create(name);

					CheckRendererForNull(item, name);
				} else if (data.Renderer.IsJsonObject) {
					var name = data.Renderer["id"].String("bk:Angled");
					item.Renderer = RendererRegistry.Create(name);

					CheckRendererForNull(item, name);
					
					if (item.Renderer != null) {
						item.Renderer.Item = item;
						item.Renderer.Setup(data.Renderer);
					}
				} else {
					Log.Error($"Invalid renderer declaration in item {data.Id}");
				}
			}

			foreach (var u in item.Uses) {
				u.Item = item;
				u.Init();
			}

			return item;
		}
		public static bool ShouldAppear(string id) {
			if (id == "bk:coin") {
				return true;
			}

			if (id == "bk:blindfold") {
				return false;
			}
			
			if (!Datas.TryGetValue(id, out var data)) {
				return false;
			}

			if (Settings.Vegan && veganProofItems.Contains(id)) {
				return false;
			}

			return ShouldAppear(data);
		}
		public static string GenerateAndRemove(List<ItemData> datas, Func<ItemData, bool> filter = null, bool removeFromFloor = false) {
			double sum = 0;
			
			foreach (var chance in datas) {
				if (filter == null || filter(chance)) {
					sum += chance.Chance.Calculate(PlayerClass.Any);
				}
			}

			var value = Rnd.Double(sum);
			sum = 0;

			string id = null;
			ItemData data = null;
			
			foreach (var t in datas) {
				if (filter == null || filter(t)) {
					sum += t.Chance.Calculate(PlayerClass.Any);
					
					if (value < sum) {
						id = t.Id;
						data = t;
						break;
					}

				}
			}

			if (id != null) {
				if (removeFromFloor) {
					GeneratedOnFloor.Add(id);
				}
				
				datas.Remove(data);
				return id;
			}

			return PlaceholderItem;
		}
		private static string Generate(List<ItemData> types, Func<ItemData, bool> filter, PlayerClass c) {
			double sum = 0;
			var datas = GeneratePool(types, filter, c);
			
			foreach (var chance in datas) {
				sum += chance.Chance.Calculate(c);
			}

			var value = Rnd.Double(sum);
			sum = 0;

			foreach (var t in datas) {
				sum += t.Chance.Calculate(c);

				if (value < sum) {
					return t.Id;
				}
			}

			return PlaceholderItem;
		}
		public static Item CreateAndAdd(string id, Area area, bool scourgeFree = true) {
			var item = Create(id);

			if (item == null) {
				return null;
			}
			
			area.Add(item);
			item.AddDroppedComponents();

			if (scourgeFree && (!Datas.ContainsKey(id) || !Datas[id].Scourged)) {
				item.Scourged = false;
			}
			
			return item;
		}
		public static void Unlock(string id) {
			if (!Datas.TryGetValue(id, out var data)) {
				Log.Error($"Unknown item {id}");
				return;
			}

			if (!data.Lockable) {
				return;
			}

			if (data.Unlocked) {
				return;
			}
			
			GlobalSave.Put(data.Id, true);

			var e = new Item.UnlockedEvent {
				Data = data
			};

			try {
				Engine.Instance.State.Ui.EventListener.Handle(e);
				Context.Area.EventListener.Handle(e);

				if (!Achievements.ItemBuffer.Contains(id)) {
					Achievements.ItemBuffer.Add(id);
				}
			} catch (Exception er) {
				Log.Error(er);
			}

			CheckForCollector();
		}
	}
}
