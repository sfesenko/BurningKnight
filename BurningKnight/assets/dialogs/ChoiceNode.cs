using System.Collections.Generic;
using BurningKnight.ui.dialog;
using Lens.assets;
using Lens.lightJson;

namespace BurningKnight.assets.dialogs {
	public partial class ChoiceNode : GraphNode, IDialogNode {
		private List<string> choices = new List<string>();
		private string label = "";
		private string name = "";

		public ChoiceNode() {
			AddInput();
		}

		public override string GetName() {
			return name;
		}

		public override void Save(JsonObject root) {
			base.Save(root);

			var i = 0;
			
			foreach (var c in choices) {
				Locale.Map[$"{LocaleId}_{i}"] = c;
				i++;
			}

			root["cc"] = choices.Count;
			Locale.Map[LocaleId] = name;
		}

		public override void Load(JsonObject root) {
			base.Load(root);

			for (var i = 0; i < root["cc"]; i++) {
				choices.Add($"{LocaleId}_{i}");
				AddOutput();
			}

			name = Locale.Get(LocaleId);
			label = name;
		}

		public Dialog Convert() {
			List<string[]> variants = null;
			
			if (Outputs.Count > 0) {
				variants = new List<string[]>();
				
				foreach (var o in Outputs) {
					if (o.ConnectedTo.Count > 0) {
						var list = new string[o.ConnectedTo.Count];

						for (var j = 0; j < o.ConnectedTo.Count; j++) {
							list[j] = o.ConnectedTo[j].Parent.LocaleId;
						}

						variants.Add(list);
					}
				}
			}
			
			return new ChoiceDialog(LocaleId, choices.ToArray(), variants);
		}
	}
}