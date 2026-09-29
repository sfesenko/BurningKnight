using BurningKnight.assets;
using BurningKnight.state;
using Lens;
using Lens.assets;
using Lens.graphics;
using Lens.util.file;

namespace BurningKnight.level.entities.exit {
	public partial class ChallengeExit : Exit {
		private byte id;
		
		protected override void Descend() {
			Run.ChallengeId = id;
    	Run.StartNew(1, RunType.Challenge);
    }

    protected override string GetFxText() {
    	return Locale.Get($"challenge_{id}");
    }

    public override void Load(FileReader stream) {
	    base.Load(stream);
	    id = stream.ReadByte();
    }

    public override void Save(FileWriter stream) {
	    base.Save(stream);
	    stream.WriteByte(id);
    }


    public override void Render() {
	    base.Render();

	    if (Engine.EditingLevel) {
		    Graphics.Print(id.ToString(), Font.Small, Position);
	    }
    }
	}
}