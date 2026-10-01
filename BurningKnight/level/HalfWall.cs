using Lens.entity;

namespace BurningKnight.level {
	public class HalfWall : Entity {
		public Level Level = null!;

		public override void AddComponents() {
			base.AddComponents();
			AddComponent(new HalfWallBodyComponent {
				Level = Level
			});
		}
	}
}