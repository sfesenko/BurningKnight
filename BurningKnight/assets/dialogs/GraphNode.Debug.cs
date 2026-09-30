using System;
using BurningKnight.assets;
using ImGuiNET;
using Lens.input;
using Lens.util.camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace BurningKnight.assets.dialogs {
	// The editor's half of the node: rendering, dragging and deletion. A release build excludes
	// every *.Debug.cs (ADR-0003), so the data half never references ImGui.
	public partial class GraphNode {
		private const int connectorRadius = 7;
		private const int connectorRadiusSquare = connectorRadius * connectorRadius;
		private static Color connectorColor = new Color(0.6f, 0.6f, 0.6f, 1f); 
		private static Color hoveredConnectorColor = new Color(1f, 1f, 1f, 1f);
		private static Color connectionColor = new Color(0.6f, 1f, 0.6f, 0.6f);
		private static Color hoveredConnectionColor = new Color(0.3f, 1f, 0.3f, 1f);
		private static Color nodeBg = new Color(0f, 0f, 0f, 0.8f);
		private static Color hoveredNodeBg = new Color(0.1f, 0.1f, 0.1f, 0.8f);
		private static Color activeNodeBg = new Color(0.2f, 0.2f, 0.2f, 0.8f);
		private static Vector2 connectorVector = new Vector2(connectorRadius);
		public static GraphNode Focused;
		public static Vector2 Offset;
		public GraphConnection CurrentActive;
		public bool Done;
		private static bool IsConnectorHovered(Vector2 connector) {
			var mouse = ImGui.GetIO().MousePos;

			var dx = mouse.X - connector.X;
			var dy = mouse.Y - connector.Y;

			return (dx * dx + dy * dy) < connectorRadiusSquare;
		}
		private unsafe void RenderConnector(ImDrawListPtr list, GraphConnection connection, Vector2 connector) {
			var hovered = IsConnectorHovered(connector);
			list.AddCircleFilled(connector, connectorRadius, (hovered ? hoveredConnectorColor : connectorColor).PackedValue);

			if ((connection.ConnectedTo.Count == 0 || connection.Input) && !OnScreen) {
				return;
			}

			if (!OnScreen) {
				var found = false;

				foreach (var c in connection.ConnectedTo) {
					if (c.Parent.OnScreen) {
						found = true;
						break;
					}
				}

				if (!found) {
					return;
				}
			}

			if (hovered) {
				if (ImGui.IsMouseClicked(0)) {
					justStarted = true;

					if (CurrentActive != null && CurrentActive == connection) {
						CurrentActive.Active = false;
						CurrentActive = null;
					} else {
						if (CurrentActive != null) {
							CurrentActive.Active = false;
						}

						CurrentActive = connection;
						CurrentActive.Active = true;
						
						if (ImGuiHelper.CurrentActive == null) {
							ImGuiHelper.CurrentActive = this;
						} else {
							var active = ImGuiHelper.CurrentActive;
							ImGuiHelper.CurrentActive = null;
							
							if (active.CurrentActive != CurrentActive && active.CurrentActive.Input != CurrentActive.Input) {
								active.CurrentActive.ConnectedTo.Add(CurrentActive);
								CurrentActive.ConnectedTo.Add(active.CurrentActive);

							}
							
							active.justStarted = true;
							active.CurrentActive.Active = false;
							active.CurrentActive = null;

							if (CurrentActive != null) {
								CurrentActive.Active = false;
								CurrentActive = null;
							}
						}
					}
				} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && connection.ConnectedTo.Count > 0) {
					RemoveConnection(connection);
				}
			}

			if (!connection.Input && connection.ConnectedTo.Count > 0) {
				foreach (var to in connection.ConnectedTo) {
					var p = to.Parent.Position + to.Offset;
					DrawHermite(ImGui.GetBackgroundDrawList(), connector, p, 12, hovered || IsConnectorHovered(p) ? hoveredConnectionColor : connectionColor);	
				}
			}

			if (CurrentActive == connection) {
				DrawHermite(ImGui.GetBackgroundDrawList(), connector, ImGui.GetIO().MousePos, 12, hovered ? hoveredConnectionColor : connectionColor);	
			}
		}
		public void RemoveConnection(GraphConnection connection, bool remove = false) {
			if (ImGuiHelper.CurrentActive == this) {
				ImGuiHelper.CurrentActive = null;
			} else {
				foreach (var to in connection.ConnectedTo) {
					if (ImGuiHelper.CurrentActive == to.Parent) {
						ImGuiHelper.CurrentActive = null;
						break;
					}
				}
			}

			foreach (var to in connection.ConnectedTo) {
				to.Active = false;
				to.ConnectedTo.Remove(connection);
				to.Parent.CurrentActive = null;
			}
					
			connection.ConnectedTo.Clear();
			connection.Active = false;

			CurrentActive = null;

			if (remove) {
				if (connection.Input) {
					Inputs.Remove(connection);
				} else {
					Outputs.Remove(connection);
				}
			}
		}
		private bool hovered;
		private bool focused;
		private bool justStarted;
		public bool ForceFocus;
		public bool New;
		private bool readPosition;
		public void RemoveEmptyConnection() {
			if (ImGuiHelper.CurrentActive == this) {
				if (justStarted) {
					justStarted = false;
				} else if (ImGui.IsMouseClicked(0) && CurrentActive != null) {
					CurrentActive.Active = false;
					CurrentActive = null;
					ImGuiHelper.CurrentActive = null;
				}
			}
		}
		public bool OnScreen;
		public void RenderConnections() {
			var rightSide = Position + new Vector2(Size.X, 0);
			var list = OnScreen ? ImGui.GetWindowDrawList() : ImGui.GetForegroundDrawList();

			list.PushClipRect(Position - connectorVector, Position + Size + connectorVector);

			foreach (var input in Inputs) {
				RenderConnector(list, input, Position + input.Offset);
			}
			
			foreach (var output in Outputs) {
				RenderConnector(list, output, rightSide + output.Offset);
			}
			
			list.PopClipRect();
		}
		public virtual void Render() {
			if (!New) {
				Position = RealPosition + Offset;
			}

			if (readPosition && Context.Camera != null && (Position.X + Size.X < 0 || Position.X > Context.Camera.Width || Position.Y + Size.Y < 0 || Position.Y > Context.Camera.Height)) {
				OnScreen = false;
				RenderConnections();
				return;
			}

			OnScreen = true;
			
			if (outputs != null) {
				ReadOutputs();
			}
			
			if (Focused != this) {
				focused = false;
				ForceFocus = false;
			}
			
			if (focused || ForceFocus) {
				ImGui.PushStyleColor(ImGuiCol.WindowBg, activeNodeBg.PackedValue);
			} else if (hovered) {
				ImGui.PushStyleColor(ImGuiCol.WindowBg, hoveredNodeBg.PackedValue);
			} else {
				ImGui.PushStyleColor(ImGuiCol.WindowBg, nodeBg.PackedValue);
			}

			if (New) {
				ImGui.SetNextWindowPos(Position, ImGuiCond.Always);
				RealPosition = Position - Offset; // fixme
			} else if (readPosition) {
				ImGui.SetNextWindowPos(RealPosition + Offset, ImGuiCond.Always);
			}

			ImGui.Begin($"node_{Tip}_{Id}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize);
			var old = ImGuiHelper.CurrentMenu;
			ImGuiHelper.CurrentMenu = this;
			ImGuiHelper.RenderMenu(true);

			RenderElements();
			RenderConnections();
			
			hovered = ImGui.IsWindowHovered();
			focused = ImGui.IsWindowFocused();

			if (focused) {
				Focused = this;
			}

			if (New) {
				New = false;
			} else {
				readPosition = true;
				Size = ImGui.GetWindowSize();
			}

			Moved = false;
			
			if (hovered) {
				if (ImGui.IsMouseDragging(0) || Input.Keyboard.IsDown(Keys.LeftControl, true)) {
					var d = ImGui.GetIO().MouseDelta;
					RealPosition += d;	
				} else if (Input.Keyboard.IsDown(Keys.LeftShift, true)) {
					Move(ImGui.GetIO().MouseDelta);
				} else if (Input.Keyboard.IsDown(Keys.LeftWindows, true)) {
					MoveBoth(ImGui.GetIO().MouseDelta);
				}
			}

			ImGui.End();
			ImGui.PopStyleColor();
			ImGuiHelper.CurrentMenu = old;

			if (Done) {
				Remove();
			}
		}
		public bool Moved;
		public void Move(Vector2 d) {
			if (Moved) {
				return;
			}
			
			Moved = true;
			RealPosition += d;

			foreach (var c in Outputs) {
				foreach (var cc in c.ConnectedTo) {
					cc.Parent.Move(d);
				}
			}
		}
		public void MoveBoth(Vector2 d) {
			if (Moved) {
				return;
			}
			
			Moved = true;
			RealPosition += d;

			foreach (var c in Outputs) {
				foreach (var cc in c.ConnectedTo) {
					cc.Parent.Move(d);
				}
			}
			
			foreach (var c in Inputs) {
				foreach (var cc in c.ConnectedTo) {
					cc.Parent.Move(d);
				}
			}
		}
		public void Remove() {
			Done = true;
			
			if (ImGuiHelper.CurrentActive == this) {
				ImGuiHelper.CurrentActive = null;
			}

			foreach (var i in Inputs) {
				foreach (var t in i.ConnectedTo) {
					t.ConnectedTo.Remove(i);
				}

				i.ConnectedTo.Clear();
			}

			foreach (var i in Outputs) {
				foreach (var t in i.ConnectedTo) {
					t.ConnectedTo.Remove(i);
				}

				i.ConnectedTo.Clear();
			}
		}
		public virtual void RenderElements() {
			
		}
		public static unsafe void DrawHermite(ImDrawList* drawList, Vector2 p1, Vector2 p2, int steps, Color color) {
			var t1 = new Vector2(80.0f, 0.0f);
			var t2 = new Vector2(80.0f, 0.0f);

			for (var step = 0; step <= steps; step++) {
				var t = (float) step / steps;
				var h1 = +2 * t * t * t - 3 * t * t + 1.0f;
				var h2 = -2 * t * t * t + 3 * t * t;
				var h3 = t * t * t - 2 * t * t + t;
				var h4 = t * t * t - t * t;

				ImGuiNative.ImDrawList_PathLineTo(drawList, new Vector2(h1 * p1.X + h2 * p2.X + h3 * t1.X + h4 * t2.X, h1 * p1.Y + h2 * p2.Y + h3 * t1.Y + h4 * t2.Y));
			}

			ImGuiNative.ImDrawList_PathStroke(drawList, color.PackedValue, 0, 3.0f);
		}
	}
}