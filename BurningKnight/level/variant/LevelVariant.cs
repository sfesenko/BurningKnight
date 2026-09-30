namespace BurningKnight.level.variant {
	public class LevelVariant {
		public static readonly string Regular = "regular";
		public static readonly string Sand = "sand";
		public static readonly string Flooded = "flooded";
		public static readonly string Webbed = "webbed";
		public static readonly string Snow = "snow";
		public static string Chasm = "chasm";
		public static readonly string Gold = "gold";
		public static readonly string Forest = "forest";
		public static readonly string RaveCave = "rave_cave";
		
		private string id;

		public string Id => id;

		public LevelVariant(string id) {
			this.id = id;
		}

		public virtual void ModifyPainter(Painter painter) {
			
		}

		public virtual void PostInit(Level level) {
			
		}
	}
}