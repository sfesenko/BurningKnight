using System.Collections.Generic;
using System.Numerics;
using BurningKnight.ui.imgui.node;

namespace BurningKnight.ui.imgui;

public class ImConnection {
	public Vector2 Offset;
	public readonly List<ImConnection> ConnectedTo = [];
	public ImNode Parent;
	public bool Input;
	public int Id;
	public bool Active;
}