using System;
using BurningKnight.assets.achievements;
using BurningKnight.entity.events;
using BurningKnight.entity.item.util;
using BurningKnight.level.rooms;
using Lens.entity;
using Lens.entity.component;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.entity.component {
	public partial class StatsComponent : SaveableComponent {
		private float speed = 1;
		private float damage = 1;
		private float fireRate = 1;
		private float rangedRate = 1;
		private float accuracy = 1;
		private float range = 1;
		private float knockback = 1;

		public float DMChance;
		public float GrannyChance;
		public bool SawDeal;
		public bool TookDeal;
		public bool TookDamageInRoom;
		public bool TookDamageInLastRoom;
		public bool TookDamageOnLevel;
		public bool UsedWeaponInRoom;

		public int HeartsPayed;

		public float Speed {
			get => speed;
			set => speed = MathUtils.Clamp(0.02f, 3f, value);
		}
		
		public float Damage {
			get => damage;
			set => damage = MathUtils.Clamp(0.1f, 100f, value);
		}
		
		public float FireRate {
			get => fireRate;
			set => fireRate = MathUtils.Clamp(0.1f, 3f, value);
		}
		
		public float RangedRate {
			get => rangedRate;
			set => rangedRate = MathUtils.Clamp(0.1f, 3f, value);
		}
		
		public float Accuracy {
			get => accuracy;
			set => accuracy = MathUtils.Clamp(0.1f, 10f, value);
		}
		
		public float Range {
			get => range;
			set => range = MathUtils.Clamp(0.1f, 3f, value);
		}

		public float Knockback {
			get => knockback;
			set => knockback = MathUtils.Clamp(0f, 4f, value);
		}


		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			stream.WriteBoolean(SawDeal);
			stream.WriteBoolean(TookDeal);
			stream.WriteBoolean(TookDamageInRoom);
			stream.WriteBoolean(TookDamageOnLevel);
			stream.WriteInt32(HeartsPayed);
		}

		public override void Load(FileReader stream) {
			base.Load(stream);

			SawDeal = stream.ReadBoolean();
			TookDeal = stream.ReadBoolean();
			TookDamageInRoom = stream.ReadBoolean();
			TookDamageOnLevel = stream.ReadBoolean();
			HeartsPayed = stream.ReadInt32();
		}

		public override bool HandleEvent(Event e) {
			if (e is PlayerHurtEvent) {
				if (!TookDamageOnLevel) {
					DMChance -= 0.5f;
				}

				var rm = Entity.GetComponent<RoomComponent>()!.Room;
				
				if (rm != null && !TookDamageInRoom && rm.Type == RoomType.Boss) {
					DMChance -= 0.25f;
				}
				
				TookDamageInRoom = true;
				TookDamageOnLevel = true;
			} else if (e is RoomChangedEvent rce) {
				if (rce.JustDiscovered) {
					TookDamageInLastRoom = TookDamageInRoom;
					TookDamageInRoom = false;
					UsedWeaponInRoom = false;
				}
			} else if (e is NewLevelStartedEvent) {
				TookDamageOnLevel = false;
				TookDamageInRoom = false;
				TookDamageInLastRoom = false;

				GrannyChance = 0.5f;
				DMChance = 1f;
			} else if (e is ProjectileCreatedEvent pce) {
				var projectile = pce.Projectile;
				
				projectile.Damage *= Damage;

				// Can turn out to be a disaster
				if (projectile.T > 0) {
					projectile.T *= Range;
				}
			} else if (e is MeleeArc.CreatedEvent mace) {
				var arc = mace.Arc;

				arc.Damage *= Damage;
				arc.Width *= Math.Max(1, Range);
			} else if (e is RoomClearedEvent rr) {
				if (!UsedWeaponInRoom && rr.Room != null) {
					var t = rr.Room.Type;

					if (t == RoomType.Regular || t == RoomType.Challenge || t == RoomType.Boss) {
						Achievements.Unlock("bk:white_flag");
					}
				}
			}
			
			return base.HandleEvent(e);
		}
	}
}