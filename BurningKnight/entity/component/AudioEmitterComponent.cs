using System;
using System.Collections.Generic;
using System.Linq;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component;
using Lens.util;
using Lens.util.math;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace BurningKnight.entity.component {
	public class AudioEmitterComponent : Component {
		public static AudioListener? Listener;
		public static Vector2 ListenerPosition;
		
		public static float PositionScale = 0.00000001f;
		public static float Distance = 200;
		
		public AudioEmitter Emitter = new AudioEmitter();
		public float PitchMod;
		public bool DestroySounds = false;

		public Dictionary<string, Sfx> Playing = new();

		private static Audio Audio => Context.Audio;
		
		public class Sfx {
			public SoundEffectInstance Effect = null!;
			public float BaseVolume = 1f;
			public bool KeepAround;
			public bool ApplyBuffer;
			public bool Dead;
			public TweenTask? Tween;
		}
		
		public override void Destroy() {
			base.Destroy();

			if (DestroySounds) {
				StopAll();
			}
		}

		public void StopAll() {
			foreach (var s in Playing.Values) {
				// A pending delayed tween must not touch a disposed instance when it fires.
				s.Dead = true;
				s.Tween?.Ended = true;

				try {
					s.Effect.Stop();
				} catch (Exception e) {
					// Dead voice (device loss): teardown continues regardless.
					Log.Error(e);
				}

				try {
					s.Effect.Dispose();
				} catch (Exception e) {
					Log.Error(e);
				}
			}

			Playing.Clear();
		}
		
		private void UpdatePosition() {
			Emitter.Position = new Vector3(Entity.CenterX * PositionScale, 0, Entity.CenterY * PositionScale);

			if (Listener != null) {
				var d = (ListenerPosition - Entity.Center).Length();

				foreach (var s in Playing.Values)
				{
					try {
						var sfxVolumeBuffer = (1 - Math.Min(Distance, d) / Distance) 
						                      * Settings.MasterVolume 
						                      * Settings.SfxVolume 
						                      * s.BaseVolume 
						                      * (s.ApplyBuffer ? Audio.SfxVolumeBuffer : 1);
						
						s.Effect.Volume = MathUtils.Clamp(0, 1, sfxVolumeBuffer);
					} catch (Exception e) {
						// Dead voice (device loss): the Update loop drops it below.
						Log.Error(e);
					}
				}
			}
		} // 6y0204mm

		public override void Update(float dt) {
			base.Update(dt);

			if (Playing.Count == 0) {
				return;
			}

			UpdatePosition();
			var keys = Playing.Keys.ToArray();
			
			foreach (var k in keys) {
				var s = Playing[k];

				try {
					if (!s.KeepAround && s.Effect.State != SoundState.Playing) {
						Playing.Remove(k);
						// A finished instance still owns its OpenAL source: the pool only gets it back
						// on Stop or Dispose. Leaving it to the GC drains the pool and Play starts
						// throwing InstancePlayLimitException.
						s.Dead = true;
						s.Tween?.Ended = true;
						s.Effect.Dispose();
					} else if (Listener != null) {
						s.Effect.Apply3D(Listener, Emitter);
					}
				} catch (Exception e) {
					// Dead voice (device loss across pause/resume): drop it, keep the rest.
					// A live device that threw on State/Apply3D still owns its OpenAL
					// source — release it like the finished path above, or the pool drains.
					Log.Error(e);
					Playing.Remove(k);
					s.Dead = true;
					s.Tween?.Ended = true;

					try {
						s.Effect.Dispose();
					} catch (Exception disposeException) {
						Log.Error(disposeException);
					}
				}
			}
		}

		public SoundEffectInstance? EmitRandomizedPrefixed(string sfx, int prefixMax, float volume = 1f, bool insert = true, bool looped = false, bool tween = false, float sz = 0.4f) {
			if (sfx == null) {
				return null;
			}
		
			return Emit($"{sfx}_{Rnd.Int(1, prefixMax + 1)}", volume, PitchMod + Rnd.Float(-sz, sz), insert, looped, tween);
		}
		
		public SoundEffectInstance? EmitRandomized(string sfx, float volume = 1f, bool insert = true, bool looped = false, bool tween = false, float sz = 0.4f) {
			if (sfx == null) {
				return null;
			}
			
			return Emit(sfx, volume, PitchMod + Rnd.Float(-sz, sz), insert, looped, tween);
    }

		private static bool TryPlay(SoundEffectInstance effect) {
			try {
				effect.Play();

				return true;
			} catch (InstancePlayLimitException) {
				// The OpenAL source pool is finite and a burst can exhaust it. A dropped sound
				// must not kill the run.
				return false;
			} catch (Exception e) {
				// Lost device across pause/resume, disposed instance: same, never fatal.
				Log.Error(e);

				return false;
			}
		}

		public SoundEffectInstance? Emit(string sfx, float volume = 1f, float pitch = 0f, bool insert = true, bool looped = false, bool tween = false) {
			if (!Assets.LoadSfx || sfx == null) {
				return null;
			}

			Sfx instance;
			var v = volume * 0.8f;

			if (!insert) {
				v *= Audio.MasterVolume * Audio.SfxVolume * Audio.SfxVolumeBuffer;
			}
			
			var applyBuffer = !sfx.StartsWith("level_explosion");

			/*if (applyBuffer) {
				v *= Audio.SfxVolumeBuffer;
			}*/

			if (!insert || !Playing.TryGetValue(sfx, out instance!)) {
				var sound = Audio.GetSfx(sfx);

				if (sound == null) {
					return null;
				}

				// CreateInstance/IsLooped touch the OpenAL source pool: an exhausted or
				// dead device throws, and Emit must degrade to a silent no-op like
				// TryPlay does instead of crashing the caller.
				SoundEffectInstance? effect = null;

				try {
					effect = sound.CreateInstance();
					effect.IsLooped = looped;
				} catch (Exception e) {
					Log.Error(e);

					try {
						effect?.Dispose();
					} catch {
						// Already dead.
					}

					return null;
				}

				instance = new Sfx {
					Effect = effect,
					KeepAround = tween,
					ApplyBuffer = applyBuffer
				};

				if (insert) {
					Playing[sfx] = instance;
				}
			}

			instance.BaseVolume = tween ? 0 : v;

			if (tween) {
				var t = Tween.To(v, 0, x => instance.BaseVolume = x, 0.5f);

				t.Delay = 1f;
				instance.Tween = t;
				t.OnStart = () => {
					if (instance.Dead) {
						return;
					}

					TryPlay(instance.Effect);
					instance.KeepAround = false;

					if (Listener != null) {
						try {
							instance.Effect.Apply3D(Listener, Emitter);
						} catch (Exception e) {
							Log.Error(e);
						}
					}
				};
			}

			UpdatePosition();

			try {
				instance.Effect.Stop();
				instance.Effect.Pitch = MathUtils.Clamp(-1f, 1f, pitch);
			} catch (Exception e) {
				Log.Error(e);
			}

			if (!tween) {
				TryPlay(instance.Effect);
			}

			if (Listener != null) {
				try {
					instance.Effect.Apply3D(Listener, Emitter);
				} catch (Exception e) {
					Log.Error(e);
				}
			}
			
			return instance.Effect;
		}

		public static AudioEmitterComponent Dummy(Area area, Vector2 where) {
			var entity = new EmitterDummy();
			var component = new AudioEmitterComponent();

			area.Add(entity);
			entity.AddComponent(component);
			entity.Center = where;
			entity.AlwaysActive = true;

			return component;
		}

		private class EmitterDummy : Entity {
			public override void Update(float dt) {
				base.Update(dt);
				
				if (GetComponent<AudioEmitterComponent>()!.Playing.Count == 0) {
					Done = true;
				}
			}
		}
	}
}
