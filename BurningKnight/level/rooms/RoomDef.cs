using System;
using System.Collections.Generic;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.room;
using BurningKnight.level.floors;
using BurningKnight.level.rooms.boss;
using BurningKnight.level.rooms.challenge;
using BurningKnight.level.rooms.connection;
using BurningKnight.level.rooms.darkmarket;
using BurningKnight.level.rooms.entrance;
using BurningKnight.level.rooms.granny;
using BurningKnight.level.rooms.oldman;
using BurningKnight.level.rooms.payed;
using BurningKnight.level.rooms.scourged;
using BurningKnight.level.rooms.secret;
using BurningKnight.level.rooms.shop;
using BurningKnight.level.rooms.shop.sub;
using BurningKnight.level.rooms.special;
using BurningKnight.level.rooms.spiked;
using BurningKnight.level.rooms.trap;
using BurningKnight.level.rooms.treasure;
using BurningKnight.level.tile;
using BurningKnight.level.walls;
using BurningKnight.state;
using BurningKnight.util;
using BurningKnight.util.geometry;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.rooms {
	public abstract partial class RoomDef : Rect {
		public enum Connection {
			All,
			Left,
			Right,
			Top,
			Bottom
		}

		public Dictionary<RoomDef, DoorPlaceholder> Connected = new Dictionary<RoomDef, DoorPlaceholder>();
		public int Id;
		public int Distance = -1;

		public List<RoomDef> Neighbours = new List<RoomDef>();
		private List<Dot> Busy = new List<Dot>();

		public virtual int GetMinWidth() {
			return 10;
		}

		public virtual int GetMinHeight() {
			return 10;
		}

		public virtual int GetMaxWidth() {
			return 16;
		}

		public virtual int GetMaxHeight() {
			return 16;
		}

		public abstract int GetMaxConnections(Connection Side);

		public abstract int GetMinConnections(Connection Side);

		public virtual void PaintFloor(Level level) {
			Painter.Fill(level, this, Tile.WallA);
			FloorRegistry.Paint(level, this, -1, Painter.AllGold);
		}
		
		public virtual void Paint(Level level) {
			WallRegistry.Paint(level, this);
		}

		public virtual void SetupDoors(Level level) {
			foreach (var door in Connected.Values) {
				door.Type = DoorPlaceholder.Variant.Regular;
			}
		}

		public int GetLastConnections(Connection Direction) {
			if (GetCurrentConnections(Connection.All) >= GetMaxConnections(Connection.All)) {
				return 0;
			}

			return GetMaxConnections(Direction) - GetCurrentConnections(Direction);
		}

		public virtual bool CanConnect(Connection direction) {
			var Cnt = GetLastConnections(direction);

			return Cnt > 0;
		}

		public virtual bool CanConnect(RoomDef r) {
			var I = Intersect(r);
			var FoundPoint = false;

			foreach (var P in I.GetPoints()) {
				if (CanConnect(r, P) && r.CanConnect(r, P)) {
					FoundPoint = true;

					break;
				}
			}

			if (!FoundPoint) {
				return false;
			}

			if (I.GetWidth() == 0 && I.Left == Left) {
				return CanConnect(Connection.Left) && r.CanConnect(Connection.Left);
			}

			if (I.GetHeight() == 0 && I.Top == Top) {
				return CanConnect(Connection.Top) && r.CanConnect(Connection.Top);
			}

			if (I.GetWidth() == 0 && I.Right == Right) {
				return CanConnect(Connection.Right) && r.CanConnect(Connection.Right);
			}

			if (I.GetHeight() == 0 && I.Bottom == Bottom) {
				return CanConnect(Connection.Bottom) && r.CanConnect(Connection.Bottom);
			}

			return false;
		}

		public bool ConnectWithRoom(RoomDef roomDef) {
			if ((Neighbours.Contains(roomDef) || ConnectTo(roomDef)) && !Connected.ContainsKey(roomDef) && CanConnect(roomDef)) {
				Connected[roomDef] = null;
				roomDef.Connected[this] = null;

				return true;
			}

			return false;
		}

		public Dot GetRandomCell() {
			return new Dot(Rnd.Int(Left + 1, Right), Rnd.Int(Top + 1, Bottom));
		}
		
		public Dot GetRandomCellWithWalls() {
			return new Dot(Rnd.Int(Left, Right + 1), Rnd.Int(Top, Bottom + 1));
		}

		public bool HasDoorsNear(int x, int y, int r) {
			foreach (var Door in Connected.Values) {
				var Dx = (Door.X - x);
				var Dy = (Door.Y - y);
				var D = (float) Math.Sqrt(Dx * Dx + Dy * Dy);

				if (D < r) {
					return true;
				}
			}

			return false;
		}
		
		protected virtual int ValidateWidth(int W) {
			return W;
		}

		protected virtual int ValidateHeight(int H) {
			return H;
		}

		protected bool SetSize(int MinW, int MaxW, int MinH, int MaxH) {
			if (MinW < GetMinWidth() || MaxW > GetMaxWidth() || MinH < GetMinHeight() || MaxH > GetMaxHeight() || MinW > MaxW || MinH > MaxH) {
				return false;
			}

			if (Quad()) {
				var V = Math.Min(ValidateWidth(Rnd.Int(MinW, MaxW) - 1), ValidateHeight(Rnd.Int(MinH, MaxH) - 1));
				Resize(V, V);
			} else {
				Resize(ValidateWidth(Rnd.Int(MinW, MaxW) - 1), ValidateHeight(Rnd.Int(MinH, MaxH) - 1));
			}


			return true;
		}

		protected virtual bool Quad() {
			return false;
		}

		public void ClearConnections() {
			foreach (var R in Neighbours) {
				R.Neighbours.Remove(this);
			}

			Neighbours.Clear();

			foreach (var R in Connected.Keys) {
				R.Connected.Remove(this);
			}

			Connected.Clear();
		}

		public bool CanPlaceWater(Dot P) {
			return Inside(P);
		}

		public bool CanPlaceGrass(Dot P) {
			return Inside(P);
		}

		public List<Dot> GetPassablePoints(Level level) {
			var Points = new List<Dot>();

			for (var I = Left + 1; I <= Right - 1; I++) {
				for (var J = Top + 1; J <= Bottom - 1; J++) {
					if (level.IsPassable(I, J)) {
						Points.Add(new Dot(I, J));
					}
				}
			}

			return Points;
		}

		public override int GetWidth() {
			return base.GetWidth() + 1;
		}

		public override int GetHeight() {
			return base.GetHeight() + 1;
		}

		public Dot GetCenter() {
			return new Dot(Left + GetWidth() / 2, Top + GetHeight() / 2);
		}

		public Dot GetTileCenter() {
			return new Dot(Left + (int) (GetWidth() / 2f), Top + (int) (GetHeight() / 2f));
		}
		
		public Vector2 GetCenterVector() {
			return new Vector2((Left + GetWidth() / 2f) * 16, (Top + GetHeight() / 2f) * 16);
		}
		
		public Rect GetCenterRect() {
			var x = (int) (Left + GetWidth() / 2f); 
			var y = (int) (Top + GetHeight() / 2f);
			return new Rect(x, y, x + 1, y + 1);
		}

		public virtual Rect GetConnectionSpace() {
			var C = GetDoorCenter();
			return new Rect(C.X, C.Y, C.X, C.Y);
		}

		public virtual float WeightMob(MobInfo info, SpawnChance chance) {
			return chance.Chance;
		}

		public virtual void ModifyMobList(List<MobInfo> infos) {
			
		}

		public virtual bool ShouldSpawnMobs() {
			return false;
		}

		public static RoomType DecideType(RoomDef r, Type room) {
			if (typeof(TrapRoom).IsAssignableFrom(room)) {
				return RoomType.Trap;
			}
			
			if (typeof(SubShopRoom).IsAssignableFrom(room)) {
				return RoomType.SubShop;
			}
			
			if (typeof(DarkMarketRoom).IsAssignableFrom(room)) {
				return RoomType.DarkMarket;
			}
			
			if (typeof(PayedRoom).IsAssignableFrom(room)) {
				return RoomType.Payed;
			}
			
			if (typeof(ScourgedRoom).IsAssignableFrom(room)) {
				return RoomType.Scourged;
			}
			
			if (typeof(ChallengeRoom).IsAssignableFrom(room)) {
				return RoomType.Challenge;
			}

			if (typeof(SpikedRoom).IsAssignableFrom(room)) {
				return RoomType.Spiked;
			}

			if (typeof(BossRoom).IsAssignableFrom(room)) {
				return RoomType.Boss;
			}
			
			if (typeof(GrannyRoom).IsAssignableFrom(room)) {
				return RoomType.Granny;
			}
			
			if (typeof(OldManRoom).IsAssignableFrom(room)) {
				return RoomType.OldMan;
			}
			
			if (typeof(EntranceRoom).IsAssignableFrom(room)) {
				return RoomType.Entrance;
			}
			
			if (typeof(ExitRoom).IsAssignableFrom(room)) {
				return RoomType.Exit;
			}
			
			if (typeof(SecretRoom).IsAssignableFrom(room)) {
				return RoomType.Secret;
			}
			
			if (typeof(TreasureRoom).IsAssignableFrom(room)) {
				return RoomType.Treasure;
			}
			
			if (typeof(ShopRoom).IsAssignableFrom(room)) {
				return RoomType.Shop;
			}
			
			if (typeof(SpecialRoom).IsAssignableFrom(room)) {
				return RoomType.Special;
			}
			
			if (typeof(ConnectionRoom).IsAssignableFrom(room)) {
				return RoomType.Connection;
			}
			
			return RoomType.Regular;
		}

		public virtual void ModifyRoom(Room room) {
			
		}

		public virtual bool ConvertToEntity() {
			return true;
		}

		public virtual float GetWeightModifier() {
			return 1f;
		}
	}
}