using System;
using System.Collections.Generic;
using Lens.util;
using Lens.util.file;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.assets {
	public static class Effects {
		public static Dictionary<string, Effect> All = new Dictionary<string, Effect>();
		
		public static void Load() {
			var shaderDir = FileHandle.FromRoot("Shaders/");
			
			if (shaderDir.Exists()) {
				foreach (var h in shaderDir.ListFileHandles()) {
					if (h.Extension == ".xnb") {
						var file = h;

						Gpu.Defer(() => LoadEffect(file));
					}
				}

				Gpu.Wait();
			}
		}

		private static void LoadEffect(FileHandle handle)
		{
			// Runs inside Gpu.Flush on the main thread: catch inside the closure.
			try {
				var assetName = $"Shaders/{handle.NameWithoutExtension}";
				var effect = Assets.Content.Load<Effect>(assetName);
				All[handle.NameWithoutExtension] = effect;
			} catch (Exception e) {
				Log.Error($"Failed to load effect {handle.NameWithoutExtension}: {e}");
			}
		}
		
		public static void Destroy() {
			foreach (var e in All) {
				e.Value.Dispose();
			}	
			
			All.Clear();
		}

		public static Effect? Get(string id) {
			return All.TryGetValue(id, out var o) ? o : null;
		}

		// Fail-fast: mandatory lookup, message built in one place.
		public static Effect Require(string id) {
			return Get(id) ?? throw new InvalidOperationException($"Shader '{id}' failed to load.");
		}
	}
}