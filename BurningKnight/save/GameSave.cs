using System;
using System.Collections.Generic;
using BurningKnight.state;
using Lens.entity;
using Lens.util;
using Lens.util.file;
using Lens.util.math;

namespace BurningKnight.save {
	public class GameSave : Saver {
		public static Dictionary<string, string> Values = new Dictionary<string, string>();

		public static bool IsTrue(string Key, bool Def = false) {
			if (Values.TryGetValue(Key, out var Value)) {
				return Value == "1";
			}
			
			return Def;
		}

		public static bool IsFalse(string Key) {
			return !IsTrue(Key);
		}

		public static void Put(string Key, object Val) {
			Values[Key] = Val?.ToString();
		}

		public static void Put(string Key, int Val) {
			Values[Key] = Val.ToString();
		}

		public static void Put(string Key, float Val) {
			Values[Key] = Val.ToString();
		}

		public static void Put(string Key, bool Val) {
			Values[Key] = Val ? "1" : "0";
		}

		public static string GetString(string Key, string? Def = null) {
			return Values.TryGetValue(Key, out var Value) ? Value : Def;
		}
		
		public static int GetInt(string Key, int Def = 0) {
			try {
				return Values.TryGetValue(Key, out var Value) ? Int32.Parse(Value) : Def;
			} catch (Exception) {
				return Def;
			}
		}

		public static float GetFloat(string Key, float Def = 0) {
			try {			
				return Values.TryGetValue(Key, out var Value) ? Single.Parse(Value) : Def;
			} catch (Exception) {
				return Def;
			}
		}

		public override void Save(Area area, FileWriter writer, bool old) {
			writer.WriteInt32(Values.Count);

			foreach (var Pair in Values) {
				writer.WriteString(Pair.Key);
				writer.WriteString(Pair.Value);
			}
		
			writer.WriteSbyte((sbyte) Context.Run.Depth);
			writer.WriteInt32(Context.Run.KillCount);
			writer.WriteFloat(Context.Run.Time);
			writer.WriteByte((byte) Context.Run.PermanentScourge);
			writer.WriteString(Context.Run.Seed);
			writer.WriteString(Context.Run.Type.ToString());
			writer.WriteInt32(Context.Run.DailyId);
			writer.WriteInt32(Context.Run.Loop);
			writer.WriteBoolean(Context.Run.CustomSeed);
		}

		public override string GetPath(string path, bool old = false) {
			return $"{path}game.sv";
		}

		public override void Load(Area area, FileReader reader) {
			if (Context.Run.HasRun) {
				return;
			}
			
			Values.Clear();
			var Count = reader.ReadInt32();

			for (var I = 0; I < Count; I++) {
				var Key = reader.ReadString();
				var Val = reader.ReadString();
				
				Values[Key!] = Val;
			}

			Context.Run.HasRun = true;
			Context.Run.LastSavedDepth = reader.ReadSbyte();
			
			Context.Run.KillCount = reader.ReadInt32();
			Context.Run.Time = reader.ReadFloat();

			Context.Run.PermanentScourge = reader.ReadByte();

			var seed = reader.ReadString();
			if (Context.Run.LastSavedDepth > 0) {
				Rnd.Seed = Context.Run.Seed = seed;
			}

			if (Enum.TryParse<RunType>(reader.ReadString(), out var t)) {
				Context.Run.Type = t;
			} else {
				Context.Run.Type = RunType.Regular;
			}

			Context.Run.DailyId = reader.ReadInt32();
			Context.Run.Loop = reader.ReadInt32();
			Context.Run.CustomSeed = reader.ReadBoolean();
		}

		public static int PeekDepth(FileReader reader) {
			return reader.ReadSbyte();
		}
		
		public override void Generate(Area area) {
			Values.Clear();
			Context.Run.ResetStats();
			
			if (GlobalSave.IsFalse("finished_tutorial")) {
				if (BK.Version.Dev) {
					GlobalSave.Put("finished_tutorial", true);
				} else if (Context.Run.Depth != -2 && Context.Run.Depth != -3) {
					Context.Run.Depth = -3;
					Context.Run.IntoMenu = true;
					Log.Info("Throwing the player into tutorial");
				}
			}

			Put("mimic_chance", 0.2f);
		}

		public GameSave() : base(SaveType.Game) {
			
		}

		public override void Delete() {
			base.Delete();
			Values.Clear();
		}
	}
}