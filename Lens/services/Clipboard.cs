#nullable enable

namespace Lens.services;

public interface IClipboard {
	void SetText(string text);
	string GetText();
}

public static class Clipboard {
	public static IClipboard? Instance;

	public static void SetText(string text) {
		Instance?.SetText(text);
	}

	public static string? GetText() {
		return Instance?.GetText();
	}
}
