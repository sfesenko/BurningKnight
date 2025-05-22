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
using Lens.lightJson;
using Lens.lightJson.Serialization;
using Lens.util;
using Lens.util.file;
using Microsoft.Xna.Framework.Input;

namespace BurningKnight.assets.achievements {
	public delegate void AchievementUnlockedCallback(string id);
	public delegate void AchievementLockedCallback(string id);
	public delegate void AchievementProgressSetCallback(string id, int progress, int max);
	
	public static class Achievements {
		public static readonly Dictionary<string, Achievement> Defined = new();
		public static readonly List<string> AchievementBuffer = [];
		public static readonly List<string> ItemBuffer = [];

		private static readonly System.Numerics.Vector2 size = new(300, 400);

		public static AchievementUnlockedCallback UnlockedCallback;
		public static AchievementLockedCallback LockedCallback;
		public static AchievementProgressSetCallback ProgressSetCallback;
		public static Action PostLoadCallback;

		private static unsafe ImGuiTextFilterPtr _filter2 = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

		public static Achievement Get(string id)
		{
			if (!Defined.TryGetValue(id, out var a))
			{
				Log.Error($"Achievements.Get: wrong id: {id}");
			}

			return a;
		}
		
		public static void Load() {
			Load(FileHandle.FromRoot("achievements.json"));
			LoadState();
		}

		private static void Load(FileHandle handle) {
			if (!handle.Exists()) {
				Log.Error($"Achievement data {handle.FullPath} does not exist!");
				return;
			}
			
			var root = JsonValue.Parse(handle.ReadAll());

			foreach (var item in root.AsJsonObject) {
				var a = new Achievement(item.Key);
				a.Load(item.Value);
				Defined[item.Key] = a;
			}
		}

		private static void Save() {
			var root = new JsonObject();

			foreach (var a in Defined.Values) {
				var data = new JsonObject();
				a.Save(data);
				root[a.Id] = data;
			}

			using var file = File.CreateText(FileHandle.FromRoot("achievements.json").FullPath);
			var writer = new JsonWriter(file);
			writer.Write(root);
			file.Close();

			Locale.Save();
		}

		public static void LockAll()
		{
			foreach (var a in Defined.Values)
			{
				a.Unlocked = false;
			}
		}

		public static void LoadState() {
			foreach (var a in Defined.Values) {
				a.Unlocked = GlobalSave.IsTrue($"ach_{a.Id}");
				a.CompletionDate = GlobalSave.GetString($"ach_{a.Id}_date", "???");
			}
		}

		public static void SetProgress(string id, int progress, int max = -1) {
			if (Assets.DataModified) {
				return;
			}
			
			if (progress == 0) {
				return;
			}
			
			var a = Get(id);

			if (a == null) {
				Log.Error($"Unknown achievement {id}!");
				return;
			}
			
			if (a.Unlocked) {
				return;
			}

			if (max == -1) {
				Log.Info($"Reading max as {a.Max} for {id}");
				max = a.Max;
			}

			if (max == 0) {
				Log.Error($"Max for {id} is 0");
				return;
			}

			var idt = $"ach_{a.Id}";

			if (progress < max) {
				GlobalSave.Put(idt, progress);
			}

			try {
				ProgressSetCallback?.Invoke(id, progress, max);
			} catch (Exception e) {
				Log.Error(e);
			}
			
			if (progress >= max) {
				Log.Info($"Progress {progress} is >= than {max} for {id}");
				ReallyUnlock(id, a);
			}
		}

		public static void Unlock(string id) {
			if (Assets.DataModified) {
				return;
			}
			
			var a = Get(id);

			if (a == null) {
				Log.Error($"Unknown achievement {id}!");
				return;
			}

			if (a.Unlocked || GlobalSave.IsTrue($"ach_{a.Id}")) {
				a.Unlocked = true;
				return;
			}

			ReallyUnlock(id, a);
		}

		private static void ReallyUnlock(string id, Achievement a) {
			a.Unlocked = true;
			a.CompletionDate = DateTime.Now.ToString("dd/MM/yyy");
			
			GlobalSave.Put($"ach_{a.Id}", true);
			GlobalSave.Put($"ach_{a.Id}_date", a.CompletionDate);
			
			Log.Info($"Achievement {id} was complete on {a.CompletionDate}!");

			var e = new Achievement.UnlockedEvent {
				Achievement = a
			};

			Engine.Instance?.State?.Area?.EventListener?.Handle(e);
			Engine.Instance?.State?.Ui?.EventListener?.Handle(e);

			if (!string.IsNullOrEmpty(a.Unlock)) {
				Items.Unlock(a.Unlock);
			}

			try {
				UnlockedCallback?.Invoke(id);
			} catch (Exception ex) {
				Log.Error(ex);
			}

			if (!AchievementBuffer.Contains(id)) {
				AchievementBuffer.Add(id);
			}

			var area = Engine.Instance?.State?.Area;
			
			if (area != null) {
				var player = LocalPlayer.Locate(area);

				if (player != null) {
					AnimationUtil.Confetti(player.Center);
				}
			}
		}

		private static void Lock(string id) {
			var a = Get(id);

			if (a == null) {
				Log.Error($"Unknown achievement {id}!");
				return;
			}

			if (!a.Unlocked) {
				return;
			}

			a.Unlocked = false;
			GlobalSave.Put($"ach_{a.Id}", false);
			
			Log.Info($"Achievement {id} was locked!");

			var e = new Achievement.LockedEvent {
				Achievement = a
			};
			
			Engine.Instance.State.Area.EventListener.Handle(e);
			Engine.Instance.State.Ui.EventListener.Handle(e);
			
			try {
				LockedCallback?.Invoke(id);
			} catch (Exception ex) {
				Log.Error(ex);
			}
		}
		
		private static string _achievementName = "";
		private static Achievement _selected;
		private static bool _hideLocked;
		private static bool _hideUnlocked;
		private static bool _forceFocus;

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
			
			ImGui.Text(_selected.Id);
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

		private static int count;
		
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

			_filter2.Draw("Search");
			
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
				
				if (_filter2.PassFilter(i.Id)) {
					if ((_hideLocked && !i.Unlocked) || (_hideUnlocked && i.Unlocked)) {
						continue;
					}
					
					count++;

					if (ImGui.Selectable(i.Id, i == _selected)) {
						_selected = i;

						if (ImGui.IsMouseDown(ImGuiMouseButton.Right)) {
							if (ImGui.Button("Give")) {
								LocalPlayer.Locate(Engine.Instance.State.Area)
									?.GetComponent<InventoryComponent>()
									.Pickup(Items.CreateAndAdd(
										_selected.Id, Engine.Instance.State.Area
									), true);
							}
						}
					}
				}

				ImGui.PopID();
			}

			ImGui.EndChild();
			ImGui.End();
		}

		public static bool IsComplete(string id) {
			var ach = Get(id);
			return ach is { Unlocked: true };
		}

		public static bool IsGroupComplete(string group) {
			var found = false;

			foreach (var a in Defined.Values) {
				if (a.Group == group) {
					found = true;

					if (!a.Unlocked) {
						return false;
					}
				}
			}
			return found;
		} 
	}
}
