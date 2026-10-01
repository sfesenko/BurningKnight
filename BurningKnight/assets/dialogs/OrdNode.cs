
using Lens.assets;
using System.Text.Json.Nodes;

namespace BurningKnight.assets.dialogs {
	public partial class OrdNode : GraphNode {
		private string header = "Title";
		private string optionA = "Option A";
		private string optionB = "Option B";
		private string answerA = "Answer A";
		private string answerB = "Answer B";

		public OrdNode() {
			AddInput();
			
			AddOutput();
			AddOutput();
		}

		public override void Save(JsonObject root) {
			base.Save(root);
			
			Locale.Map[LocaleId] = header;
			Locale.Map[$"{LocaleId}_a"] = optionA;
			Locale.Map[$"{LocaleId}_aa"] = answerA;
			Locale.Map[$"{LocaleId}_b"] = optionB;
			Locale.Map[$"{LocaleId}_ab"] = answerB;
		}

		public override void Load(JsonObject root) {
			base.Load(root);
			
			header = Locale.Get(LocaleId);
			optionA = Locale.Get($"{LocaleId}_a");
			answerA = Locale.Get($"{LocaleId}_aa");
			optionB = Locale.Get($"{LocaleId}_b");
			answerB = Locale.Get($"{LocaleId}_ab");
		}

		public override string GetName() {
			return header;
		}
	}
}