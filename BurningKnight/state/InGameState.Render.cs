using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.input;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.fx;
using BurningKnight.entity.item;
using BurningKnight.entity.item.use;
using BurningKnight.entity.room;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.paintings;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.ui;
using BurningKnight.ui.dialog;
using BurningKnight.ui.inventory;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.game;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.input;
using Lens.lightJson;
using Lens.util;
using Lens.util.camera;
using Lens.services;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Timer = Lens.util.timer.Timer;

namespace BurningKnight.state {
	public partial class InGameState {
		public override void RenderUi() {
			base.RenderUi();
			
			if (!Settings.HideUi) {
				if (Run.Depth == 0 || emeraldY > -20) {
					var y = Run.Depth == 0 ? 0 : emeraldY;
					var str = $"{GlobalSave.Emeralds}";
					var xx = Display.UiWidth - emerald.Width - 8;

					Graphics.Render(emerald, new Vector2(xx, 8 + y));
					Graphics.Print(str, Font.Small, new Vector2(xx - 8 - Font.Small.MeasureString(str).Width, 9 + y));
				}
			}

			if (blackBarsSize > 0.01f) {
				Graphics.Render(black, Vector2.Zero, 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, blackBarsSize));
				Graphics.Render(black, new Vector2(0, Display.UiHeight + 1 - blackBarsSize), 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, blackBarsSize + 1));
			}

			painting?.RenderUi();

			TopUi.Render();

			if (Settings.HideUi) {
				foreach (var e in TopUi.Tagged[Tags.Cursor]) {
					e.Render();
				}

				return;
			}

			if (Menu && offset <= Display.UiHeight) {
				Graphics.Render(black, new Vector2(0, offset - Display.UiHeight), 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, Display.UiHeight + 1));
				Graphics.Render(gardient, new Vector2(0, offset), 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, (Display.UiHeight + 1) / 90f));
				
				Graphics.Print("Press X", Font.Small, Display.Height + 48 + (int) offset);
				
				LogoRenderer.Render(offset);
			}

			var x = 1;
			
			if (Settings.ShowFps) {
				var c = Engine.Instance.AverageFramesPerSecond;

				var color = c switch
				{
					>= 55 => Color.Green,
					>= 45 => Color.Yellow,
					_ => Color.Red
				};

				Graphics.Color = color;
				var s = $"{c}";
				Graphics.Print(s, Font.Small, x, 1);
				x += (int) Font.Small.MeasureString(s).Width + 1;
				Graphics.Color = ColorUtils.WhiteColor;
			}

			if (Settings.SpeedrunTimer) {
				Graphics.Print(GetRunTime(), Font.Small, x, 1);
			}

			// Graphics.Batch.DrawString(Font.Test, "Test 你 Test", Vector2.One, Color.White);
			
			if (RenderDebug) {
				Ui?.RenderDebug();
				TopUi?.RenderDebug();
			}
		}
	}
}
