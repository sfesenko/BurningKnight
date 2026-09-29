using System;
using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.bomb;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.door;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using BurningKnight.entity.room.controllable;
using BurningKnight.entity.room.controllable.platform;
using BurningKnight.entity.room.controller;
using BurningKnight.entity.room.input;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.entities.chest;
using BurningKnight.level.rooms;
using BurningKnight.level.rooms.granny;
using BurningKnight.level.rooms.oldman;
using BurningKnight.level.tile;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.editor;
using BurningKnight.util;
using BurningKnight.util.geometry;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.camera;
using Lens.util.file;
using Lens.util.math;
using Lens.util.timer;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.entity.room {
	// The editor half of Room; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Room {
		public override void RenderImDebug() {
			var v = (int) Type;

			if (ImGui.Combo("Type", ref v, RoomRegistry.Names, RoomRegistry.Names.Length)) {
				Type = (RoomType) v;
			}

			if (Id == null) {
				Id = "";
			}
			
			ImGui.InputText("Id", ref Id, 128);
			ImGui.Separator();

			ImGui.InputInt("Map X", ref MapX);
			ImGui.InputInt("Map Y", ref MapY);
			ImGui.InputInt("Map W", ref MapW);
			ImGui.InputInt("Map H", ref MapH);

			if (Id == null) {
				Id = "";
			}
			
			ImGui.Text($"Doors: {Doors.Count}");

			X = MapX * 16 + 4;
			Y = MapY * 16 - 4;
			Width = MapW * 16 - 8;
			Height = MapH * 16 - 8;

			/*if (ImGui.Button("Sync")) {
				MapX = (int) Math.Floor(X / 16);
				MapY = (int) Math.Floor(Y / 16);
				MapW = (int) Math.Floor(Width / 16);
				MapH = (int) Math.Floor(Height / 16);
			}*/
		}
	}
}
