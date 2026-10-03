namespace Lens.services;

public interface IStats {
	void Reset();
}

public static class Stats {
	public static volatile IStats? Instance;

	public static void Reset() {
		Instance?.Reset();
	}
}
