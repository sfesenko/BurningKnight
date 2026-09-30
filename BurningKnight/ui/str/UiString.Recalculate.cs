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
		private void Recalculate() {
			glyphs.Clear();
			effects.Clear();
			renderers.Clear();
			
			StartTyping();

			var (builder, events) = CalculateLabel(parsingToken: false);

			label = builder.ToString();
			builder.Clear();
			var spaceWidth = (int) (font.MeasureString("a a").Width - font.MeasureString("aa").Width);

			var glp = font.GetGlyphs(label);

			if (WidthLimit > 0) {
				var k = 0;
				var lastSpace = 0;
				var sinceLastSpace = 0;
				var sinceLast = 0;
				var width = 0;
				var first = true;
				var i = 0;
				
				foreach (var g in glp) {
					var c = label[k];
					var w = 0;

					switch (c)
					{
						case ' ':
							lastSpace = k;
							sinceLastSpace = 0;
							w = spaceWidth;
							break;
						case '\n':
							sinceLast = 0;
							sinceLastSpace = 0;
							width = 0;
							break;
						default:
							// null checks for missing font glyphs
							w = g.Character?.TextureRegion?.Width ?? 8;
							break;
					}

					var hadIcon = false;
					
					if (renderers.Count > 0) {
						foreach (var r in renderers) {
							if (r.Where == i && r is IconRenderer ir) {
								w += ir.GetWidth(this) - spaceWidth;
								hadIcon = true;
							}
						}
					}
					
					i++;

					if (c != '\n' || hadIcon) {
						sinceLast += w;
						width += w;

						if (c != ' ') {
							sinceLastSpace += w;
						}
					}

					builder.Append(c);
					
					if (width >= WidthLimit) {
						if (first) {
							WidthLimit = width - sinceLastSpace - spaceWidth;
							first = false;
						}

						width -= sinceLast;
						
						sinceLast = width;
						builder[lastSpace] = '\n';
					}

					k++;
				}

				label = builder.ToString();
			}
			
			glp = font.GetGlyphs(label);
			var size = font.MeasureString(label);

			FinalWidth = size.Width;
			finalHeight = size.Height - 4;

			var j = 0;
			var ww = 0;

			foreach (var g in glp) {
				var gl = new Glyph {
					G = g
				};
				
				if (label[j] == '\n') {
					ww = 0;
				}
				
				gl.G.Position.X += ww;
				
				if (renderers.Count > 0) {
					foreach (var r in renderers) {
						if (r.Where == j && r is IconRenderer ir) {
							var v = ir.GetWidth(this) - spaceWidth;
							ww += v;
							FinalWidth += v;
						}
					}
				}

				gl.Reset();
				glyphs.Add(gl);

				for (var i = events.Count - 1; i >= 0; i--) {
					var e = events[i];

					if (e.I == j) {
						gl.Events.Add(e);
						events.RemoveAt(i);
					}
				}

				j++;
			}
		}
		private (StringBuilder, List<GlyphEvent>) CalculateLabel(bool parsingToken)
		{
			var builder = new StringBuilder();
			var events = new List<GlyphEvent>();

			var token = new StringBuilder();
			var lc = '\0';
			foreach (var c in label)
			{
				if (parsingToken) {
					if (c == ']') {
						parsingToken = false;
						
						var t = token.ToString().TrimStart().TrimEnd();
						var parts = t.Split(null);

						if (parts.Length == 0) {
							continue;
						}

						GlyphEvent? e = null;

						switch (parts[0]) {
							case "skp": {
								e = new SkipEvent();
								break;
							}

							case "sp": {
								e = new SpeedEvent();
								break;
							}

							case "dl": {
								e = new DelayEvent();
								break;
							}

							case "ev": {
								e = new UserEvent();
								break;
							}

							case "vr": {
								if (parts.Length > 1) {
									if (Variables.TryGetValue(parts[1], out var vr)) {
										builder.Append(vr == null ? "null" : vr.ToString());
									} else {
										Log.Error($"Undefined variable {parts[1]}!");
									}

									break;
								}
								
								continue;
							}

							case "rn": {
								try {
									renderers.Add(new StrRenderer {
										Id = parts.Length > 1 ? int.Parse(parts[1]) : 0,
										Where = builder.Length
									});
								} catch (Exception ex) {
									Log.Error(ex);
								}
								
								break;
							}

							case "ic": {
								try {
									renderers.Add(new IconRenderer {
										Id = parts.Length > 1 ? int.Parse(parts[1]) : 0,
										Where = builder.Length
									});
									
									builder.Append(' ');
								} catch (Exception ex) {
									Log.Error(ex);
								}
								
								break;
							}

							case "cl": {
								if (parts.Length >= 2) {
									var ef = new ColorEffect();

									ef.ParseColor(parts[1]);
									ef.Start = builder.Length;
									effects.Add(ef);
								} else {
									var en = FindEffect<ColorEffect>();

									if (en != null) {
										en.End = builder.Length;
										en.Closed = true;
									}
								}
								
								break;
							}

							case "/cl": {
								AddEffect<WaveEffect>(builder);
								break;
							}
						}

						if (e != null) {
							e.I = builder.Length;
							e.Parse(parts);
							
							events.Add(e);
						}
						
						continue;
					}

					token.Append(c);
					continue;
				}
				
				switch (c) {
					case '[': {
						if (lc == '\\') {
							builder.Remove(builder.Length - 1, 1);
							builder.Append('[');
							break;
						}
						
						parsingToken = true;
						token.Clear();
						break;
					}

					case '^': {
						if (lc == '^') {
							builder.Remove(builder.Length - 1, 1);
							AddEffect<WaveEffect>(builder);
						} else {
							builder.Append(c);
						}
						
						break;
					}

					case '*': {
						if (lc == '*') {
							builder.Remove(builder.Length - 1, 1);
							AddEffect<BoldEffect>(builder);
						} else {
							builder.Append(c);
						}

						break;
					}

					case '_': {
						if (lc == '\\') {
							builder.Remove(builder.Length - 1, 1);
							builder.Append(c);
						} else {
							AddEffect<ItalicEffect>(builder);
						}

						break;
					}

					case '%': {
						if (lc == '%') {
							builder.Remove(builder.Length - 1, 1);
							AddEffect<RainbowEffect>(builder);
						} else {
							builder.Append(c);
						}
						
						break;
					}
					
					case '&': {
						if (lc == '&') {
							builder.Remove(builder.Length - 1, 1);
							AddEffect<FlipEffect>(builder);
						} else {
							builder.Append(c);
						}

						continue;
					}

					case '@': {
						if (lc == '@') {
							builder.Remove(builder.Length - 1, 1);
							AddEffect<BlinkEffect>(builder);
						} else {
							builder.Append(c);
						}
						
						break;
					}

					case '#': {
						if (lc == '#') {
							builder.Remove(builder.Length - 1, 1);
							AddEffect<ShakeEffect>(builder);
						} else {
							builder.Append(c);
						}
						
						break;
					}

					case '~': {
						if (lc == '~') {
							builder.Remove(builder.Length - 1, 1);
							AddEffect<RandomEffect>(builder);
						} else {
							builder.Append(c);
						}
						
						break;
					}

					default: {
						builder.Append(c);
						break;
					}
				}

				lc = c;
			}

			foreach (var e in effects) {
				if (!e.Closed) {
					e.End = builder.Length;
				}
			}

			return (builder, events);
		}
	}
}
