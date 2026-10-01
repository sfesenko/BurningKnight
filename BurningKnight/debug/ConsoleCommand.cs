namespace BurningKnight.debug {
	public abstract class ConsoleCommand {
		public enum Access {
			Everyone,
			Testers,
			Developers
		}

		public Access RunPermission = Access.Everyone;
		public string Name = null!;
		public string ShortName = null!;

		public abstract void Run(Console Console, string[] Args);

		public virtual string AutoComplete(string input) {
			return "";
		}
	}
}