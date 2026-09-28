using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Lens.graphics.animation;
using Lens.util;
using Lens.util.file;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.assets {
	public struct Animations {
		public static bool Reload;
		private static readonly Dictionary<string, AnimationData> animations = new();
		private static Dictionary<string, JsonElement> sources = new();
		private static JsonDocument document;

		internal static void Load()
		{
			var file = FileHandle.FromRoot("bin/Animations/animations.json");

			if (!file.Exists())
			{
				Log.Error($"Can't load animations from: {file}");
				return;
			}

			// Only the index is read here; sheets load on first use. Dropping the loaded ones lets a
			// reload pick up new art. Their textures are left to the finalizer, because an Animation
			// may still be drawing one mid-frame and Animations.Reload makes consumers refetch.
			animations.Clear();
			sources = new Dictionary<string, JsonElement>();

			document?.Dispose();
			document = JsonDocument.Parse(file.ReadAll());

			var root = document.RootElement;
			var version = root.GetProperty("version").GetInt32();

			if (version != 1)
			{
				Log.Error($"Unsupported animations.json version: {version}");
				return;
			}

			foreach (var entry in root.GetProperty("animations").EnumerateObject())
			{
				sources[entry.Name] = entry.Value;
			}
		}

		internal static void Destroy() {
			foreach (var animation in animations.Values) {
				animation.Texture.Dispose();
			}

			animations.Clear();
			sources.Clear();

			document?.Dispose();
			document = null;
		}

		public static AnimationData Get(string id) {
			if (animations.TryGetValue(id, out var animation)) {
				return animation;
			}

			if (!sources.TryGetValue(id, out var source)) {
				Log.Error($"Animation {id} was not found!");
				return null;
			}

			var path = FileHandle.FromRoot($"bin/Animations/{id}.png").FullPath;

			using var stream = new FileStream(path, FileMode.Open);

			// MonoGame does not premultiply here, and the sheets are premultiplied already.
			var texture = Texture2D.FromStream(Engine.GraphicsDevice, stream);

			animation = AnimationUtils.LoadAnimation(texture, source);
			animations[id] = animation;

			return animation;
		}

		public static Animation Create(string id, string layer = null) {
			return new Animation(Get(id), layer);
		}

		public static AnimationData GetColored(string id, ColorMap colorMap) {
			if (colorMap.IsEmpty())
			{
				return Get(id);
			}
			
			var fullId = $"{id}_{colorMap.Id}";
			
			if (animations.TryGetValue(fullId, out var animation)) {
				return animation;
			}
			
			animation = Get(id);

			if (animation == null) {
				return null;
			}

			var data = animation.Recolor(colorMap);			
			
			animations[fullId] = data;
			
			return data;
		}
	}
}