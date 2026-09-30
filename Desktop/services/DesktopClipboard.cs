using TextCopy;

namespace Desktop.services {
	public class DesktopClipboard : Lens.services.IClipboard {
		public void SetText(string text) {
			ClipboardService.SetText(text);
		}

		public string? GetText() {
			return ClipboardService.GetText();
		}
	}
}
