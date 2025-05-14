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
		public static TextureRegion Missing;
		
		internal static void Load() {
			var textureDir = FileHandle.FromRoot("Textures/");
			
			if (textureDir.Exists()) {
				LoadTextures(textureDir);
			}
		}

		public static Texture2D FastLoad(string path) {
			return Texture2D.FromFile(Engine.GraphicsDevice, path);
		}
		
		private static void LoadTextures(FileHandle handle) {
			foreach (var h in handle.ListFileHandles()) {
				LoadTexture(h);
			}

			foreach (var h in handle.ListDirectoryHandles()) {
				LoadTextures(h);
			}
		}

		private static void LoadTexture(FileHandle handle) {
			var region = new TextureRegion();
			string id = handle.NameWithoutExtension;

			if (Assets.LoadOriginalFiles) {
				var fileStream = new FileStream(handle.FullPath, FileMode.Open);
				region.Texture = Texture2D.FromStream(Engine.GraphicsDevice, fileStream);
				fileStream.Dispose();
			} else {
				region.Texture = Assets.Content.Load<Texture2D>($"bin/Textures/{handle.NameWithoutExtension}");
			}

			region.Source = region.Texture.Bounds;
			region.Center = new Vector2(region.Source.Width / 2f, region.Source.Height / 2f);
			
			textures[id] = region;
		}

		internal static void Destroy() {
			foreach (var region in textures.Values) {
				region.Texture.Dispose();
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