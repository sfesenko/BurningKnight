using BurningKnight.assets;
using BurningKnight.level.biome;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.graphics;
using Lens.util.file;

namespace BurningKnight.level.entities.exit {
	// The editor half of ShortcutExit; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ShortcutExit {
    public override void RenderImDebug() {
      base.RenderImDebug();
      var v = (int) id;

      if (ImGui.InputInt("To depth", ref v)) {
    	  id = (byte) v;
      }
    }
	}
}
