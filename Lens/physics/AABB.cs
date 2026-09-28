using Microsoft.Xna.Framework;

namespace Lens.physics;

public struct AABB {
	public Vector2 LowerBound;
	public Vector2 UpperBound;

	public Vector2 Center => 0.5f * (LowerBound + UpperBound);
}
