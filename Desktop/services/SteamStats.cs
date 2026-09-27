using Lens.services;
using Steamworks;

namespace Desktop.services {
	public class SteamStats : IStats {
		public void Reset() {
			SteamUserStats.ResetAll(true);
		}
	}
}
