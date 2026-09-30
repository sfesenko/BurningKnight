using System;
using System.Collections.Generic;
using System.Numerics;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item;
using BurningKnight.entity.room;
using BurningKnight.level;
using BurningKnight.level.rooms;
using BurningKnight.state;
using BurningKnight.ui.imgui;
using ImGuiNET;
using Lens.entity;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.save.statistics {
	// The editor half of RunStatistics; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class RunStatistics {
		public new static void RenderDebug() {
			if (!WindowManager.RunInfo) {
				return;
			}
			
			ImGui.SetWindowPos(Vector2.Zero, ImGuiCond.Once);
			
			if (!ImGui.Begin("Run Info")) {
				return;
			}
			
			ImGui.Text($"Run Type: {GameContext.Current.Run.Type}");

			ImGui.Text($"World Time: {Weather.TimeOfDay}");
			ImGui.Text($"Night: {Weather.IsNight}");

			if (ImGui.Button("Set to day")) {
				Weather.Time = 7f;
			}

			ImGui.SameLine();
			
			if (ImGui.Button("Set to night")) {
				Weather.Time = 21f;
			}
			
			ImGui.Text($"Rain Left: {Weather.RainLeft}");
			ImGui.Text($"Rains: {Weather.Rains}");
			ImGui.Text($"Snows: {Weather.Snows}");

			if (Weather.Rains && ImGui.Button("End rain")) {
				Weather.RainLeft = 0;
			}
			
			if (Weather.Snows && ImGui.Button("End snow")) {
				Weather.RainLeft = 0;
			}

			if (!Weather.Rains && !Weather.Snows && ImGui.Button("Start rain or snow")) {
				Weather.RainLeft = 0;
			}

			GameContext.Current.Run.Statistics?.RenderWindow();

			ImGui.End();
		}
		public void RenderWindow() {
			ImGui.Separator();
			
			ImGui.Text($"Time: {Math.Floor(Context.Run.Time / 3600f)}h {Math.Floor(Context.Run.Time / 60f % 60f)}m {Math.Floor(Context.Run.Time % 60f)}s");
			ImGui.Text($"Won: {Won}");
			ImGui.Text($"Loop: {Context.Run.Loop}");
			ImGui.Text($"Max Depth: {MaxDepth}");
			ImGui.Text($"Game Version: {GameVersion}");
			Context.Run.CalculateScore();
			ImGui.Text($"Score: {Context.Run.Score}");
			ImGui.Separator();
			
			if (ImGui.TreeNode("Items")) {
				foreach (var item in Items) {
					ImGui.BulletText(item);
				}
				
				ImGui.TreePop();
			}
			
			if (ImGui.TreeNode("Banned items")) {
				foreach (var item in Banned) {
					ImGui.BulletText(item);
				}
				
				ImGui.TreePop();
			}

			ImGui.Separator();
			ImGui.Text($"Coins Collected: {CoinsObtained}");
			ImGui.Text($"Keys Collected: {KeysObtained}");
			ImGui.Text($"Bombs Collected: {BombsObtained}");
			ImGui.Text($"Hearts Collected: {HeartsCollected}");
			ImGui.Text($"Heart Containers Collected: {MaxHealth}");
			ImGui.Separator();
			
			
			ImGui.Text($"Damage Taken: {DamageTaken}");
			ImGui.Text($"Damage Dealt: {DamageDealt}");
			ImGui.Text($"Mobs Killed: {MobsKilled}");
			ImGui.Text($"Rooms Explored: {RoomsExplored} / {RoomsTotal}");
			ImGui.Text($"Secret Rooms Explored: {SecretRoomsFound} / {SecretRoomsTotal}");
			ImGui.Separator();

			ImGui.Text($"Date Started: {Day} {Year}");
			ImGui.Text($"Tiles Walked: {TilesWalked}");
			ImGui.Text($"Pits Fallen: {PitsFallen}");
			ImGui.Text($"Bosses Defeated: {BossesDefeated}");
			ImGui.Text($"Spikes Triggered: {SpikesTriggered}");
			ImGui.Text($"Paintings Broke: {GlobalSave.GetInt("paintings_destroyed")}");

			ImGui.Separator();
			ImGui.Text($"Luck: {Context.Run.Luck}");
			ImGui.Text($"Scourge: {Context.Run.Scourge}");

			if (ImGui.TreeNode("Scourges")) {
				foreach (var curse in Scourge.Defined) {
					var v = Scourge.IsEnabled(curse);

					if (ImGui.Checkbox(curse, ref v)) {
						if (v) {
							Scourge.Enable(curse);
						} else {
							Scourge.Disable(curse);
						}
					}
				}
				
				ImGui.TreePop();
			}
		}
	}
}
