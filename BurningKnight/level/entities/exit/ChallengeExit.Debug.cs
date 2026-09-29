using BurningKnight.assets;
using BurningKnight.state;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.graphics;
using Lens.util.file;

namespace BurningKnight.level.entities.exit {
	// The editor half of ChallengeExit; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ChallengeExit {
    public override void RenderImDebug() {
	    base.RenderImDebug();
	    var v = (int) id;

	    if (ImGui.InputInt("Id", ref v)) {
		    id = (byte) v;
	    }
    }
	}
}
