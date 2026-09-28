using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Lens.physics;

/// <summary>A polygon's points, in the body's local space.</summary>
public class Vertices : List<Vector2> {
	public Vertices() { }
	public Vertices(int capacity) : base(capacity) { }
	public Vertices(IEnumerable<Vector2> vertices) : base(vertices) { }
}
