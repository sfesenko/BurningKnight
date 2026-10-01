using System;
using System.Collections.Generic;

namespace BurningKnight.assets.dialogs {
	public static class GraphNodeRegistry {
		public static Dictionary<string, Type> Defined = new Dictionary<string, Type>();

		static GraphNodeRegistry() {
			Define<OrdNode>("Ord");
			Define<TextNode>("Text");
			Define<TextOutputNode>("Text output");
			Define<TextInputNode>("Text input");
			Define<DialogNode>("Dialog");
			Define<ChoiceNode>("Choice");
			Define<AnswerNode>("Answer");
		}
		
		public static void Define<T>(string name) where T : GraphNode {
			Defined[name] = typeof(T);
		}

		public static GraphNode? Create(string name) {
			if (!Defined.TryGetValue(name, out var type)) {
				return null;
			}
			
			var node = (GraphNode) Activator.CreateInstance(type);
			node!.Tip = name;
			
			return node;
		}

		public static string GetName(GraphNode node) {
			return node.Tip;
		}
	}
}