using Microsoft.Xna.Framework;

namespace Lens.input;

// Rumble differs per platform: desktop sustains and stops a motor value; MonoGame's Android
// backend plays one fixed one-shot and ignores strength. The host supplies the implementation.
public interface IRumble {
	void Play(PlayerIndex player, float strength, float time);
	void Update(PlayerIndex player, float dt);
	void Stop(PlayerIndex player);
}

// Default when the host supplies none: a no-op, so game code never null-checks.
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
