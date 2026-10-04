using System;
using System.IO;
using Lens.util;
using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace Lens.assets {
	// MonoGame's Song opens from a file path only — its constructors are private and its playback
	// path is not overridable — so the archive's music is played here instead: NVorbis, the decoder
	// MonoGame itself ships, reads the entry from the content source and feeds a dynamic sound
	// instance, which is a public MonoGame API. Nothing is written to disk.
	public class MusicPlayer : IDisposable {
		private const int SamplesPerBuffer = 4096;

		private readonly VorbisReader? reader;
		private readonly DynamicSoundEffectInstance? instance;
		private readonly float[] samples = new float[SamplesPerBuffer];
		private readonly byte[] buffer = new byte[SamplesPerBuffer * 2];

		public MusicPlayer(string name) {
			using var source = Assets.Source.Open($"Music/{name}.ogg");

			if (source == null) {
				Log.Error($"Music {name} was not found!");
				return;
			}

			// NVorbis seeks — it reads the last page for the length and loops by seeking back — and
			// a deflated archive entry cannot, so the track is read into memory first. That is the
			// whole file, a few megabytes, and only while it plays.
			var memory = new MemoryStream();

			source.CopyTo(memory);
			memory.Position = 0;

			reader = new VorbisReader(memory, true);
			Log.Debug($"Music {name}: {reader.TotalSamples / (double) reader.SampleRate:F1}s at {reader.SampleRate} Hz, {reader.Channels} channel(s)");

			instance = new DynamicSoundEffectInstance(reader.SampleRate, (AudioChannels) reader.Channels);
			instance.BufferNeeded += (sender, args) => Submit();

			Submit();
		}

		public bool Ready => instance != null;

		// Every member below is touched from tweens and loops all over Audio.cs. Two
		// failure modes must never reach those callers: the instance being null (no
		// device) and the voice being dead (device loss across pause/resume throws
		// from setters/getters even though the reference is non-null). All of it is
		// funneled through Guard so the properties stay one-liners.
		private void Guard(Action<DynamicSoundEffectInstance> action) {
			var i = instance;

			if (i == null) {
				return;
			}

			try {
				action(i);
			} catch {
				// Dead voice: degrade (fade stops, track ends) instead of crashing.
			}
		}

		private T Guard<T>(Func<DynamicSoundEffectInstance, T> read, T fallback) {
			var i = instance;

			if (i == null) {
				return fallback;
			}

			try {
				return read(i);
			} catch {
				return fallback;
			}
		}

		public float Volume {
			get => Guard(i => i.Volume, 0f);
			set => Guard(i => i.Volume = value);
		}

		public SoundState State => Guard(i => i.State, SoundState.Stopped);

		private bool failed;

		// Runs from MonoGame's BufferNeeded on the main update thread: a corrupt or
		// truncated tail that survives the header makes NVorbis throw mid-stream, and
		// an uncaught throw here lands in the frame loop and kills the process. Catch
		// inside (same rule as the Gpu closures) and stop feeding after the first
		// failure so this cannot log per buffer or submit an empty buffer.
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
