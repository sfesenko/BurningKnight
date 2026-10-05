using System.Collections.Generic;
using Lens.input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Desktop.core;

// Desktop motors hold the value they are set to, so a rumble is sustained and decayed here.
public class DesktopRumble : IRumble {
	private class Motor {
		public float Strength;
		public float Time;
	}

	private readonly Dictionary<PlayerIndex, Motor> motors = new();

	public void Play(PlayerIndex player, float strength, float time) {
		if (!GamePad.SetVibration(player, strength, strength)) {
			return;
		}

		if (!motors.TryGetValue(player, out var motor)) {
			motors[player] = motor = new Motor();
		}

		motor.Strength = strength;
		motor.Time = time;
	}

	public void Update(PlayerIndex player, float dt) {
		if (!motors.TryGetValue(player, out var motor) || motor.Time <= 0) {
			return;
		}

		motor.Time -= dt;
		motor.Strength -= dt;

		if (motor.Time <= 0 || motor.Strength < 0) {
			GamePad.SetVibration(player, 0, 0);
			motors.Remove(player);
		} else {
			GamePad.SetVibration(player, motor.Strength, motor.Strength);
		}
	}

	public void Stop(PlayerIndex player) {
		GamePad.SetVibration(player, 0, 0);
		motors.Remove(player);
	}
}
