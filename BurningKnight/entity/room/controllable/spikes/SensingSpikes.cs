using BurningKnight.assets.achievements;
using BurningKnight.entity.creature.player;
using BurningKnight.state;

namespace BurningKnight.entity.room.controllable.spikes {
	public class SensingSpikes : Spikes {
		private float timer;
		private bool wasTriggered;
		
		public override void Update(float dt) {
			base.Update(dt);

			if (timer > 0) {
				timer -= dt;

				if (timer <= 0) {
					TurnOff();
				}
			} else if (!On && Colliding.Count > 0) {
				foreach (var c in Colliding) {
					if (c is Player) {
						timer = 1.5f;
						TurnOnSlowly();

						if (!wasTriggered) {
							wasTriggered = true;

							if (Context.Run.Statistics != null) {
								Context.Run.Statistics.SpikesTriggered++;

								if (Context.Run.Statistics.SpikesTriggered >= 100) {
									Achievements.Unlock("bk:spikes");
								}
							}
						}
						
						break;
					}
				}
			}
		}
	}
}