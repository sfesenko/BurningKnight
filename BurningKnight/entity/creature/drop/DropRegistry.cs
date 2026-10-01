using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.creature.drop {
	public static partial class DropRegistry {
		public static Dictionary<string, DropInfo> Defined = new Dictionary<string, DropInfo>();

		static DropRegistry() {
			Define<AnyDrop>("any");
			Define<EmptyDrop>("empty");
			Define<OneOfDrop>("one");
			Define<PoolDrop>("pool");
			Define<SimpleDrop>("simple");
			Define<SingleDrop>("single");

			DefineDebugRenderers();
		}
		
		public static void Define<T>(string id) where T : Drop {
			Defined[id] = new DropInfo {
				Id = id,
				Type = typeof(T)
			};
		}

		// Implemented in DropRegistry.Debug.cs, which a release build excludes (ADR-0003).
		static partial void DefineDebugRenderers();
	}
}
