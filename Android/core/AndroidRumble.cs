using System;
using Lens.input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AndroidPort.core;

// MonoGame's Android backend ignores strength: one fixed 500 ms one-shot, even for a zero
// request. Nothing to sustain or stop — a "stop" would only buzz again.
public class AndroidRumble : IRumble {
	public void Play(PlayerIndex player, float strength, float time) {
		if (strength <= 0 || time <= 0) {
			return;
		}

		try {
			GamePad.SetVibration(player, strength, strength);
		} catch (Exception e) {
			// Missing VIBRATE or dead service: rumble is cosmetic, never fatal.
			Lens.util.Log.Error(e);
		}
	}

	public void Update(PlayerIndex player, float dt) {
	}

	public void Stop(PlayerIndex player) {
	}
}
