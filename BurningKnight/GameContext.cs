#nullable enable

using BurningKnight.level;
using BurningKnight.state;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.util.camera;

namespace BurningKnight {
	// The handful of things game code reaches for, behind one explicit accessor. It is an instance
	// so a state or a test can own and replace it; `GameContext.Current` is the one justified static
	// (the running game).
	//
	// A plain static, not AsyncLocal: there is one run per process, and the level generation worker
	// reads the same context its spawner does. If contexts ever need to differ per thread, AsyncLocal
	// is the mechanism — nothing needs that today.
	//
	// This is not a service locator: the members are the seams, and nothing else belongs here.
	// Every member falls back to the state, singleton or static it is retiring from until those
	// move in (R16/R17); reads already go through here, so the backing can change without touching
	// the call sites.
	public class GameContext {
		private static GameContext? current;

		// Created on first use, replaced by a state or a test when it has something better.
		public static GameContext Current {
			get => current ??= new GameContext();
			set => current = value;
		}

		private Area? area;
		private Camera? camera;

		public Area? Area {
			get => area ?? Engine.Instance.State?.Area;
			set => area = value;
		}

		public Camera? Camera {
			get => camera ?? Engine.Instance.State?.Camera;
			set => camera = value;
		}

		private Audio? audio;

		public Audio Audio {
			get => audio ??= Engine.Instance.Audio;
			set => audio = value;
		}

		// Still the run's static; WS-5 moves the backing in here.
		public Level? Level {
			get => Run.Level;
			set => Run.Level = value;
		}
	}

	// The reads: `Context.Camera`, `Context.Level`. Entities have their own `Context` property
	// (their world's context); this one is for the code that has no entity to ask.
	public static class Context {
		public static Area? Area => GameContext.Current.Area;
		public static Camera? Camera => GameContext.Current.Camera;
		public static Audio Audio => GameContext.Current.Audio;
		public static Level? Level {
			get => GameContext.Current.Level;
			set => GameContext.Current.Level = value;
		}
	}
}
