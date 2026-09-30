using System;
using BurningKnight.entity;
using BurningKnight.entity.creature.player;
using Microsoft.Xna.Framework;

namespace BurningKnight.debug {
	public class LevelCommand : ConsoleCommand {
		public LevelCommand() {
			_Init();
		}

		protected void _Init() {
			{
				Name = "lvl";
				ShortName = "l";
			}
		}

		public override void Run(Console Console, string[] Args) {
			if (Args.Length > 0) {
				Context.Run.Depth = Int32.Parse(Args[0]);
				Context.Run.ActualDepth = -10;

				if (Args.Length > 1) {
					Context.Run.Loop = Int32.Parse(Args[1]);
				}
			}
		}
	}
}