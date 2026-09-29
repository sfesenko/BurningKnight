using Lens.lightJson;
using Lens.util;
using Lens.util.tween;

namespace BurningKnight.entity.item.renderer {
	public partial class MovingAngledRenderer : AngledRenderer {
		public float MaxAngle;		
		public float MinAngle;
		public bool Stay;
		public float SwingTime;
		public float ReturnTime;
		private bool stayed;
		
		public override void OnUse() {
			var task = Tween.To(stayed ? MinAngle : MaxAngle, SwingAngle, x => SwingAngle = x, SwingTime);
			
			if (!Stay) {
				task.OnEnd = () => {
					Tween.To(stayed ? MinAngle : MaxAngle, SwingAngle, x => SwingAngle = x, ReturnTime);
				};
			} else {
				stayed = !stayed;
			}
		}

		public override void Setup(JsonValue settings) {
			base.Setup(settings);

			Stay = settings["stay"].Bool(false);
			MaxAngle = settings["max_angle"].Number(180).ToRadians();
			MinAngle = settings["min_angle"].Number(0).ToRadians();
			SwingTime = settings["st"].Number(0.1f);
			ReturnTime = settings["rt"].Number(0.2f);

			SwingAngle = MinAngle;
		}

	}
}