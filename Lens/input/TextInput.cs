using System;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace Lens.input;

// Window.TextInput is a desktop-only event: the Android MonoGame build carries the event args
// but has no such member on GameWindow, so a direct subscription — compiled against the desktop
// package — is a MissingMethodException on the device. The facade resolves the event once by
// reflection, so the game can wire the same handlers on every platform; where the event does not
// exist, the handlers simply never fire.
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
