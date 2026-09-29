using Lens;
using Lens.core;
using Microsoft.Xna.Framework;

namespace BurningKnight.Tests;

// The engine with no game attached. `Initialize` brings up the GraphicsDevice, the SpriteBatch,
// the state renderer and input — everything the data loaders and generation need — while Update
// and Draw stay empty, so no game state has to exist. `RunOneFrame()` is the whole boot.
public class TestEngine : Engine {
	public TestEngine() : base("BurningKnight tests", BurningKnight.Display.Width,
		BurningKnight.Display.Height, false, () => new Core()) {
	}

	protected override void Update(GameTime gameTime) {
	}

	protected override void Draw(GameTime gameTime) {
	}
}
