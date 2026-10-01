using BurningKnight.ui.dialog;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.assets.dialogs {
	public partial class AnswerNode : DialogNode {
		private int type;
		
		protected override Dialog CreateDialog(string id, string[] variants) {
			return new AnswerDialog(id, (AnswerType) type, variants);
		}

		public override void Save(JsonObject root) {
			base.Save(root);
			root["atype"] = type;
		}

		public override void Load(JsonObject root) {
			base.Load(root);
			type = root["atype"].Int(0);
		}
	}
}