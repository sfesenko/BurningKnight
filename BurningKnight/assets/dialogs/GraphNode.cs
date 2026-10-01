using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Lens.util;
using Vector2 = System.Numerics.Vector2;

namespace BurningKnight.assets.dialogs {
	// The dialog graph's data model: the loader creates nodes, reads their connections and converts
	// them to dialogs. The editor's half — rendering, dragging, deletion — lives in
	// GraphNode.Debug.cs, which a release build excludes (ADR-0003).
	public partial class GraphNode {
		public const int InputHalfHeight = 8;

		public static int LastId;

		public int Id;
		public Vector2 Position;
		public Vector2 RealPosition;
		public Vector2 Size;
		public List<GraphConnection> Inputs = new List<GraphConnection>();
		public List<GraphConnection> Outputs = new List<GraphConnection>();
		public string Tip = null!;
		public string File = null!;
		
		public string LocaleId => $"{File}_{Id}";
		
		public GraphNode() {
			Id = LastId++;
		}

		public void AddInput() {
			Inputs.Add(new GraphConnection {
				Input = true,
				Parent = this,
				Id = Inputs.Count
			});
		}

		public void AddOutput() {
			Outputs.Add(new GraphConnection {
				Parent = this,
				Id = Outputs.Count
			});
		}

		private JsonArray? outputs = null!;

		public void ReadOutputs() {
			var j = -1;

			if (outputs != null) {
				foreach (var i in outputs!) {
					j++;

					if (!i.IsJsonArray()) {
						continue;
					}
						
					foreach (var o in i.AsJsonArray()!) {
						if (!o.IsJsonArray() || o.AsJsonArray()!.Count == 0) {
							continue;
						}
							
						var to = DialogGraph.Nodes[o![0].Int()];
						var from = Outputs[j];
						var where = to.Inputs[o![1].Int()];

						from.ConnectedTo.Add(where);
						where.ConnectedTo.Add(from);
					}
				}
			}

			outputs = null;
		}

		private JsonArray SaveConnections(List<GraphConnection> connections) {
			var root = new JsonArray();

			foreach (var c in connections) {
				var array = new JsonArray();
				root.Add(array);

				if (c.ConnectedTo.Count == 0) {
					array.Add(new JsonArray());
					continue;
				}

				foreach (var cc in c.ConnectedTo) {
					array.Add(new JsonArray {
						cc.Parent.Id, 
						cc.Id
					});
				}
			}
			
			return root;
		}
		
		public virtual void Save(JsonObject root) {
			root["id"] = Id;
			root["outputs"] = SaveConnections(Outputs);
			root["type"] = GraphNodeRegistry.GetName(this);
			root["x"] = RealPosition.X;
			root["y"] = RealPosition.Y;
		}

		public virtual void Load(JsonObject root) {
			Id = root["id"].AsInteger();
			outputs = root["outputs"].AsJsonArray();

			RealPosition.X = root["x"].Number();
			RealPosition.Y = root["y"].Number();

			LastId = Math.Max(LastId, Id);
		}

		public virtual string GetName() {
			return "Node";
		}

		public static GraphNode? Create(string file, JsonNode? vl, bool ignoreId = false) {
			if (!vl.IsJsonObject()) {
				return null;
			}
			
			var type = vl?["type"];

			if (!type.IsString()) {
				return null;
			}

			var node = GraphNodeRegistry.Create(type.String());

			if (node == null) {
				Log.Error($"Unknown node type {type.String()}");
				return null;
			}

			node.Tip = type.String();
			node.File = file;
			node.Load(vl.AsJsonObject()!);

			if (ignoreId) {
				node.Id = LastId;
			}
			
			DialogGraph.Nodes[node.Id] = node;
			return node;
		}
	}
}
