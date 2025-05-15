using BurningKnight.assets.achievements;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.save;
using Lens.entity;
using Lens.graphics.animation;

namespace BurningKnight.entity.door {
	public class CageLock : Lock {
		
		private static readonly ColorMap Palette = ColorMap.New([
			(assets.Palette.Default[9], assets.Palette.Default[8]),
			(assets.Palette.Default[10], assets.Palette.Default[9]),
			(assets.Palette.Default[11], assets.Palette.Default[10])
		]);
		
		protected override ColorMap GetLockPalette() {
			return Palette;
		}

		private void SaveNpc() {
			var rooms = ((Door) GetComponent<OwnerComponent>().Owner).Rooms;

			foreach (var r in rooms) {
				if (r == null) {
					continue;
				}
				
				foreach (var n in r.Tagged[Tags.Npc]) {
					if (n is ShopNpc sn) {
						sn.Save();
					}
				}
			}
			
			Audio.PlaySfx("item_cage_key_used");
			CheckProgress();
		}

		public static void CheckProgress() {
			var progress = 0;
			var total = 0;
			
			foreach (var id in ShopNpc.AllNpc) {
				if (id == ShopNpc.TrashGoblin) {
					continue;
				}
				
				total++;
				
				if (GlobalSave.IsTrue(id)) {
					progress++;
				}
			}
			
			Achievements.SetProgress("bk:npc_party2", progress, total);
		}

		protected override bool TryToConsumeKey(Entity entity) {
			if (entity.TryGetComponent<ActiveWeaponComponent>(out var a) && a.Item is { Id: "bk:cage_key" }) {
				var i = a.Item;
				a.Set(null);
				i.Done = true;
				
				a.RequestSwap();
				SaveNpc();
				
				return true;
			}
			
			if (entity.TryGetComponent<WeaponComponent>(out var w) && w.Item is { Id: "bk:cage_key" }) {
				var i = w.Item;
				w.Set(null);
				i.Done = true;

				SaveNpc();
				
				return true;
			}
			
			return false;
		}
	}
}