using BurningKnight.entity.events;
using BurningKnight.entity.item.use.parent;

namespace BurningKnight.entity.item.use {
	// The editor half: the item editor renders a use by its registered callback. A release build
	// excludes every *.Debug.cs (ADR-0003).
	public static partial class UseRegistry {
		static partial void RegisterDebugRenderers() {
			Register<SpawnBombUse>(SpawnBombUse.RenderDebug);
			Register<MeleeArcUse>(MeleeArcUse.RenderDebug);
			Register<ModifyShieldHeartsUse>(ModifyHpUse.RenderDebug);
			Register<ModifyHpUse>(ModifyHpUse.RenderDebug);
			Register<ModifyManaUse>(ModifyManaUse.RenderDebug);
			Register<ModifyMaxHpUse>(ModifyMaxHpUse.RenderDebug);
			Register<GiveHeartContainersUse>(ModifyHpUse.RenderDebug);
			Register<SimpleShootUse>(SimpleShootUse.RenderDebug);
			Register<ShootQueueUse>(ShootQueueUse.RenderDebug);
			Register<RandomUse>(RandomUse.RenderDebug);
			Register<GiveGoldUse>(GiveGoldUse.RenderDebug);
			Register<GiveKeyUse>(GiveKeyUse.RenderDebug);
			Register<GiveBombUse>(GiveBombUse.RenderDebug);
			Register<GiveItemUse>(GiveItemUse.RenderDebug);
			Register<SetMaxHpUse>(SetMaxHpUse.RenderDebug);
			Register<SpawnItemsUse>(SpawnItemsUse.RenderDebug);
			Register<SpawnMobsUse>(SpawnMobsUse.RenderDebug);
			Register<DiscoverSecretRoomsUse>(DiscoverSecretRoomsUse.RenderDebug);
			Register<ModifyStatUse>(ModifyStatUse.RenderDebug);
			Register<MakeProjectilesBounceUse>(MakeProjectilesBounceUse.RenderDebug);
			Register<MakeProjectilesHomeInUse>(MakeProjectilesHomeInUse.RenderDebug);
			Register<MakeProjectilesSlowDown>(MakeProjectilesSlowDown.RenderDebug);
			Register<MakeLayerPassableUse>(MakeLayerPassableUse.RenderDebug);
			Register<SpawnOrbitalUse>(SpawnOrbitalUse.RenderDebug);
			Register<SpawnPetUse>(SpawnPetUse.RenderDebug);
			Register<RerollItemsUse>(RerollItemsUse.RenderDebug);
			Register<RerollAndHideUse>(RerollItemsUse.RenderDebug);
			Register<SpawnProjectilesUse>(SpawnProjectilesUse.RenderDebug);
			Register<UseOnEventUse>(UseOnEventUse.RenderDebug);
			Register<SaleItemsUse>(SaleItemsUse.RenderDebug);
			Register<ModifyActiveChargeUse>(ModifyActiveChargeUse.RenderDebug);
			Register<PreventDamageUse>(PreventDamageUse.RenderDebug);
			Register<RemoveFromPoolUse>(RemoveFromPoolUse.RenderDebug);
			Register<ModifyGameSaveValueUse>(ModifyGameSaveValueUse.RenderDebug);
			Register<GiveWeaponUse>(GiveWeaponUse.RenderDebug);
			Register<GiveEmeraldsUse>(GiveEmeraldsUse.RenderDebug);
			Register<ModifyProjectilesUse>(ModifyProjectilesUse.RenderDebug);
			Register<TeleportUse>(TeleportUse.RenderDebug);
			Register<DoOnEnemyCollisionUse>(DoOnEnemyCollisionUse.RenderDebug);
			Register<DoOnHurtUse>(DoOnHurtUse.RenderDebug);
			Register<GiveBuffUse>(GiveBuffUse.RenderDebug);
			Register<GiveBuffImmunityUse>(GiveBuffImmunityUse.RenderDebug);
			Register<DoWithUse>(DoWithUse.RenderDebug);
			Register<SetKnockbackModifierUse>(SetKnockbackModifierUse.RenderDebug);
			Register<ScourgeUse>(ScourgeUse.RenderDebug);
			Register<ModifyLuckUse>(ModifyLuckUse.RenderDebug);
			Register<RerollItemsOnPlayerUse>(RerollItemsOnPlayerUse.RenderDebug);
			Register<ModifyBombsUse>(ModifyBombsUse.RenderDebug);
			Register<KillMobUse>(KillMobUse.RenderDebug);
			Register<SpawnDropUse>(SpawnDropUse.RenderDebug);
			Register<ModifyStatsUse>(ModifyStatsUse.RenderDebug);
			Register<EnableScourgeUse>(EnableScourgeUse.RenderDebug);
			Register<SetMusicSpeed>(SetMusicSpeed.RenderDebug);
			Register<DoOnTimerUse>(DoOnTimerUse.RenderDebug);
			Register<MakeBombsHomeUse>(MakeBombsHomeUse.RenderDebug);
			Register<RegenUse>(RegenUse.RenderDebug);
			Register<GivePhaseUse>(GivePhaseUse.RenderDebug);
			Register<AffectDealChanceUse>(AffectDealChanceUse.RenderDebug);
			Register<DoOnNewFloorUse>(DoUsesUse.RenderDebug);
			Register<DoUsesIfUse>(DoUsesIfUse.RenderDebug);
			Register<ModifyArcUse>(ModifyArcUse.RenderDebug);
			Register<MakeProjectileShrinkUse>(MakeProjectileShrinkUse.RenderDebug);
			Register<MakeProjectileExpandUse>(MakeProjectileExpandUse.RenderDebug);
			Register<ModifyManaMaxUse>(ModifyManaMaxUse.RenderDebug);
			Register<ModifyConsumableWeightsUse>(ModifyConsumableWeightsUse.RenderDebug);
			Register<InvokeItemsUse>(InvokeItemsUse.RenderDebug);
			Register<MakeProjectilesKillWithBuffUse>(MakeProjectilesKillWithBuffUse.RenderDebug);
			Register<ModifyShootUse>(ModifyShootUse.RenderDebug);
			Register<ModifyGenUse>(ModifyGenUse.RenderDebug);
			Register<BlockDamageUse>(BlockDamageUse.RenderDebug);
			Register<ModifyProjectileTextureUse>(ModifyProjectileTextureUse.RenderDebug);
			Register<BucketUse>(BucketUse.RenderDebug);
			Register<ChanceToUseWeaponUse>(ChanceToUseWeaponUse.RenderDebug);
		}
	}
}
