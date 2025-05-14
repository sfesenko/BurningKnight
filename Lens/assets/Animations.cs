using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lens.graphics.animation;
using Lens.util;

namespace Lens.assets {
	public struct Animations {
		public static bool Reload;
		private static readonly Dictionary<string, AnimationData> animations = new();
		
		internal static void Load()
		{
			var animationDir = Path.Combine(Assets.Root, "Animations");
			if (!Directory.Exists(animationDir))
			{
				Log.Error($"Can't load animations from: {animationDir}");
				return;
			}

			var jobs = Directory.GetFiles(animationDir, "*.ase")
				.Select(file => Task.Run(() => (file, animation: AnimationUtils.LoadAnimation(file))))
				.ToList();
			foreach(var t in jobs)
			{
				var (file, animation) = t.Result;
				var name = Path.GetFileNameWithoutExtension(file);
				animations[name] = animation;
			}
		}

		internal static void Destroy() {
			foreach (var animation in animations.Values) {
				animation.Texture.Dispose();
			}
			animations.Clear();
		}

		public static AnimationData Get(string id) {
			if (animations.TryGetValue(id, out var animation)) {
				return animation;
			}
			
			Log.Error($"Animation {id} was not found!");
			return null;
		}

		public static Animation Create(string id, string layer = null) {
			return new Animation(Get(id), layer);
		}

		public static AnimationData GetColored(string id, ColorSet set) {
			if (set == null) {
				return Get(id);
			}
			
			var fullId = $"{id}_{set.Id}";
			
			if (animations.TryGetValue(fullId, out var animation)) {
				return animation;
			}
			
			animation = Get(id);

			if (animation == null) {
				return null;
			}

			var data = animation.Recolor(set);			
			
			animations[fullId] = data;
			
			return data;
		}
	}
}