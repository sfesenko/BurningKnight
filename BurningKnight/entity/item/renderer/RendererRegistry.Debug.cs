namespace BurningKnight.entity.item.renderer {
	// The editor half: the item editor renders a renderer by its registered callback. A release
	// build excludes every *.Debug.cs (ADR-0003).
	public static partial class RendererRegistry {
		static partial void RegisterDebugRenderers() {
			Register<AngledRenderer>(AngledRenderer.RenderDebug);
			Register<MovingAngledRenderer>(MovingAngledRenderer.RenderDebug);
			Register<StickRenderer>(StickRenderer.RenderDebug);
		}
	}
}
