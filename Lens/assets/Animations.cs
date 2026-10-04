using System;
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
		private static Dictionary<string, AnimationSource> sources = new();

		public static void Load()
		{
			var file = FileHandle.FromRoot("Animations/animations.json");

			if (!file.Exists())
			{
				Log.Error($"Can't load animations from: {file}");
				return;
			}

			// Only the index is read here; sheets load on first use. Dropping the loaded ones lets a
			// reload pick up new art. Their textures are left to the finalizer, because an Animation
			// may still be drawing one mid-frame and Animations.Reload makes consumers refetch.
			animations.Clear();
			sources = new Dictionary<string, AnimationSource>();

			using var document = JsonDocument.Parse(file.ReadAll());
			var root = document.RootElement;
			var version = root.GetProperty("version").GetInt32();

			if (version != 1)
			{
				Log.Error($"Unsupported animations.json version: {version}");
				return;
			}

			foreach (var entry in root.GetProperty("animations").EnumerateObject())
			{
				sources[entry.Name] = AnimationUtils.ReadSource(entry.Value);
			}
		}

		internal static void Destroy() {
			foreach (var animation in animations.Values) {
				animation.Texture.Dispose();
			}

			animations.Clear();
			sources.Clear();
		}

		public static AnimationData? Get(string id) {
			if (animations.TryGetValue(id, out var animation)) {
				return animation;
			}

			if (!sources.TryGetValue(id, out var source)) {
				Log.Error($"Animation {id} was not found!");
				return null;
			}

			// MonoGame does not premultiply here, and the sheets are premultiplied already.
			// Buffer on the caller thread and upload only a valid stream: FromStream runs
			// on the main thread via Gpu, so a throw there would bypass the caller's
			// try/catch and kill the process instead of failing the load. OpenRead is
			// guarded too: the archive source reads the whole entry inside Open, so a
			// corrupt deflate/CRC throws from Open itself (see Textures.LoadTexture).
			MemoryStream copy;

			try {
				using (var stream = FileHandle.FromRoot($"Animations/{id}.png").OpenRead()) {
					if (stream == null) {
						Log.Error($"Animation sheet {id} was not found!");

						return null;
					}

					copy = new MemoryStream();
					stream.CopyTo(copy);
					copy.Position = 0;
				}
			} catch (Exception e) {
				Log.Error($"Failed to read animation sheet {id}: {e}");

				return null;
			}

			Texture2D? texture = null;

			try {
				using (copy) {
					// The decode runs on the main thread inside Gpu.Flush: a throw there
					// bypasses this caller's try/catch, so catch inside the closure.
					texture = Gpu.Run(() => {
						try {
							return Texture2D.FromStream(Engine.GraphicsDevice, copy);
						} catch (Exception e) {
							Log.Error($"Failed to decode animation sheet {id}: {e}");

							return null;
						}
					});
				}
			} catch (Exception e) {
				Log.Error($"Failed to decode animation sheet {id}: {e}");

				return null;
			}

			if (texture == null) {
				return null;
			}

			animation = AnimationUtils.LoadAnimation(texture, source);
			animations[id] = animation;

			return animation;
		}

		// Fail-fast accessor: every mandatory lookup goes through here so the
		// message is built in one place instead of being copy-pasted per site.
		public static AnimationData Require(string id) {
			return Get(id) ?? throw new InvalidOperationException($"Animation '{id}' failed to load.");
		}

		public static Animation Create(string id, string? layer = null) {
			return new Animation(Require(id), layer);
		}

		public static AnimationData? GetColored(string id, ColorMap colorMap) {
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

			AnimationData? data = null;

			try {
				// Same no-throw Gpu contract as above: GetData/new Texture2D/SetData
				// run on the main thread, so catch inside the closure.
				data = Gpu.Run(() => {
					try {
						return animation.Recolor(colorMap);
					} catch (Exception e) {
						Log.Error($"Failed to recolor animation {id}: {e}");

						return null;
					}
				});
			} catch (Exception e) {
				Log.Error($"Failed to recolor animation {id}: {e}");

				return null;
			}

			if (data == null) {
				return null;
			}

			animations[fullId] = data;
			
			return data;
		}
	}
}