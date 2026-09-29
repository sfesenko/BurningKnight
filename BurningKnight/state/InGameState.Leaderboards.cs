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
		public void ShowLeaderboard(string board, string name) {
			if (busy) {
				return;
			}

			busy = true;
			animating = true;

			if (loading != null) {
				loading.Hide = false;
				leaderStats.Clear();
			}

			if (choice != null) {
				choice.Option = 0;
			}

			if (name == "high_score") {
				name = "regular";
			} else if (name.StartsWith("daily")) {
				name = "daily";
			}
			
			boardType.Label = $"{Locale.Get($"run_{name}")} {Locale.Get("leaderboard")}";

			leaderMenu.Enabled = true;
			currentBack = leaderBack;
			leaderMenu.Y = Display.UiHeight;
			
			Tween.To(0, leaderMenu.Y, x => leaderMenu.Y = x, 1f, Ease.BackOut).OnEnd = () => {
				SelectFirst();
				d?.Invoke(board);
				animating = false;
			};
		}
	}
}
