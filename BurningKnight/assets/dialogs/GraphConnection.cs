using System.Collections.Generic;
using System.Numerics;

namespace BurningKnight.assets.dialogs;

public class GraphConnection {
	public Vector2 Offset;
	public readonly List<GraphConnection> ConnectedTo = [];
	public GraphNode Parent;
	public bool Input;
	public int Id;
	public bool Active;
}