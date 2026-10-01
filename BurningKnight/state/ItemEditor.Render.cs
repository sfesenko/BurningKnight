using BurningKnight.ui.imgui;
using ImGuiNET;
using System;
using System.Collections.Immutable;
using BurningKnight.assets;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.item;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.item.use;
using BurningKnight.save;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.graphics;
using Lens.input;
using Lens.lightJson;
using Lens.util;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace BurningKnight.state {
	public partial class ItemEditor {
		public static void Render() {
			if (!WindowManager.ItemEditor) {
				return;
			}
			
			if (toSort) {
				toSort = false;
				Sort();
			}

			RenderWindow();
			
			ImGui.SetNextWindowSize(size, ImGuiCond.Once);
			
			if (!ImGui.Begin("Item explorer")) {
				ImGui.End();
				return;
			}
			
			id = 0;
			ud = 0;

			if (ImGui.Button("New")) {
				ImGui.OpenPopup("New item");
				fromCurrent = false;
			}

			if (Selected != null) {
				ImGui.SameLine();

				if (ImGui.Button("New from current")) {
					ImGui.OpenPopup("New item");
					fromCurrent = true;
				}
			}
			
			ImGui.SameLine();

			if (ImGui.Button("Save") || (Input.Keyboard.IsDown(Keys.LeftControl, true) && Input.Keyboard.WasPressed(Keys.S))) {
				Log.Info("Saving items");
				Items.Save();
			}
			
			ImGui.SameLine();

			if (ImGui.Button("Spawn All (super laggy!)")) {
				var player = LocalPlayer.Locate(Context.Area!);

				foreach (var id in Items.Datas.Keys) {
					Items.CreateAndAdd(id, Context.Area!, false)!.Center = player!.Center;
				}
			}

			ImGui.SameLine();

			if (ImGui.Button("Unlock All")) {
				foreach (var data in Items.Datas.Values) {
					if (!data.Unlocked) {
						Items.Unlock(data.Id);
					}
				}
			}

			if (ImGui.BeginPopupModal("New item")) {
				ImGui.PushItemWidth(300);
				ImGui.InputText("Id", ref itemName, 64);
				ImGui.PopItemWidth();
				
				if (ImGui.Button("Create") || Input.Keyboard.WasPressed(Keys.Enter, true)) {
					var data = new ItemData();
					
					if (fromCurrent && Selected != null) {
						data.Type = Selected.Type;
						data.Quality = Selected.Quality;
						data.Animation = Selected.Animation;
						data.Pools = Selected.Pools;
						data.Root = JsonValue.Parse(Selected.Root.ToString());
						data.Renderer = data.Root["renderer"];
						data.Uses = data.Root["uses"];
						data.SingleUse = Selected.SingleUse;
						data.UseTime = Selected.UseTime;
						data.Lockable = Selected.Lockable;
						data.UnlockPrice = Selected.UnlockPrice;
						data.Single = Selected.Single;
						
						var c = Selected.Chance;
						data.Chance = new Chance(c.Any, c.Melee, c.Magic, c.Range);
					} else {
						data.Chance = Chance.All();
						data.Uses = new JsonArray();
						data.Renderer = new JsonObject();

						data.Root = new JsonObject {
							["uses"] = data.Uses, 
							["renderer"] = data.Renderer
						};
					}

					data.Id = itemName;
					Items.Datas[data.Id] = data;
					Selected = data;
					itemName = "";

					ImGui.CloseCurrentPopup();
				}	
				
				ImGui.SameLine();

				if (ImGui.Button("Cancel") || Input.Keyboard.WasPressed(Keys.Escape, true)) {
					itemName = "";
					ImGui.CloseCurrentPopup();
				}
				
				ImGui.EndPopup();
			}
			
			ImGui.Separator();
			
			filter.Draw("Search");

			ImGui.SameLine();
			ImGui.Text($"{count}");

			count = 0;
			ImGui.Combo("Filter by", ref sortBy, sortTypes, sortTypes.Length);

			if (sortBy > 0)
			{
				switch (sortBy)
				{
					case 1:
						ImGui.Combo("Type", ref sortType, Types, Types.Length);
						break;
					case 2:
						ImGui.Checkbox("Lockable", ref locked);
						break;
					case 3:
					{
						if (ImGui.TreeNode("Spawns in")) {
							ImGui.Checkbox("Does not spawn", ref invertSpawn);
							ImGui.Separator();
						
							var i = 0;
						
							foreach (var p in ItemPool.ById) {
								var val = p.Contains(pools);
					
								if (ImGui.Checkbox(p.Name, ref val)) {
									pools = p.Apply(pools, val);
								}
					
								i++;
					
								if (i == ItemPool.Count) {
									break;
								}
							}
						
							ImGui.TreePop();
						}

						break;
					}
					case 4:
						ImGui.Checkbox("Single?", ref single);
						break;
					case 5:
						ImGui.Combo("Quality", ref quality, Quality, Quality.Length);
						break;
					case 6:
					{
						var v = (int) weaponTypeSort;

						if (ImGui.Combo("Weapon Type", ref v, WeaponTypes, WeaponTypes.Length)) {
							weaponTypeSort = (WeaponType) v;
						}

						break;
					}
				}
			}

			ImGui.Separator();
			
			var height = ImGui.GetStyle().ItemSpacing.Y;
			ImGui.BeginChild("ScrollingRegionItems", new System.Numerics.Vector2(0, -height), 
				false, ImGuiWindowFlags.HorizontalScrollbar);

			var items = Items.Datas.ToImmutableSortedDictionary();
			foreach (var i in items.Values) {
				ImGui.PushID(id);

				if (ForceFocus && i == Selected) {
					ImGui.SetScrollHereY();
					ForceFocus = false;
				}
				
				if (filter.PassFilter(i.Id)) {
					if (sortBy > 0)
					{
						switch (sortBy)
						{
							case 1 when i.Type != (ItemType) sortType:
							case 2 when i.Lockable != locked:
								continue;
							case 3:
							{
								var found = false;
							
								for (var j = 0; j < 32; j++) {
									if (BitHelper.IsBitSet(pools, j) && BitHelper.IsBitSet(i.Pools, j)) {
										found = true;
										break;
									}
								}

								if (invertSpawn == found) {
									continue;
								}

								break;
							}
							case 4 when i.Single != single:
							case 5 when i.Quality != (ItemQuality) quality:
							case 6 when i.Type != ItemType.Weapon || i.WeaponType != weaponTypeSort:
								continue;
						}
					}
					
					count++;

					if (ImGui.Selectable(i.Id, i == Selected)) {
						Selected = i;

						if (ImGui.IsMouseDown(ImGuiMouseButton.Right)) {
							if (ImGui.Button("Give")) {
								LocalPlayer.Locate(Context.Area!)
									?.GetComponent<InventoryComponent>()!
									.Pickup(Items.CreateAndAdd(
										Selected.Id, Context.Area!
									)!);
							}
						}
					}
				}

				ImGui.PopID();
				id++;
			}

			ImGui.EndChild();
			ImGui.End();
		}
	}
}
