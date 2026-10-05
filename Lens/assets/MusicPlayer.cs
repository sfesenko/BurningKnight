using System;
using System.IO;
using Lens.util;
using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace Lens.assets {
	// MonoGame's Song opens from a file path only, so the archive's music plays here instead:
	// NVorbis (the decoder MonoGame ships) reads the entry straight from the content source
	// into a dynamic sound instance. Nothing is written to disk.
	public class MusicPlayer : IDisposable {
		private const int SamplesPerBuffer = 4096;

		private readonly VorbisReader? reader;
		private readonly DynamicSoundEffectInstance? instance;
		private readonly float[] samples = new float[SamplesPerBuffer];
		private readonly byte[] buffer = new byte[SamplesPerBuffer * 2];

		public MusicPlayer(string name) {
			try {
				using var source = Assets.Source.Open($"Music/{name}.ogg");

				if (source == null) {
					Log.Error($"Music {name} was not found!");
					return;
				}

				// NVorbis seeks, a deflated entry cannot — read the whole track into memory first.
				var memory = new MemoryStream();

				source.CopyTo(memory);
				memory.Position = 0;

				reader = new VorbisReader(memory, true);
				Log.Debug($"Music {name}: {reader.TotalSamples / (double) reader.SampleRate:F1}s at {reader.SampleRate} Hz, {reader.Channels} channel(s)");

				instance = new DynamicSoundEffectInstance(reader.SampleRate, (AudioChannels) reader.Channels);
				instance.BufferNeeded += (sender, args) => Submit();

				Submit();
			} catch (Exception e) {
				// Dead device or corrupt track: stay not-Ready, free what was created.
				Log.Error($"Music {name} failed to load: {e}");

				reader?.Dispose();
				instance?.Dispose();

				reader = null;
				instance = null;
			}
		}

		public bool Ready => instance != null;

		// Callers are tweens/loops all over Audio.cs: null instance and dead voice (device loss
		// throws from setters even when the reference is live) must never reach them.
		private void Guard(Action<DynamicSoundEffectInstance> action) {
			var i = instance;

			if (i == null) {
				return;
			}

			try {
				action(i);
			} catch (ObjectDisposedException) {
				// Dead voice: degrade (fade stops, track ends) instead of crashing.
			} catch (InvalidOperationException) {
				// Device lost mid-call reports this; same degrade.
			} catch (Exception e) {
				// Anything else is a real bug — keep it visible.
				Log.Error(e);
			}
		}

		private T Guard<T>(Func<DynamicSoundEffectInstance, T> read, T fallback) {
			var i = instance;

			if (i == null) {
				return fallback;
			}

			try {
				return read(i);
			} catch (ObjectDisposedException) {
				return fallback;
			} catch (InvalidOperationException) {
				return fallback;
			} catch (Exception e) {
				Log.Error(e);

				return fallback;
			}
		}

		public float Volume {
			get => Guard(i => i.Volume, 0f);
			set => Guard(i => i.Volume = value);
		}

		public SoundState State => Guard(i => i.State, SoundState.Stopped);

		private bool failed;

		// BufferNeeded runs on the frame loop: a corrupt tail makes NVorbis throw there and kill
		// the process. Catch inside; stop feeding after the first failure (no per-buffer spam).
		private void Submit() {
			if (failed) {
				return;
			}

			try {
				var read = reader!.ReadSamples(samples, 0, SamplesPerBuffer);

				if (read == 0) {
					// The end: loop back to the start.
					reader!.SamplePosition = 0;
					read = reader!.ReadSamples(samples, 0, SamplesPerBuffer);
				}

				if (read == 0) {
					// Decodes to nothing: SubmitBuffer(count: 0) would throw.
					throw new InvalidDataException("Vorbis stream decoded to zero samples.");
				}

				for (var i = 0; i < read; i++) {
					var value = (short) (Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);

					buffer[i * 2] = (byte) value;
					buffer[i * 2 + 1] = (byte) (value >> 8);
				}

				instance!.SubmitBuffer(buffer, 0, read * 2);
			} catch (Exception e) {
				failed = true;
				Log.Error($"Music buffer submit failed: {e}");

				try {
					instance?.Stop();
				} catch {
					// Already dead; the track just ends.
				}
			}
		}

		public void Play() {
			try {
				instance!.Stop();
				reader!.SamplePosition = 0;
				Submit();
				instance!.Play();
			} catch (InstancePlayLimitException) {
				// Transient pool exhaustion: propagate so Audio leaves the track unset and
				// retries — latching `failed` here would mute it for the whole session.
				throw;
			} catch (Exception e) {
				// A dead device must not take the game down with the track.
				failed = true;
				Log.Error($"Music play failed: {e}");
			}
		}

		public void Stop() {
			Guard(i => i.Stop());
		}

		public void Dispose() {
			instance?.Dispose();
			reader?.Dispose();
		}
	}
}
