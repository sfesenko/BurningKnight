using System;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.creature.drop {
	// The editor half: the item editor renders a drop by its registered callback. A release build
	// excludes every *.Debug.cs (ADR-0003).
	public static partial class DropRegistry {
		static partial void DefineDebugRenderers() {
			SetRenderer<AnyDrop>(AnyDrop.RenderDebug);
			SetRenderer<EmptyDrop>(EmptyDrop.RenderDebug);
			SetRenderer<OneOfDrop>(OneOfDrop.RenderDebug);
			SetRenderer<PoolDrop>(PoolDrop.RenderDebug);
			SetRenderer<SimpleDrop>(SimpleDrop.RenderDebug);
			SetRenderer<SingleDrop>(SingleDrop.RenderDebug);
		}

		private static void SetRenderer<T>(Action<JsonNode> render) where T : Drop {
			foreach (var id in Defined.Keys) {
				var info = Defined[id];

				if (info.Type == typeof(T)) {
					info.Render = render;
					Defined[id] = info;
					return;
				}
			}
		}
	}
}
