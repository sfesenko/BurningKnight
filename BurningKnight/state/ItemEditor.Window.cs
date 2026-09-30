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
		private static void RenderWindow() {
			if (Selected == null) {
				return;
			}
			
			var show = true;
			var player = LocalPlayer.Locate(Context.Area);

			if (!ImGui.Begin("Item editor", ref show, ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.End();
				return;
			}

			if (!show) {
				Selected = null;
				return;
			}

			var name = Locale.Get(Selected.Id);
			var animated = Selected.Animation != null;
			var region = animated ? null : CommonAse.Items.GetSlice(Selected.Id);

			if (!animated && region != null) {
				DrawItem(region);
			}

			ImGui.Text(Selected.Id);

			if (ImGui.Button("Give")) {
				LocalPlayer.Locate(Context.Area)
					?.GetComponent<InventoryComponent>()
					.Pickup(Items.CreateAndAdd(
						Selected.Id, Context.Area
					));
			}

			if (player != null) {
				ImGui.SameLine();

				if (ImGui.Button("Spawn")) {
					var item = Items.CreateAndAdd(
						Selected.Id, Context.Area, false
					);

					item.Center = player.Center;
				}
				
				ImGui.SameLine();

				if (ImGui.Button("Spawn on stand")) {
					var stand = new ItemStand();
					Context.Area.Add(stand);
					var item = Items.CreateAndAdd(
						Selected.Id, Context.Area, false
					);

					stand.Center = player.Center;
					stand.SetItem(item, null);
				}
			}

			ImGui.SameLine();
			
			if (ImGui.Button("Rename")) {
				ImGui.OpenPopup("Rename");
				itemName = Selected.Id;
			}
			
			if (ImGui.BeginPopupModal("Rename")) {
				ImGui.PushItemWidth(300);
				ImGui.InputText("Id", ref itemName, 64);
				ImGui.PopItemWidth();
				
				if (ImGui.Button("Rename") || Input.Keyboard.WasPressed(Keys.Enter, true)) {
					var iname = Locale.Get(Selected.Id);
					var idesc = $"{Selected.Id}_desc";
					var description = Locale.Get(idesc);
					
					Locale.Map.Remove(Selected.Id);
					Locale.Map.Remove(idesc);

					Items.Datas.Remove(Selected.Id);

					Selected.Id = itemName;
					itemName = "";

					Locale.Map[Selected.Id] = iname;
					Locale.Map[$"{Selected.Id}_desc"] = description;
					Items.Datas[Selected.Id] = Selected;

					ImGui.CloseCurrentPopup();
				}	
				
				ImGui.SameLine();

				if (ImGui.Button("Cancel") || Input.Keyboard.WasPressed(Keys.Escape, true)) {
					itemName = "";
					ImGui.CloseCurrentPopup();
				}
				
				ImGui.EndPopup();
			}
			
			if (ImGui.InputText("Name", ref name, 64)) {
				Locale.Map[Selected.Id] = name;
			}

			var key = $"{Selected.Id}_desc";
			var desc = Locale.Get(key);
				
			if (ImGui.InputText("Description", ref desc, 128)) {
				Locale.Map[key] = desc;
			}

			var type = (int) Selected.Type;

			if (ImGui.Combo("Type", ref type, Types, Types.Length)) {
				Selected.Type = (ItemType) type;
			}
			
			type = (int) Selected.Quality;

			if (ImGui.Combo("Quality", ref type, Quality, Quality.Length)) {
				Selected.Quality = (ItemQuality) type;
			}

			if (Selected.Type == ItemType.Weapon) {
				type = (int) Selected.WeaponType;

				if (ImGui.Combo("Weapon Type", ref type, WeaponTypes, WeaponTypes.Length)) {
					Selected.WeaponType = (WeaponType) type;
				}
			}

			var t = Selected.Type;

			if (t != ItemType.Coin && t != ItemType.Heart && t != ItemType.Bomb && t != ItemType.Key) {
				if (t == ItemType.Active) {
					var o = 0; // Room charged

					if (Selected.SingleUse) {
						o = 2;
					} else if (Selected.UseTime < -0.01f) {
						o = 1; // Auto charged
					} else if (Selected.UseTime < 0.01f) {
						o = 3; // Infinite
					}

					if (ImGui.Combo("RC", ref o, types, types.Length)) {
						Selected.SingleUse = false;
						
						if (o == 0) {
							Selected.UseTime = Math.Max(0.01f, Math.Abs(Selected.UseTime));
						} else if (o == 1) {
							Selected.UseTime = Math.Min(-0.1f, -Math.Abs(Selected.UseTime));
						} else if (o == 3) {
							Selected.UseTime = 0;
						} else {
							Selected.SingleUse = true;
						}
					}
					
					if (o == 0) {
						var v = (int) Selected.UseTime;

						if (ImGui.InputInt("Charges", ref v)) {
							Selected.UseTime = v;
						}
					} else if (o == 1) {
						var v = -Selected.UseTime;

						if (ImGui.InputFloat("Charge time", ref v)) {
							Selected.UseTime = -v;
						}
					}
				} else if (t == ItemType.Weapon) {
					ImGui.InputFloat("Use time", ref Selected.UseTime);
					ImGui.Checkbox("Automatic", ref Selected.Automatic);
				}

				ImGui.Checkbox("Auto pickup", ref Selected.AutoPickup);
				ImGui.Checkbox("Scourge", ref Selected.Scourged);
				ImGui.SameLine();
			} else {
				Selected.AutoPickup = true;
			}

			if (ImGui.Checkbox("Animated", ref animated)) {
				Selected.Animation = animated ? "" : null;
			}

			if (animated) {
				ImGui.InputText("Animation", ref Selected.Animation, 128);
			}

			ImGui.Separator();
			ImGui.Checkbox("Lockable", ref Selected.Lockable);

			if (Selected.Lockable) {
				ImGui.SameLine();
				var unlocked = GlobalSave.IsTrue(Selected.Id);

				if (ImGui.Checkbox("Unlocked", ref unlocked)) {
					if (unlocked) {
						Items.Unlock(Selected.Id);
					} else {
						GlobalSave.Put(Selected.Id, false);
					}
				}

				var sells = Selected.UnlockPrice > 0;

				if (ImGui.Checkbox("Sells", ref sells)) {
					Selected.UnlockPrice = sells ? 1 : 0;
				}

				if (sells) {
					ImGui.SameLine();
					ImGui.InputInt("Price", ref Selected.UnlockPrice);
				}
			}

			ImGui.Separator();

			if (ImGui.CollapsingHeader("Uses")) {
				DisplayUse(Selected.Root, Selected.Uses);
			}
			
			if (ImGui.CollapsingHeader("Renderer")) {
				DisplayRenderer(Selected.Root, Selected.Renderer);
			}
			
			if (ImGui.CollapsingHeader("Pools")) {
				ImGui.Checkbox("Single spawn", ref Selected.Single);
				
				var i = 0;
				ImGui.Text($"{Selected.Pools}");
				
				foreach (var p in ItemPool.ById) {
					var val = p.Contains(Selected.Pools);
					
					if (ImGui.Checkbox(p.Name, ref val)) {
						Selected.Pools = p.Apply(Selected.Pools, val);
					}
					
					i++;
					
					if (i == ItemPool.Count) {
						break;
					}
				}
			}
			
			if (ImGui.CollapsingHeader("Spawn chance")) {
				Selected.Chance.RenderDebug();
			}

			ImGui.Separator();
			
			if (ImGui.Button("Delete")) {
				Items.Datas.Remove(Selected.Id);
				Selected = null;
			}

			if (Selected != null && player != null) {
				var id = Selected.Id;
				
				if (player.GetComponent<InventoryComponent>()!.Has(id)) {
					ImGui.BulletText("Present in inventory");
				}
				
				if (player.GetComponent<ActiveWeaponComponent>()!.Has(id)) {
					ImGui.BulletText("Present in active weapon slot");
				}
				
				if (player.GetComponent<WeaponComponent>()!.Has(id)) {
					ImGui.BulletText("Present in weapon slot");
				}
				
				if (player.GetComponent<ActiveItemComponent>()!.Has(id)) {
					ImGui.BulletText("Present in active item slot");
				}
			}
			
			ImGui.End();
		}
	}
}
