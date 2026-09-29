using System;
using System.Collections.Generic;
using System.Text;
using BurningKnight.ui.str.effect;
using BurningKnight.ui.str.@event;
using Lens;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;

namespace BurningKnight.ui.str {
	public partial class UiString {
		public bool ShouldntRender => Engine.Instance.State.Paused || label == null || Tint.A == 0 || glyphs.Count == 0;
		public bool DisableRender;
		public override void Render() {
			if (ShouldntRender || DisableRender) {
				return;
			}

			RenderString();
		}
		public void RenderString() {
			var m = 1;
			var n = m - 1;
			var l = (int) Math.Min(progress + n, glyphs.Count);

			for (var i = 0; i < l; i++) {
				var g = glyphs[i];
				
				if (g.G.Character?.TextureRegion != null) {
					var pos = new Vector2(
						Position.X + g.G.Position.X + g.Offset.X + g.Origin.X,
						Position.Y + g.G.Position.Y + g.Offset.Y + g.Origin.Y
					);

					var c = g.Color;

					if (progress > i && progress < i + 1) {
						float v = (progress + n - i) / m;
						
						pos.Y -= (1 - v) * 8f;
						c.A = (byte) (v * 255);
					}

					c.A = (byte) MathUtils.Clamp(0, 255, (float) c.A * Tint.A / 255f);

					Graphics.Batch.Draw(g.G.Character.TextureRegion.Texture, pos,
						g.G.Character.TextureRegion.Bounds, c, g.Angle, g.Origin, g.Scale, g.Effects, 0);
				}

				if (renderers.Count > 0) {
					Graphics.Color.A = (byte) MathUtils.Clamp(0, 255, (float) g.Color.A * Tint.A / 255f);

					foreach (var r in renderers) {
						if (r.Where == i) {
							if (r is IconRenderer ir) {
								if (ir.Region != null) {
									Graphics.Render(ir.Region, new Vector2(Position.X + g.G.Position.X + g.Offset.X + g.Origin.X,
										Position.Y + g.G.Position.Y + (ir.Region.Height - 7) / 2f - ir.Region.Height + 1 + g.Offset.Y + g.Origin.Y));
								}
							} else {
								Renderer(Position + g.G.Position + g.Offset - new Vector2(0, 9), r.Id);
							}
						}
					}
				}
				
				Graphics.Color.A = 255;
			}
		}
	}
}
