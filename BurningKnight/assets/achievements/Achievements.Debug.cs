using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.save;
using BurningKnight.ui.imgui;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.input;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.file;
using Microsoft.Xna.Framework.Input;

namespace BurningKnight.assets.achievements {
	// The editor half of Achievements; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Achievements {
		private static ImGuiTextFilterPtr? searchFilter;
		private static unsafe ImGuiTextFilterPtr SearchFilter =>
			searchFilter ??= new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
		private static void RenderSelectedInfo() {
			var open = true;
			
			if (!ImGui.Begin("Achievement", ref open, ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.End();
				return;
			}

			if (!open) {
				_selected = null;
				ImGui.End();

				return;
			}
			
			ImGui.Text(_selected!.Id);
			ImGui.Separator();

			ImGui.InputText("Unlocks", ref _selected.Unlock, 128);
			ImGui.InputText("Group", ref _selected.Group, 128);

			ImGui.InputInt("Max progress", ref _selected.Max);
			ImGui.Checkbox("Secret", ref _selected.Secret);
			
			var u = _selected.Unlocked;
			
			if (ImGui.Checkbox("Unlocked", ref u)) {
				if (u) {
					Unlock(_selected.Id);
				} else {
					Lock(_selected.Id);
				}			
			}
			
			ImGui.SameLine();

			if (ImGui.Button("Delete##ach")) {
				Defined.Remove(_selected.Id);
				_selected = null;

				ImGui.End();
				return;
			}
			
			ImGui.Separator();

			var k = $"ach_{_selected.Id}";
			var name = Locale.Get(k);
			
			if (ImGui.InputText("Name##ac", ref name, 64)) {
				Locale.Map[k] = name;
			}

			var key = $"ach_{_selected.Id}_desc";
			var desc = Locale.Get(key);
				
			if (ImGui.InputText("Description##ac", ref desc, 256)) {
				Locale.Map[key] = desc;
			}

			ImGui.End();
		}
		public static void RenderDebug() {
			if (!WindowManager.Achievements) {
				return;
			}
			
			if (_selected != null) {
				RenderSelectedInfo();
			}
			
			ImGui.SetNextWindowSize(size, ImGuiCond.Once);

			if (!ImGui.Begin("Achievements")) {
				ImGui.End();
				return;
			}

			if (ImGui.Button("New")) {
				ImGui.OpenPopup("New achievement");
			}

			ImGui.SameLine();

			if (ImGui.Button("Save")) {
				Log.Info("Saving achievements");
				Save();
			}

			if (ImGui.BeginPopupModal("New achievement")) {
				ImGui.PushItemWidth(300);
				ImGui.InputText("Id", ref _achievementName, 64);
				ImGui.PopItemWidth();
				
				if (ImGui.Button("Create") || Input.Keyboard.WasPressed(Keys.Enter, true)) {
					Defined[_achievementName] = _selected = new Achievement(_achievementName);
					_achievementName = "";
					_forceFocus = true;
					
					ImGui.CloseCurrentPopup();
				}	
				
				ImGui.SameLine();

				if (ImGui.Button("Cancel") || Input.Keyboard.WasPressed(Keys.Escape, true)) {
					_achievementName = "";
					ImGui.CloseCurrentPopup();
				}
				
				ImGui.EndPopup();
			}
			
			ImGui.Separator();

			if (ImGui.Button("Unlock all")) {
				foreach (var a in Defined.Keys) {
					Unlock(a);
				}
			}
			
			ImGui.SameLine();

			if (ImGui.Button("Lock all")) {
				foreach (var a in Defined.Keys) {
					Lock(a);
				}
			}
			
			ImGui.Separator();

			SearchFilter.Draw("Search");
			
			ImGui.SameLine();
			ImGui.Text($"{count}");
			count = 0;

			ImGui.Checkbox("Hide unlocked", ref _hideUnlocked);
			ImGui.SameLine();
			ImGui.Checkbox("Hide locked", ref _hideLocked);
			ImGui.Separator();
			
			var height = ImGui.GetStyle().ItemSpacing.Y;
			ImGui.BeginChild("ScrollingRegionItems", new System.Numerics.Vector2(0, -height), 
				false, ImGuiWindowFlags.HorizontalScrollbar);

			foreach (var i in Defined.Values) {
				ImGui.PushID(i.Id);

				if (_forceFocus && i == _selected) {
					ImGui.SetScrollHereY();
					_forceFocus = false;
				}
				
				if (SearchFilter.PassFilter(i.Id)) {
					if ((_hideLocked && !i.Unlocked) || (_hideUnlocked && i.Unlocked)) {
						continue;
					}
					
					count++;

					if (ImGui.Selectable(i.Id, i == _selected)) {
						_selected = i;

						if (ImGui.IsMouseDown(ImGuiMouseButton.Right)) {
							if (ImGui.Button("Give")) {
								LocalPlayer.Locate(Context.Area!)
									?.GetComponent<InventoryComponent>()!
									.Pickup(Items.CreateAndAdd(
										_selected.Id, Context.Area!
									)!, true);
							}
						}
					}
				}

				ImGui.PopID();
			}

			ImGui.EndChild();
			ImGui.End();
		}
	}
}
