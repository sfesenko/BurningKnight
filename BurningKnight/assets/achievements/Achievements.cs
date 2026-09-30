using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.save;
using BurningKnight.util;
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
	
	public static partial class Achievements {
		public static readonly Dictionary<string, Achievement> Defined = new();
		public static readonly List<string> AchievementBuffer = [];
		public static readonly List<string> ItemBuffer = [];

		private static readonly System.Numerics.Vector2 size = new(300, 400);

		public static AchievementUnlockedCallback? UnlockedCallback;
		public static AchievementLockedCallback? LockedCallback;
		public static AchievementProgressSetCallback? ProgressSetCallback;
		public static Action PostLoadCallback = null!;

		// Created on first use: a field initializer would make an ImGui native call as soon as the
		// class is touched, and a release run must not touch ImGui at all.


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

			using var file = Assets.WriteContent("achievements.json");

			if (file == null) {
				return;
			}

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
			
			Context.Area!.EventListener.Handle(e);
			Engine.Instance.State.Ui.EventListener.Handle(e);
			
			try {
				LockedCallback?.Invoke(id);
			} catch (Exception ex) {
				Log.Error(ex);
			}
		}
		


		

#if DEBUG
		// The achievement editor (Achievements.Debug.cs) is Debug-only, and so are its fields.
		private static string _achievementName = "";
		private static Achievement _selected;
		private static bool _hideLocked;
		private static bool _hideUnlocked;
		private static bool _forceFocus;
		private static int count;
#endif

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
