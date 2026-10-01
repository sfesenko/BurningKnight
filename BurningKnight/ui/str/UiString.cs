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
	public delegate void StartedTyping(UiString str);
	public delegate void FinishedTyping(UiString str);
	public delegate void CharTyped(UiString str, int i, char c);
	public delegate void EventFired(UiString str, string id);
	
	/*
	 * syntax:
	 *
	 * _ starts italic
	 * ** starts bold
	 * ## starts shake
	 * @@ starts blink
	 * && starts flip
	 * %% starts rainbow
	 * ^^ starts wave
	 * ~~ starts randomizer ~~
	 *
	 * [event_name var1 var2] starts an event
	 *  + [dl time] delays
	 *  + [skp] finishes printing out the string
	 *  + [sp speed] sets speed
	 *  + [ev event] fires user event
	 *  + [vr variable_name] replaced with user variable
	 *  + [cl color] sets color, can be hex or predefined string in Palette class
	 *  + [ic id] draws an icon with id=id
	 */
	
	/*
	 * todo:
	 * [tg a]test[dl a]
	 */
	public partial class UiString : Entity {
		private string label = null!;
		private BitmapFont font;
		private List<Glyph> glyphs = [];
		private List<GlyphEffect> effects = [];
		private List<StrRenderer> renderers = [];
		private float progress;
		private int lastChar;
		protected float FinalWidth;
		private float finalHeight;

		public float Delay;
		public bool Paused;
		public float Speed = 1f;
		public int WidthLimit;
		public bool Finished => progress >= glyphs.Count;

		public StartedTyping? StartedTyping;
		public FinishedTyping? FinishedTyping;
		public EventFired EventFired = null!;
		public CharTyped? CharTyped;
		public Action<Vector2, int> Renderer = null!;
		public readonly Dictionary<string, object> Variables = new();
		public readonly List<TextureRegion> Icons = [];

		public void SetVariable(string id, object o) {
			Variables[id] = o;
		}

		public void ClearIcons() {
			Icons.Clear();
		}
		
		public void AddIcon(TextureRegion o) {
			if (o == null) {
				Log.Error("Unknown icon");
			}
			
			Icons.Add(o!);
		}

		public Color Tint = Color.White;
		
		public string Label {
			get => label;
			set {
				label = value;
				Recalculate();
			}
		}

		public UiString(BitmapFont font) {
			this.font = font;

			Width = 4;
			Height = 4;
			AlwaysActive = true;
			AlwaysVisible = true;
		}

		private GlyphEffect FindEffect<T>(bool open = true) where T: GlyphEffect {
			var t = typeof(T);

			foreach (var e in effects) {
				if (e.GetType() == t && (!open || !e.Closed)) {
					return e;
				}
			}

			return null;
		}

		private void AddEffect<T>(StringBuilder builder) where T: GlyphEffect {
			var e = FindEffect<T>();

			if (e != null) {
				e.End = builder.Length;
				e.Closed = true;
			} else {
				var ef = Activator.CreateInstance<T>();

				ef.Start = builder.Length;
				effects.Add(ef);
			}
		}

		public void Stop() {
			Paused = true;
		}
		
		public void FinishTyping() {
			progress = glyphs.Count;
			Width = FinalWidth;
			Height = finalHeight;
			FinishedTyping?.Invoke(this);
		}

		public void StartTyping() {
			progress = 0f;
			lastChar = 0;
			Delay = 0;
			Paused = false;
			Speed = 1;

			if (WidthLimit > 0) {
				WidthLimit = 200;
			}
			
			StartedTyping?.Invoke(this);
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (Delay > 0 || Paused) {
				Delay -= dt;
			} else {
				progress = Math.Min(progress + Speed * dt * 25f, glyphs.Count);
				var v = (int) Math.Floor(progress);

				while (true) {
					if (v > lastChar) {
						lastChar++;

						if (v < glyphs.Count) {
							var g = glyphs[v];

							foreach (var e in g.Events) {
								e.Fire(this, g);
							}

							CharTyped?.Invoke(this, v, label[v]);

							if (g.G.Character?.TextureRegion != null) {
								Width = Math.Max(Width, g.G.Position.X + g.G.Character.TextureRegion.Width);
								Height = Math.Max(Height, g.G.Position.Y + g.G.Character.TextureRegion.Height);
							}
						}

						if ((int) Math.Ceiling(progress) == glyphs.Count) {
							FinishTyping();
						}
					} else {
						break;
					}
				}
			}
			
			foreach (var g in glyphs) {
				g.Reset();
			}

			for (var i = effects.Count - 1; i >= 0; i--) {
				var e = effects[i];
				e.Update(dt);
				
				for (var j = e.Start; j < Math.Min(glyphs.Count, e.End); j++) {
					e.Apply(glyphs[j], j);
				}
				
				if (e.Ended()) {
					effects.RemoveAt(i);
				}
			}
		}
	}
}