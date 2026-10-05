namespace Lens.services;

public interface IClipboard {
	void SetText(string text);
	string? GetText();
}

public static class Clipboard {
	public static volatile IClipboard? Instance;

	public static bool Available => Instance != null;

	public static void SetText(string text) {
		Instance?.SetText(text);
	}

	public static string? GetText() {
		return Instance?.GetText();
	}
}
