using System;
using System.Collections.Generic;
using BurningKnight.assets;
using BurningKnight.assets.mod;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.item.use.parent;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item.use {
	public static partial class UseRegistry {
		public static Dictionary<string, Type> Uses = new Dictionary<string, Type>();
		public static Dictionary<string, Action<JsonValue>> Renderers = new Dictionary<string, Action<JsonValue>>();

		public static void Register<T>(Mod mod, Action<JsonValue> renderer = null) where T : ItemUse {
			var type = typeof(T);
			var name = type.Name;
			var id = $"{mod?.Prefix ?? Mods.BurningKnight}:{(name.EndsWith("Use") ? name.Substring(0, name.Length - 3) : name)}";

			Uses[id] = type;

			if (renderer != null) {
				Renderers[id] = renderer;
			}
		}

		private static void Register<T>(Action<JsonValue> renderer = null) where T : ItemUse {
			Register<T>(null, renderer);
		}

		public static ItemUse Create(string id) {
			if (!Uses.TryGetValue(id, out var use)) {
				return null;
			}

			return (ItemUse) Activator.CreateInstance(use);
		}

		static UseRegistry() {
			Register<DigUse>();
			Register<SpawnBombUse>();
			Register<ConsumeUse>();
			Register<MeleeArcUse>();
			Register<ModifyShieldHeartsUse>();
			Register<ModifyHpUse>();
			Register<ModifyManaUse>();
			Register<ModifyMaxHpUse>();
			Register<GiveHeartContainersUse>();
			Register<SimpleShootUse>();
			Register<ShootQueueUse>();
			Register<RandomUse>();
			Register<GiveGoldUse>();
			Register<GiveKeyUse>();
			Register<GiveBombUse>();
			Register<GiveItemUse>();
			Register<SetMaxHpUse>();
			Register<SpawnItemsUse>();
			Register<SpawnMobsUse>();
			Register<DiscoverSecretRoomsUse>();
			Register<ModifyStatUse>();
			Register<MakeProjectilesSplitOnDeathUse>();
			Register<MakeProjectilesShatternOnDeathUse>();
			Register<MakeProjectilesBounceUse>();
			Register<MakeProjectilesHomeInUse>();
			Register<MakeProjectilesSlowDown>();
			Register<TeleportToCursorUse>();
			Register<MakeLayerPassableUse>();
			Register<SpawnOrbitalUse>();
			Register<SpawnPetUse>();
			Register<RerollItemsUse>();
			Register<RerollAndHideUse>();
			Register<SpawnProjectilesUse>();
			Register<UseOnEventUse>();
			Register<MakeShopRestockUse>();
			Register<SaleItemsUse>();
			Register<ModifyActiveChargeUse>();
			Register<PreventDamageUse>();
			Register<RemoveFromPoolUse>();
			Register<ModifyGameSaveValueUse>();
			Register<GiveWeaponUse>();
			Register<AddHitboxUse>();
			Register<GiveEmeraldsUse>();
			Register<ModifyProjectilesUse>();
			Register<TeleportUse>();
			Register<DoOnEnemyCollisionUse>();
			Register<DoOnHurtUse>();
			Register<GiveBuffUse>();
			Register<GiveBuffImmunityUse>();
			Register<DoWithUse>();
			Register<TriggerHurtEventUse>();
			Register<SetKnockbackModifierUse>();
			Register<RandomActiveUse>();
			Register<ScourgeUse>();
			Register<ModifyLuckUse>();
			Register<RerollItemsOnPlayerUse>();
			Register<GoThonkUse>();
			Register<RevealMapUse>();
			Register<RevealMapUse>();
			Register<ModifyBombsUse>();
			Register<DuplicateItemsUse>();
			Register<DuplicateMobsUse>();
			Register<DuplicateMobsAndHealUse>();
			Register<GiveLaserAimUse>();
			Register<KillMobUse>();
			Register<GiveFlightUse>();
			Register<SpawnDropUse>();
			Register<ModifyStatsUse>();
			Register<MakeProjectilesBoomerangUse>();
			Register<EnableScourgeUse>();
			Register<SetMusicSpeed>();
			Register<DoOnTimerUse>();
			Register<MakeBombsHomeUse>();
			Register<MakeBombsExplodeOnTouchUse>();
			Register<RegenUse>();
			Register<ReplaceHeartsWithShieldsUse>();
			Register<GivePhaseUse>();
			Register<ExplodeUse>();
			Register<DiscoverSideRoomsUse>();
			Register<DetonateBombsUse>();
			Register<GiveScourgeImmunityUse>();
			Register<MakeBombsBlankUse>();
			Register<BlankUse>();
			Register<MakeProjectilesBlankOnDeathUse>();
			Register<AffectDealChanceUse>();
			Register<LeaveLegoUse>();
			Register<DoOnNewFloorUse>();
			Register<DoUsesIfUse>();
			Register<ModifyArcUse>();
			Register<BlindFoldUse>();
			Register<MakeProjectileShrinkUse>();
			Register<MakeProjectileExpandUse>();
			Register<SuperHotUse>();
			Register<ModifyManaMaxUse>();
			Register<ModifyConsumableWeightsUse>();
			Register<MakeProjectilesHurtOnMissUse>();
			Register<TeleportToShopUse>();
			Register<PlaceDecoyUse>();
			Register<InvokeItemsUse>();
			Register<SpeedUpOrbitalsUse>();
			Register<TeleportToPrevRoomUse>();
			Register<BreakPiggyBankUse>();
			Register<MakeProjectilesKillWithBuffUse>();
			Register<ModifyShootUse>();
			Register<ModifyGenUse>();
			Register<MakeProjectilesSplitUse>();
			Register<PoofUse>();
			Register<MakeProjectileReshootUse>();
			Register<PokemonUse>();
			Register<BlockDamageUse>();
			Register<ModifyProjectileTextureUse>();
			Register<ShootLaserUse>();
			Register<GiveRandomPickupUse>();
			Register<BucketUse>();
			Register<FireInAllDirsUse>();
			Register<MakeItemsAttactUse>();
			Register<MakeRollKickProjectilesUse>();
			Register<ChanceToUseWeaponUse>();
			Register<ModEachAttackUse>();

			RegisterDebugRenderers();
		}

		// Implemented in UseRegistry.Debug.cs, which a release build excludes (ADR-0003).
		static partial void RegisterDebugRenderers();
	}
}
