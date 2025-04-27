using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lens.entity;
using Microsoft.Xna.Framework;

namespace Lens.util.math {
	public static class Rnd {
		private static string seed;

		public static Random Generator { get; private set; } = new(Guid.NewGuid().GetHashCode());

		public static string Seed {
			get => seed;

			set {
				seed = value;
				IntSeed = ParseSeed(seed);
				Generator = new Random(IntSeed);
			}
		}

		public static int IntSeed { get; private set; }

		public const string SeedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

		static Rnd() {
			Seed = GenerateSeed();
			Log.Debug($"Random seed is {seed}");
		}

		private static int ParseSeed(string seed1) {
			if (seed1 == null) {
				return 0;
			}
			
			var value = 0;
			var i = 0;

			foreach (var c in seed1) {
				var index = SeedChars.IndexOf(c);

				if (index == -1) {
					Log.Error($"Unknown seed char '{c}' ({(int) c})!");
					continue;
				}

				value += index << (i * 4);
				i++;
			}
			
			return value;
		}

		public static string GenerateSeed(int len = 8, int seed = -1) {
			var builder = new StringBuilder();
			var r = seed == -1 ? new Random(Environment.TickCount + Guid.NewGuid().GetHashCode()) : new Random(seed);

			for (var i = 0; i < len; i++) {
				builder.Append(SeedChars[r.Next(SeedChars.Length - 1)]);
			}
			
			return builder.ToString();
		}

		public static Vector2 Vector(float min, float max) {
			if (min > max) {
				(min, max) = (max, min);
			}
			
			return new Vector2(Float(min, max), Float(min, max));
		}

		public static int Int(int max) {
			return Generator.Next(0, max);
		}
		
		public static int Int(int min, int max) {
			if (min > max) {
				(min, max) = (max, min);
			}
		
			return Generator.Next(min, max);
		}

		public static int IntCentred(int min, int max) {
			if (min <= max) return (int)((Int(min, max) + Int(min, max)) / 2f - 0.1f);
			(min, max) = (max, min);

			return (int) ((Int(min, max) + Int(min, max)) / 2f - 0.1f);
		}

		public static float Float() {
			return (float) Generator.NextDouble();
		}
		
		public static float Float(float max) {
			return (float) (Generator.NextDouble() * max);
		}

		public static float Float(float min, float max) {
			if (!(min > max)) return (float)(Generator.NextDouble() * (max - min) + min);
			(min, max) = (max, min);

			return (float) (Generator.NextDouble() * (max - min) + min);
		}

		public static Vector2 Offset(float d) {
			var a = AnglePI();
			return new Vector2((float) Math.Cos(a) * d, (float) Math.Sin(a) * d);
		}
		
		public static double Double() {
			return Generator.NextDouble();
		}
		
		public static double Double(double max) {
			return Generator.NextDouble() * max;
		}

		public static double Double(double min, double max) {
			return Generator.NextDouble() * (max - min) + min;
		}

		public static bool Bool() {
			return Generator.NextDouble() >= 0.5;
		}

		public static bool Chance(float chance = 50) {
			return Generator.NextDouble() * 100 <= chance;
		}

		public static float Angle() {
			return Float(360);
		}

		public static float AnglePI() {
			return Float((float) (Math.PI * 2));
		}

		public static int Chances(float[] chances) {
			var length = chances.Length;
			float sum = chances.Sum();

			float value = Float(sum);
			sum = 0;

			for (int i = 0; i < length; i++) {
				sum += chances[i];

				if (value < sum) {
					return i;
				}
			}

			return -1;
		}

		public static int Chances(List<float> chances) {
			var length = chances.Count;
			var sum = chances.Sum();

			var value = Float(sum);
			sum = 0;

			for (int i = 0; i < length; i++) {
				sum += chances[i];

				if (value < sum) {
					return i;
				}
			}

			return -1;
		}

		public static int Sign() {
			return Chance() ? -1 : 1;
		}

		public static T Element<T>(List<T> list, Func<T, bool> filter) where T : Entity {
			var length = list.Count;
			var sum = list.Count(filter);

			int value = Int(sum);
			sum = 0;

			for (int i = 0; i < length; i++) {
				if (filter(list[i])) {
					sum++;
					
					if (value < sum) {
						return list[i];
					}
				}
			}

			return null;
		}
	}
}