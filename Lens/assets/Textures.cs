using System;
using System.Collections.Generic;
using System.IO;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.assets {
	public static class Textures {
		private static Dictionary<string, TextureRegion> textures = new();
		public static TextureRegion Missing = null!; // Load() sets it
		
		public static void Load() {
			var textureDir = FileHandle.FromRoot("Textures/");

			if (textureDir.Exists()) {
				QueueTextures(textureDir);

				// The device belongs to the main thread; a worker waits for the uploads.
				Gpu.Wait();
			}
		}

		public static Texture2D FastLoad(string path) {
			try {
				using var stream = Assets.Source.Open(path);

				if (stream == null) {
					Log.Error($"Texture {path} was not found, using a fallback");

					return Fallback();
				}

				// Decode runs on the main thread inside Gpu.Flush: catch inside the closure
				// (Flush would otherwise hand back null and the fallback contract breaks).
				var texture = Gpu.Run(() => {
					try {
						return Texture2D.FromStream(Engine.GraphicsDevice, stream);
					} catch (Exception e) {
						Log.Error($"Failed to decode texture {path}: {e}");

						return null;
					}
				});

				return texture ?? Fallback();
			} catch (Exception e) {
				// Boot-time load on the main thread: one corrupt file must not crash boot.
				Log.Error($"Failed to fast-load texture {path}: {e}");

				return Fallback();
			}
		}

		private static Texture2D Fallback() {
			return Gpu.Run(() => {
				try {
					var texture = new Texture2D(Engine.GraphicsDevice, 1, 1);
					texture.SetData([Color.White]);

					return texture;
				} catch (Exception e) {
					// Even this can fail on a dead device: log and hand back null rather than
					// throw out of FastLoad's fallback path.
					Log.Error(e);

					return null!;
				}
			});
		}
		
		private static void QueueTextures(FileHandle handle) {
			foreach (var h in handle.ListFileHandles()) {
				var file = h;

				Gpu.Defer(() => LoadTexture(file));
			}

			foreach (var h in handle.ListDirectoryHandles()) {
				QueueTextures(h);
			}
		}

		private static void LoadTexture(FileHandle handle) {
			var region = new TextureRegion();
			string id = handle.NameWithoutExtension;

			try {
				using var fileStream = handle.OpenRead();

				if (fileStream == null) {
					Log.Error($"Texture {id} is missing, skipping");

					return;
				}

				region.Texture = Texture2D.FromStream(Engine.GraphicsDevice, fileStream);
			} catch (Exception e) {
				// One bad asset must not kill boot: log it and keep loading the rest.
				Log.Error($"Failed to load texture {id}: {e}");

				return;
			}

			region.Source = region.Texture.Bounds;
			region.Center = new Vector2(region.Source.Width / 2f, region.Source.Height / 2f);
			
			textures[id] = region;
		}

		internal static void Destroy() {
			foreach (var region in textures.Values) {
				region.Texture?.Dispose();
			}
			
			textures.Clear();
		}
		
		public static TextureRegion Get(string id) {
			if (textures.TryGetValue(id, out var region)) {
				return region;
			}
			
			Log.Error($"Texture {id} was not found!");
			return Missing;
		}
	}
}