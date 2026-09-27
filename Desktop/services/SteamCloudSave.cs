using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurningKnight.save;
using Lens.services;
using Lens.util;
using Lens.util.file;
using Steamworks;

namespace Desktop.services {
	public class SteamCloudSave : ICloudSave {
		public static readonly SteamCloudSave Instance = new();

		public bool Enabled;

		public void Delete() {
			if (!Enabled || !SteamRemoteStorage.IsCloudEnabled) {
				return;
			}

			foreach (var f in SteamRemoteStorage.Files) {
				SteamRemoteStorage.FileDelete(f);
			}
		}

		public void Load() {
			if (!Enabled || !SteamRemoteStorage.IsCloudEnabled) {
				return;
			}

			Log.Info("Loading data from cloud");

			if (!SteamClient.IsLoggedOn) {
				Log.Error("Can't connect to steam servers");

				return;
			}

			if (SteamRemoteStorage.FileCount > 0) {
				RemoveFile(new FileHandle(SaveManager.SaveDir), "");
			}

			foreach (var file in SteamRemoteStorage.Files) {
				var to = $"{SaveManager.SaveDir}{file}";
				Log.Info($"Loading file {file} to {to}");

				var handle = new FileHandle(to);

				if (!handle.Parent.Exists()) {
					Log.Info($"Making the directory {handle.Parent.FullPath}");
					handle.Parent.MakeDirectory();
				}

				File.WriteAllBytes(to, SteamRemoteStorage.FileRead(file));
			}
		}

		private static void RemoveFile(FileHandle handle, string path) {
			if (handle.IsDirectory()) {
				path += $"{handle.Name}/";

				foreach (var dir in handle.ListDirectoryHandles()) {
					RemoveFile(dir, path);
				}

				foreach (var file in handle.ListFileHandles()) {
					RemoveFile(file, path);
				}
			} else {
				path = $"{path}{handle.Name}";

				if (!SteamRemoteStorage.Files.Contains(path)) {
					Log.Info($"Removing file {path} from local saves");
				}
			}
		}

		public void Save() {
			if (!Enabled || !SteamRemoteStorage.IsCloudEnabled) {
				return;
			}

			Log.Info("Saving data to cloud");

			if (!SteamClient.IsLoggedOn) {
				Log.Error("Can't connect to steam servers");

				return;
			}

			var toRemove = new List<string>();

			foreach (var file in SteamRemoteStorage.Files) {
				var handle = new FileHandle($"{SaveManager.SaveDir}{file}");

				if (!handle.Exists()) {
					toRemove.Add(file);
				}
			}

			foreach (var file in toRemove) {
				Log.Info($"Removing cloud file {file}");
				SteamRemoteStorage.FileDelete(file);
			}

			WriteFile(new FileHandle(SaveManager.SaveDir), "");
		}

		private static void WriteFile(FileHandle handle, string path) {
			if (handle.IsDirectory()) {
				path += $"{handle.Name}/";

				foreach (var dir in handle.ListDirectoryHandles()) {
					WriteFile(dir, path);
				}

				foreach (var file in handle.ListFileHandles()) {
					WriteFile(file, path);
				}
			} else {
				if (handle.Extension != ".sv" && handle.Extension != ".lvl") {
					Log.Info($"Ignoring file {handle.FullPath} cause of its extension {handle.Extension}");

					return;
				}

				path = $"{path}{handle.Name}";
				Log.Info($"Saving file {path} from {handle.FullPath}");

				SteamRemoteStorage.FileWrite(path, File.ReadAllBytes(handle.FullPath));
			}
		}
	}
}
