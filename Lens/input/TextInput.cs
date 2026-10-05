using System;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Lens.input;

// Window.TextInput is desktop-only: the Android build has the event args but no GameWindow
// member. Resolve once by reflection; where the event doesn't exist, the handlers never fire.
public static class TextInput {
	private static readonly EventInfo? Event = typeof(GameWindow).GetEvent("TextInput");
	private static readonly MethodInfo? Add = Event?.GetAddMethod();
	private static readonly MethodInfo? Remove = Event?.GetRemoveMethod();

	public static bool Available => Add != null;

	public static void Subscribe(GameWindow window, EventHandler<TextInputEventArgs> handler) {
		Add?.Invoke(window, new object[] { handler });
	}

	public static void Unsubscribe(GameWindow window, EventHandler<TextInputEventArgs> handler) {
		Remove?.Invoke(window, new object[] { handler });
	}
}
