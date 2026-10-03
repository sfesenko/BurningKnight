using Lens.input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AndroidPort.core;

// MonoGame's Android backend ignores strength and always plays a fixed 500 ms one-shot, even for
// a zero request. There is nothing to sustain or stop: one call is the whole buzz, and a stop
// would only start another.
public class AndroidRumble : IRumble {
	public void Play(PlayerIndex player, float strength, float time) {
		if (strength <= 0 || time <= 0) {
			return;
		}

		GamePad.SetVibration(player, strength, strength);
	}

	public void Update(PlayerIndex player, float dt) {
	}

	public void Stop(PlayerIndex player) {
	}
}
