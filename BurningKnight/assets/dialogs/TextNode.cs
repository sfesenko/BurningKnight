using BurningKnight.ui.dialog;
using Lens.assets;
using System.Text.Json.Nodes;

namespace BurningKnight.assets.dialogs {
	public partial class TextNode : GraphNode, IDialogNode {
		private string name = "";
		private string label = "";
		
		public override void Load(JsonObject root) {
			base.Load(root);
			name = Locale.Get(LocaleId);
			label = name;
		}

		public override void Save(JsonObject root) {
			base.Save(root);
			name = label;
			Locale.Map[LocaleId] = name;
		}

		public override string GetName() {
			return name;
		}

		public virtual Dialog Convert() {
			string[]? variants = null;

			if (Outputs.Count == 1) {
				var t = Outputs[0].ConnectedTo;
				variants = new string[t.Count];
				var i = 0;
				
				foreach (var o in t) {
					variants[i] = o.Parent.LocaleId;
					i++;
				}
			}
			
			return CreateDialog(LocaleId, variants!);
		}

		protected virtual Dialog CreateDialog(string id, string[] variants) {
			return new Dialog(id, variants);
		}
	}
}