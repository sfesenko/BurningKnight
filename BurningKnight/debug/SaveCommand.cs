using BurningKnight.save;
using Lens;
using Lens.util.file;

namespace BurningKnight.debug {
	public class SaveCommand : ConsoleCommand {
		public SaveCommand() {
			Name = "save";
			ShortName = "s";
		}
		
		public override void Run(Console console, string[] args) {
			if (args.Length == 0 || args.Length > 3) {
				console.Print("save [save path] (save type)");
				return;
			}

			var path = args[0];
			var saveType = args.Length == 1 ? "all" : args[1];
			var area = Context.Area;

			// Synchronous: a console command, and the savers serialise the live area.
			switch (saveType) {
				case "all": {
					SaveManager.Save(area!, SaveType.Level, false, path);
					SaveManager.Save(area!, SaveType.Player, false, path);
					SaveManager.Save(area!, SaveType.Game, false, path);
					SaveManager.Save(area!, SaveType.Global, false, path);
					break;
				}

				case "level": {
					SaveManager.Save(area!, SaveType.Level, false, path);
					break;
				}

				case "player": {
					SaveManager.Save(area!, SaveType.Player, false, path);
					break;
				}

				case "game": {
					SaveManager.Save(area!, SaveType.Game, false, path);
					break;
				}

				case "global": {
					SaveManager.Save(area!, SaveType.Global, false, path);
					break;
				}

				case "run": {
					SaveManager.Save(area!, SaveType.Level, false, path);
					SaveManager.Save(area!, SaveType.Player, false, path);
					SaveManager.Save(area!, SaveType.Game, false, path);
					break;
				}

				case "prefab": {
					SaveManager.Save(area!, SaveType.Level, false, $"{FileHandle.FromRoot("Prefabs/").FullPath}/{path}.lvl");
					break;
				}

				default: {
					console.Print($"Unknown save type {saveType}. Should be one of all, level, player, game, global, run, prefab");
					break;
				}
			}
				
			console.Print($"Done saving {saveType}");
		}
	}
}