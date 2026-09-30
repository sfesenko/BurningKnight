#nullable enable

using System.Threading;
using BurningKnight.level;
using BurningKnight.state;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.util.camera;

namespace BurningKnight {
	// The handful of things game code reaches for, behind one explicit accessor. It is an instance
	// so a state or a test can own and replace it; `GameContext.Current` is the one justified static
	// (the running game), and it is ambient — a worker thread sees the context its spawner had.
	//
	// This is not a service locator: the members are the seams, and nothing else belongs here.
	// Area and camera fall back to the state and the singleton they are retiring from until those
	// move in (R16); reads already go through here, so the backing can change without touching
	// the call sites.
	public class GameContext {
		private static readonly AsyncLocal<GameContext> current = new();

		// Created on first use, replaced by a state or a test when it has something better.
		// AsyncLocal rather than a plain static so the value follows the work that was spawned
		// with it (the level generation thread).
		public static GameContext Current {
			get => current.Value ??= new GameContext();
			set => current.Value = value;
		}

		private Area? area;
		private Camera? camera;

		public Area? Area {
			get => area ?? Engine.Instance.State?.Area;
			set => area = value;
		}

		public Camera? Camera {
			get => camera ?? Lens.util.camera.Camera.Instance;
			set => camera = value;
		}

		public Audio Audio { get; set; } = Lens.assets.Audio.Instance;

		// Still the run's static; WS-5 moves it in here.
		public Level? Level => Run.Level;
	}

	// The reads: `Context.Camera`, `Context.Level`. Entities have their own `Context` property
	// (their world's context); this one is for the code that has no entity to ask.
	public static class Context {
		public static Area? Area => GameContext.Current.Area;
		public static Camera? Camera => GameContext.Current.Camera;
		public static Audio Audio => GameContext.Current.Audio;
		public static Level? Level => GameContext.Current.Level;
	}
}
