using Microsoft.Xna.Framework;

namespace Lens.input;

// How a rumble reaches the motors differs by platform: a desktop sustains a motor value and can
// stop it, while MonoGame's Android backend plays one fixed one-shot and ignores strength — a
// "stop" there would only start another buzz. The host supplies the implementation; until it
// does (or on a platform with no rumble at all) the NoRumble default silently absorbs every call.
public interface IRumble {
	void Play(PlayerIndex player, float strength, float time);
	void Update(PlayerIndex player, float dt);
	void Stop(PlayerIndex player);
}

// The default when the host supplies no rumble: every call is a no-op, so game code never
// null-checks. A platform with no rumble at all simply never replaces it.
public sealed class NoRumble : IRumble {
	public static readonly NoRumble Default = new();

	private NoRumble() {
	}

	public void Play(PlayerIndex player, float strength, float time) {
	}

	public void Update(PlayerIndex player, float dt) {
	}

	public void Stop(PlayerIndex player) {
	}
}

public static class Vibration {
	private static volatile IRumble instance = NoRumble.Default;

	public static IRumble Instance {
		get => instance;
		set => instance = value;
	}

	public static bool Available => instance is not NoRumble;

	public static void Play(PlayerIndex player, float strength, float time) {
		instance.Play(player, strength, time);
	}

	public static void Update(PlayerIndex player, float dt) {
		instance.Update(player, dt);
	}

	public static void Stop(PlayerIndex player) {
		instance.Stop(player);
	}
}
