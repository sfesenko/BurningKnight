using System;
using BurningKnight.assets;
using BurningKnight.assets.items;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.controller;
using BurningKnight.assets.particle.custom;
using BurningKnight.assets.particle.renderer;
using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;
using Lens.util;
using Lens.util.camera;
using Lens.util.math;
using Lens.util.timer;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace BurningKnight.entity.item.use {
	// The editor half of SimpleShootUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SimpleShootUse {
		public static void RenderDebug(JsonValue root) {
			if (root.InputInt("Mana Usage", "mana", 0) > 0) {
				var b = root["mdr"].Int(0);

				if (ImGui.Combo("Place Mana", ref b, manaDropNames, manaDropNames.Length)) {
					root["mdr"] = b;
				}
			}

			if (ImGui.TreeNode("Stats")) {
				root.Checkbox("To Cursor", "cursor", false);
				root.Checkbox("To Cloest Target", "tomb", false);
				root.InputFloat("Damage", "damage");
				root.InputInt("Projectile Count", "amount");
				root.InputText("Sound", "sfx", "item_gun_fire");
				root.InputInt("Sound Prefix Number", "sfxn", 0);
				root.Checkbox("Reload Sound", "rsfx", false);
				root.Checkbox("Drop Shells", "shells", true);
				root.Checkbox("Uses Emeralds", "emeralds", false);

				var c = root.InputText("Color", "color");

				if (!string.IsNullOrEmpty(c) && !ProjectileColor.Colors.ContainsKey(c)) {
					ImGui.BulletText("Unknown color");
				}

				ImGui.Separator();

				root.InputFloat("Min Speed", "speed", 10);
				root.InputFloat("Max Speed", "speedm", 10);
				root.Checkbox("Disable Boost", "dsb", false);

				ImGui.Separator();
				
				root.InputFloat("Min Scale", "scale");
				root.InputFloat("Max Scale", "scalem");
				
				ImGui.Separator();

				var range = (float) root["range"].Number(0);

				if (ImGui.InputFloat("Range", ref range)) {
					root["range"] = range;
				}

				var knockback = (float) root["knockback"].Number(1);

				if (ImGui.InputFloat("Knockback", ref knockback)) {
					root["knockback"] = knockback;
				}

				root.InputFloat("Additional Angle", "ang", 0);
				var accuracy = (float) root["accuracy"].Number(0);

				if (ImGui.InputFloat("Accuracy", ref accuracy)) {
					root["accuracy"] = accuracy;
				}

				var light = root["light"].Bool(true);

				if (ImGui.Checkbox("Light", ref light)) {
					root["light"] = light;
				}

				var rect = root["rect"].Bool(false);

				if (ImGui.Checkbox("Rect body", ref rect)) {
					root["rect"] = rect;
				}

				var wait = root["wait"].Bool(false);

				if (ImGui.Checkbox("Wait for projectile death", ref wait)) {
					root["wait"] = wait;
				}

				var prefab = root["prefab"].String("");

				if (ImGui.InputText("Prefab", ref prefab, 128)) {
					root["prefab"] = prefab;
				}

				if (prefab.Length > 0 && ProjectileRegistry.Get(prefab) == null) {
					ImGui.BulletText("Unknown prefab");
				}

				var slice = root["texture"].String("rect");

				if (slice == "default") {
					slice = "rect";
				}
				
				var region = CommonAse.Projectiles.GetSlice(slice, false);

				if (region != null) {
					ImGui.Image(ImGuiHelper.ProjectilesTexture, new Num.Vector2(region.Width * 3, region.Height * 3),
						new Num.Vector2(region.X / region.Texture.Width, region.Y / region.Texture.Height),
						new Num.Vector2((region.X + region.Width) / region.Texture.Width,
							(region.Y + region.Height) / region.Texture.Height));
				}

				if (ImGui.InputText("Texture", ref slice, 128)) {
					root["texture"] = slice;
				}

				ImGui.TreePop();
			}

			if (ImGui.TreeNode("Modifiers")) {
				if (!root["modifiers"].IsJsonArray) {
					root["modifiers"] = new JsonArray();
				}

				ItemEditor.DisplayUse(root, root["modifiers"], "bk:ModifyProjectiles");
				ImGui.TreePop();
			}
		}
	}
}
