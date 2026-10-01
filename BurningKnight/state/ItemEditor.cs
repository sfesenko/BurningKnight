using BurningKnight.ui.imgui;
using ImGuiNET;
using System;
using System.Collections.Immutable;
using BurningKnight.assets;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.item;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.item.use;
using BurningKnight.save;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.graphics;
using Lens.input;
using System.Text.Json.Nodes;
using Lens.util;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace BurningKnight.state {
	public static partial class ItemEditor {
		private static unsafe ImGuiTextFilterPtr filter = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
		private static unsafe ImGuiTextFilterPtr popupFilter = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
		private static Num.Vector2 size = new(300, 400);
		private static Num.Vector2 popupSize = new(400, 400);

		private static int id;
		private static int ud;
		private static string selectedUse = null!;
		private static string selectedRenderer = null!;
		public static ItemData? Selected;
		private static JsonNode? toAdd;
		
		public static readonly string[] Types = Enum.GetNames<ItemType>();
		private static readonly string[] WeaponTypes = Enum.GetNames<WeaponType>();
		private static readonly string[] Quality = Enum.GetNames<ItemQuality>();

		private static int toRemove = -1;

		private static bool toSort = true;

		public static void DrawItem(TextureRegion region) {
			ImGui.Image(ImGuiHelper.ItemsTexture, new Num.Vector2(region.Width * 3, region.Height * 3),
				new Num.Vector2(region.X / region!.Texture!.Width, region.Y / region.Texture.Height),
				new Num.Vector2((region.X + region.Width) / region.Texture.Width, 
					(region.Y + region.Height) / region.Texture.Height));
		}

		public static bool ForceFocus;

		private static string[] types = {
			"Room charged", "Auto charged", "Single use", "Infinite"
		};
		
		private static bool fromCurrent;
		private static int sortType;
		private static string itemName = "";
		private static int count;
		private static bool locked = true;
		private static int pools;
		private static int quality;
		private static int sortBy;
		private static bool single;
		private static bool invertSpawn;
		private static WeaponType weaponTypeSort;

		private static string[] sortTypes = {
			"None",
			"Type",
			"Lockable",
			"Pool",
			"Single spawn",
			"Quality",
			"Weapon Type"
		};
		
		private static void Sort() {
			// fixme
		}
	}
}