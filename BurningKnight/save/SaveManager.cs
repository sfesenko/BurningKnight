using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurningKnight.state;
using Lens;
using Lens.entity;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.save {
	public class SaveManager {
		// A subdirectory of the host-supplied data directory: the platform decides where
		// writable state lives, and the saves live inside it.
		public static string SaveDir => Path.Combine(Paths.DataDir, "saves") + Path.DirectorySeparatorChar;
		public const int MagicNumber = 894923782;
		public const short Version = 3;

		public static readonly byte CurrentSlot = 0;
		public static string SlotDir = $"{SaveDir}slot-{CurrentSlot}/";

		public static Saver[] Savers = null!;

		public static void Init() {
			Migrate();

			Log.Info($"Save directory is '{new FileHandle(SaveDir).FullPath}'");

			Savers = new Saver[6];
			Savers[(int) SaveType.Global] = new GlobalSave();
			Savers[(int) SaveType.Game] = new GameSave();
			Savers[(int) SaveType.Level] = new LevelSave();
			Savers[(int) SaveType.Player] = new PlayerSave();
			Savers[(int) SaveType.Secret] = new SecretSave();
			Savers[(int) SaveType.Statistics] = new StatisticsSaver();

			var saveDirectory = new FileHandle(SaveDir);

			if (!saveDirectory.Exists()) {
				saveDirectory.MakeDirectory();
				Log.Info("Creating the save directory");

				SecretSave.HadSaveBefore = false;
			}
		}

		public static Saver ForType(SaveType type) {
			return Savers[(int) type];
		}

		// Saves used to live beside the executable. Move them once, rather than lose a run that
		// started before the move.
		private static void Migrate() {
			if (Paths.LegacyDataDir == null || Directory.Exists(SaveDir) || !Directory.Exists(Paths.LegacyDataDir)) {
				return;
			}

			try {
				Directory.CreateDirectory(Paths.DataDir);
				Directory.Move(Paths.LegacyDataDir, SaveDir);

				Log.Info($"Moved the saves from {Paths.LegacyDataDir} to {SaveDir}");
			} catch (Exception e) {
				Log.Error($"Could not move the saves from {Paths.LegacyDataDir}");
				Log.Error(e);
			}
		}

		public static string GetSavePath(SaveType saveType, bool old = false, string? path = null) {
			return ForType(saveType).GetPath((path ?? (saveType == SaveType.Statistics || saveType == SaveType.Global ||
			                                           (saveType == SaveType.Level && (old ? Context.Run.LastDepth : Context.Run.Depth) < 1)
				? SaveDir
				: SlotDir)), old);
		}

		public static FileHandle GetFileHandle(string path) {
			return new FileHandle(path);
		}

		private static FileWriter GetWriter(string path) {
			return new FileWriter(path);
		}

		private static FileReader GetReader(string path) {
			return new FileReader(path);
		}

		public static void Save(Area area, SaveType saveType, bool old = false, string? path = null) {
			var p = GetSavePath(saveType, old, path);
			var file = new FileInfo(p);

			if (saveType != SaveType.Secret || Engine.Version.Dev) {
				Log.Info($"Saving {saveType} {(old ? Context.Run.LastDepth : Context.Run.Depth)} to {file.FullName}");
			}

			file.Directory?.Create();

			var stream = GetWriter(p);

			stream.WriteInt32(MagicNumber);
			stream.WriteInt16(Version);
			stream.WriteByte((byte) saveType);

			ForType(saveType).Save(area, stream, old);
			stream.Close();

			if (saveType != SaveType.Secret) {
				SecretSave.HadSaveBefore = true;
			}
		}

		public static bool ExistsAndValid(SaveType saveType, Action<FileReader>? action = null, string? path = null) {
			var save = GetFileHandle(GetSavePath(saveType, false, path));

			if (!save.Exists()) {
				return false;
			}

			var stream = GetReader(save.FullPath);

			if (stream.ReadInt32() != MagicNumber) {
				return false;
			}

			if (stream.ReadInt16() > Version) {
				return false;
			}

			if (stream.ReadByte() != (byte) saveType) {
				return false;
			}

			action?.Invoke(stream);

			return true;
		}

		public static void Load(Area area, SaveType saveType, string? path = null) {
			var save = GetFileHandle(GetSavePath(saveType, false, path));

			if (!save.Exists()) {
				Generate(area, saveType);
			} else {
				if (saveType != SaveType.Secret || Engine.Version.Dev) {
					Log.Info($"Loading {saveType} {Context.Run.Depth} l{Context.Run.Loop}{(path == null ? $" from {save.FullPath}" : $" from {path}")}");
				}

				var stream = GetReader(save.FullPath);

				if (stream.ReadInt32() != MagicNumber) {
					Log.Error("Invalid magic number!");
					Generate(area, saveType);

					return;
				}

				var version = stream.ReadInt16();
				stream.SaveVersion = version;

				if (version > Version) {
					if (saveType != SaveType.Global && saveType != SaveType.Game) {
						Log.Error($"Unknown version {version}, generating new");
						Generate(area, saveType);

						return;
					}
				} else if (version < 2) {
					// Version 2 is still read — only the entity size field changed width in 3.
					if (saveType != SaveType.Global && saveType != SaveType.Game && !(path ?? save.FullPath).StartsWith("Content")) {
						Log.Error($"Old version {version}, generating new");
						Generate(area, saveType);

						return;
					}
				}

				if (stream.ReadByte() != (byte) saveType) {
					Log.Error("Save file did not match it's loader type!");
					Generate(area, saveType);

					return;
				}

				ForType(saveType).Load(area, stream);
			}
		}

		public static void Generate(Area area, SaveType saveType) {
			if (saveType != SaveType.Secret || Engine.Version.Dev) {
				Log.Info($"Generating {saveType} {Context.Run.Depth}");
			}

			ForType(saveType).Generate(area);

			if (Context.Run.Depth > 0) {
				Save(area, saveType);
			}
		}

		public static void Delete(params SaveType[] types) {
			foreach (var type in types) {
				if (type != SaveType.Secret || Engine.Version.Dev) {
					Log.Info($"Deleting {type} save");
				}

				try {
					ForType(type).Delete();
				} catch (Exception e) {
					Log.Error(e);
				}
			}
		}
	}
}
