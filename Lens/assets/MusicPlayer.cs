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

		private readonly VorbisReader reader;
		private readonly DynamicSoundEffectInstance instance;
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

		public float Volume {
			get => instance?.Volume ?? 0;
			set {
				if (instance != null) {
					instance.Volume = value;
				}
			}
		}

		public SoundState State => instance?.State ?? SoundState.Stopped;

		private void Submit() {
			var read = reader.ReadSamples(samples, 0, SamplesPerBuffer);

			if (read == 0) {
				// The end: loop back to the start.
				reader.SamplePosition = 0;
				read = reader.ReadSamples(samples, 0, SamplesPerBuffer);
			}

			for (var i = 0; i < read; i++) {
				var value = (short) (Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);

				buffer[i * 2] = (byte) value;
				buffer[i * 2 + 1] = (byte) (value >> 8);
			}

			instance.SubmitBuffer(buffer, 0, read * 2);
		}

		public void Play() {
			instance.Stop();
			reader.SamplePosition = 0;
			Submit();
			instance.Play();
		}

		public void Stop() {
			instance.Stop();
		}

		public void Dispose() {
			instance?.Dispose();
			reader?.Dispose();
		}
	}
}
