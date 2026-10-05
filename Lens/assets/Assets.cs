using System;
using System.Collections.Generic;
using System.IO;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Microsoft.Xna.Framework.Content;

namespace Lens.assets {
	public static class Assets {
		public static bool ImGuiEnabled;
		private static bool modified;

		public static bool DataModified {
			get => modified;

			set {
				if (!modified && value)
				{
					modified = true;

					Engine.Instance.Window.Title = $"MODIFIED {Engine.Instance.Window.Title}";
				}
			}
		}

		public const int ItemData = 6617551;
	
#if DEBUG
		public static bool LoadOriginalFiles = true;
		public static bool LoadMusic = true;
		public static bool LoadSfx = true;
		public const bool Reload = true;
		public static bool LoadMods = false;
#else
		public static bool LoadOriginalFiles = false;
		public static bool LoadMusic = true;
		public static bool LoadSfx = true;
		public static bool Reload = false;
		public static bool LoadMods = true;
#endif

		public static bool FailedToLoadAudio;

		public static ContentManager Content = null!; // set by the host
		public static string Root { get; private set; } =
			Path.Combine(AppContext.BaseDirectory, "Content") + Path.DirectorySeparatorChar;

		// Reads go through the source, so the same loaders serve plain files and a packaged
		// archive. FullPath on a handle still points into the content root, for writes.
		// Volatile: the host installs it before the game thread starts, and loader workers read it.
		private static volatile IContentSource source = new FileContentSource(Root);
		public static IContentSource Source => source;

		// The host supplies the root; the engine must not guess it from the working directory.
		public static void SetRoot(string root) {
			Root = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
		}

		public static void SetSource(IContentSource value) {
			source = value;
		}

		/// <summary>
		/// Opens a file in the content tree for writing, or returns null outside Debug. The content
		/// tree ships read-only — a Release install may not even be writable — and only the dev
		/// editors write into it; their callers log and give up when this returns null.
		/// </summary>
		public static StreamWriter? WriteContent(string path) {
			if (!Engine.Debug) {
				Log.Warning($"Not writing {path}: the content tree is read-only outside Debug");
				return null;
			}

			return File.CreateText(FileHandle.FromRoot(path).FullPath);
		}
		
		private static string[] folders = [];
		private static List<FileSystemEventArgs> changed = [];
		private static float lastUpdate;
		
		public static void Load(ref int progress) {
			LoadAssets(ref progress);

			if (Reload && LoadOriginalFiles)
			{
				folders =
				[
					//"Textures/",
					"bin/Animations/"
					//"Sfx/"
				];

				foreach (var t in folders)
				{
					var path = Path.GetFullPath(Root + t);

					if (!Directory.Exists(path)) {
						Log.Warning($"Not watching {t}: it does not exist yet");
						continue;
					}

					var watcher = new FileSystemWatcher();
					
					watcher.Path = path;
					// No LastAccess: the game reads these files itself, and watching access times
					// makes every read schedule a reload of what it just read.
					watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
					                                                | NotifyFilters.DirectoryName;

					watcher.Changed += OnChanged;
					watcher.Created += OnChanged;

					watcher.EnableRaisingEvents = true;
				}
			}
		}

		private static void OnChanged(object source, FileSystemEventArgs e) {
			changed.Add(e);
		}

		private static void LoadAssets(ref int progress) {
			if (Locale.Map == null) {
				Locale.Load(Locale.PrefferedClientLanguage);
			}

			progress++;
			Effects.Load();
			progress++;
			Textures.Load();
			progress++;
			Animations.Load();
			progress++;
			
			if (LoadSfx) {
				Engine.Instance.Audio.Load();
			}

			progress++;
		}

		public static void Destroy() {
			Graphics.Destroy();
			DestroyAssets();
		}

		private static void DestroyAssets() {
			Effects.Destroy();
			Textures.Destroy();
			Animations.Destroy();
			Engine.Instance.Audio.Destroy();
		}

		public static void Update(float dt) {
			Animations.Reload = false;

			lastUpdate += dt;

			if (lastUpdate >= 0.3f) {
				lastUpdate = 0;
				CheckForUpdates();
			}
		}

		private static void CheckForUpdates() {
			if (changed.Count == 0) {
				return;
			}
			
			var reloadedTextures = false;
			var reloadedSfx = false;
			var reloadedAnimations = false;

			var changedClone = changed.ToArray();
			
			foreach (var e in changedClone) {
				switch (Path.GetFileName(Path.GetDirectoryName(e.FullPath))) {
					case "Textures": {
						if (!reloadedTextures) {
							Log.Debug("Reloading textures...");
							reloadedTextures = true;
						}

						break;
					}

					case "Sfx": {
						if (!reloadedSfx) {
							Log.Debug("Reloading sfx...");

							Engine.Instance.Audio.Load();
							
							reloadedSfx = true;
						}

						break;
					}

					case "Animations": {
						if (!reloadedAnimations) {
							Log.Debug("Reloading animations...");

							Animations.Load();
							Animations.Reload = true;

							reloadedAnimations = true;
						}

						break;
					}
				}
			}
			
			changed.Clear();
		}
	}
}