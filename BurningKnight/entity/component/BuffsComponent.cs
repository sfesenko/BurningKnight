using System;
using System.Collections.Generic;
using System.Linq;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.buff;
using BurningKnight.entity.events;
using BurningKnight.level;
using BurningKnight.level.tile;
using BurningKnight.state;
using Lens;
using Lens.entity;
using Lens.entity.component;
using Lens.util.file;

namespace BurningKnight.entity.component {
	public partial class BuffsComponent : SaveableComponent {
		public Dictionary<Type, Buff> Buffs = new Dictionary<Type, Buff>();
		private List<Type> immune = new List<Type>();
		
		public List<BuffParticle> Particles = new List<BuffParticle>();
		public bool IceImmunity;
		public bool PitImmunity;

		public void AddImmunity<T>() {
			var type = typeof(T);

			if (!immune.Contains(type)) {
				immune.Add(type);
			}
		}
		
		public Buff? Add(Buff buff) {
			if (buff == null) {
				return null;
			}
			
			var type = buff.GetType();
			
			if (Buffs.ContainsKey(type)) {
				return null;
			}
			
			foreach (var t in immune) {
				if (t == type) {
					return null;
				}
			}
			
			if (Send(new BuffCheckEvent {
					Entity = Entity,
					Buff = buff
			})) {
				return null;
			}

			Buffs[type] = buff;
			buff.Entity = Entity;
			buff.Init();
			
			Send(new BuffAddedEvent {
				Buff = buff
			});

			if (Engine.Instance.State is InGameState && buff.GetIcon() != null) {
				var part = new BuffParticle(buff, Entity);
				Particles.Add(part);
				Engine.Instance.State.Ui.Add(part);
			}

			return buff;
		}

		public Buff Add(string id) {
			return Add(BuffRegistry.Create(id));
		}

		public bool Has<T>() {
			return Buffs.ContainsKey(typeof(T));
		}

		public void Remove<T>() {
			Remove(typeof(T));
		}

		public void Remove(Type type) {
			if (Buffs.TryGetValue(type, out var buff)) {
				if (!Send(new BuffRemovedEvent {
					Buff = buff
				})) {
					buff.Destroy();
					Buffs.Remove(type);

					var toRemove = -1;

					for (var i = 0; i < Particles.Count; i++) {
						if (Particles[i].Buff == buff) {
							toRemove = i;
							Particles[i].Remove();
							break;
						}
					}

					if (toRemove != -1) {
						Particles.RemoveAt(toRemove);
					}
				}
			}
		}

		private bool addedIcons;
		
		public override void Update(float dt) {
			base.Update(dt);

			if (!addedIcons) {
				addedIcons = true;

				foreach (var b in Buffs.Values) {
					var part = new BuffParticle(b, Entity);
					Particles.Add(part);
					Engine.Instance.State.Ui.Add(part);
				}
			}

			foreach (var buff in Buffs.Values) {				
				buff.Update(dt);
			}

			foreach (var key in Buffs.Keys.ToList()) {
				var buff = Buffs[key];

				if (buff.TimeLeft <= 0) {
					Remove(key);
				}
			}
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			stream.WriteByte((byte) Buffs.Count);
			
			foreach (var buff in Buffs.Values) {
				stream.WriteString(buff.Type);
				stream.WriteFloat(buff.TimeLeft);
			}
		}

		public override void Load(FileReader reader) {
			base.Load(reader);
			var count = reader.ReadByte();

			for (int i = 0; i < count; i++) {
				var buff = Add(reader.ReadString()!);
				buff.TimeLeft = reader.ReadFloat();
				buff.Entity = Entity;
				buff.Init();
			}
		}

		public override bool HandleEvent(Event e) {
			foreach (var b in Buffs.Values) {
				b.HandleEvent(e);
			}
			
			if (e is TileCollisionStartEvent tileStart) {
				if (tileStart.Tile == Tile.Water) {
					Remove<BurningBuff>();
				}
			}/* else if (e is FlagCollisionStartEvent flagStart) {
				if (flagStart.Flag == Flag.Burning) {
					Add<BurningBuff>();
				}
			}*/
			
			return base.HandleEvent(e);
		}

#if DEBUG
		// The buff editor (BuffsComponent.Debug.cs) is Debug-only, and so is its buffer.
		private static string toAdd = "";
#endif
	}
}