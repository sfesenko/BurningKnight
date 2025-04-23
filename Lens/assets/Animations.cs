using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aseprite;
using Lens.graphics;
using Lens.graphics.animation;
using Lens.util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

			foreach (var file in Directory.GetFiles(animationDir, "*.ase"))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				var animation = LoadAnimation(file);
				animations[name] = animation;
			}
		}
		
		private static AnimationData LoadAnimation(string fileName)
		{
			var file = new AsepriteFile(fileName);
			var texture = new Texture2D(Engine.GraphicsDevice, file.TextureWidth, file.TextureHeight + 1);
			texture.SetData(file.pixelData);
			
			var animation = new AnimationData();
			
			for (var i = 0; i < file.Layers.Count; i++) {
				var layer = file.Layers[i];
				var list = new List<AnimationFrame>();
				
				for (var j = 0; j < file.Frames.Count; j++) {
					var frame = file.Frames[j];
					var newFrame = new AnimationFrame
					{
						Duration = frame.Duration,
						Texture = new TextureRegion(texture, new Rectangle(j * file.Width, i * file.Height, file.Width, file.Height))
					};

					newFrame.Bounds = newFrame.Texture.Source;
					
					list.Add(newFrame);
				}
				
				animation.Layers[layer.Name] = list;
			}
			
			foreach (var slice in file.Slices)
			{
				animation.Slices[slice.Name] = new TextureRegion(texture, new Rectangle(slice.OriginX, slice.OriginY, slice.Width, slice.Height));
			}
			
			foreach (var tag in file.Animations.Values) {
				var newTag = new AnimationTag
				{
					Direction = (AnimationDirection) tag.Directions,
					StartFrame = (uint) tag.FirstFrame,
					EndFrame = (uint) tag.LastFrame
				};

				animation.Tags[tag.Name] = newTag;
			}
			
			foreach (var tag in file.Tags) {
				var newTag = new AnimationTag
				{
					Direction = (AnimationDirection) tag.LoopDirection,
					StartFrame = (uint) tag.From,
					EndFrame = (uint) tag.To
				};

				animation.Tags[tag.Name] = newTag;
			}


			animation.Texture = texture;
			return animation;
		}

		internal static void Destroy() {
			foreach (var animation in animations.Values) {
				animation.Texture.Dispose();
			}
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
			
			var data = new AnimationData();
			var w = animation.Texture.Width;
			var h = animation.Texture.Height;
			var texture = new Texture2D(Engine.GraphicsDevice, w, h);
			var tdata = new Color[w * h];
			
			animation.Texture.GetData(tdata);
			var pixelData = new Color[w * h];
			
			for (var y = 0; y < h; y++) {
				for (var x = 0; x < w; x++) {
					var i = x + y * w;
					var color = tdata[i];

					for (var c = 0; c < set.From.Length; c++) {
						if (ColorUtils.Compare(set.From[c], color, 4)) {
							color = set.To[c];
						}
					}
					
					pixelData[i] = color;
				}
			}
			
			texture.SetData(pixelData);

			foreach (var l in animation.Layers)
			{
				data.Layers[l.Key] =
					l.Value.Select(f => f with { Texture = new TextureRegion(texture, f.Bounds) })
						.ToList();
			}

			foreach (var (key, value) in animation.Slices) {
				data.Slices[key] = new TextureRegion(texture, value.Source);
			}

			foreach (var t in animation.Tags) {
				data.Tags[t.Key] = t.Value;
			}
			
			data.Texture = texture;
			animations[fullId] = data;
			
			return data;
		}
	}
}