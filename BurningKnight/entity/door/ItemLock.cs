using System.Collections.Generic;
using System.Linq;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.room;
using Lens.entity.component.logic;

namespace BurningKnight.entity.door {
	public class ItemLock : Lock {
		protected readonly List<Room> rooms = [];

		public void CalcRooms()
		{
			rooms.Clear();

			foreach (var room in Area.Tagged[Tags.Room].Where(room => room.Overlaps(this)))
			{
				rooms.Add((Room) room);
			}
		}

		protected override bool Disposable() {
			return false;
		}

		public override bool CanInteract() {
			return false;
		}

		private bool updatedRooms;

		public override void Update(float dt) {
			base.Update(dt);

			if (!updatedRooms || rooms.Count == 0) {
				updatedRooms = true;
				CalcRooms();
			}

			UpdateState();
		}

		protected virtual void UpdateState() {
			var shouldLock = false;

			foreach (var r in rooms)
			{
				if (r.Tagged[Tags.Player].Count == 0) {
					continue;
				}

				if (r.Tagged[Tags.Item].Any(item => item is ItemStand { Item: not null }))
				{
					shouldLock = true;
				}
			}

			if (shouldLock && !IsLocked) {
				SetLocked(true, null);
				GetComponent<StateComponent>().Become<ClosingState>();
			} else if (!shouldLock && IsLocked) {
				SetLocked(false, null);
				GetComponent<StateComponent>().Become<OpeningState>();
			}
		}
	}
}