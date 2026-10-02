using Microsoft.Xna.Framework;

namespace Lens.input;

// How a rumble reaches the motors differs by platform: a desktop sustains a motor value and can
// stop it, while MonoGame's Android backend plays one fixed one-shot and ignores strength — a
// "stop" there would only start another buzz. The host supplies the implementation; a null one
// means the platform has no rumble at all.
public interface IRumble {
	void Play(PlayerIndex player, float strength, float time);
	void Update(PlayerIndex player, float dt);
	void Stop(PlayerIndex player);
}

public static class Vibration {
	public static IRumble? Instance;
}
