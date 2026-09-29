using BurningKnight.assets.items;
using BurningKnight.entity.events;
using ImGuiNET;
using Lens.entity;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.entity.item.stand {
	// The editor half of PermanentStand; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class PermanentStand {
		public override void RenderImDebug() {
			if (ImGui.InputText("Item", ref debugItem, 128, ImGuiInputTextFlags.EnterReturnsTrue)) {
				if (Item != null) {
					Item.Done = true;
				}

				SetItem(Items.CreateAndAdd(debugItem, Area), null);
				SavedItem = debugItem;
			}
		}
	}
}
