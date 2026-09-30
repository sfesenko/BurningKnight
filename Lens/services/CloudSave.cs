#nullable enable

namespace Lens.services;

public interface ICloudSave {
	void Load();
	void Save();
	void Delete();
}

public static class CloudSave {
	public static ICloudSave? Instance;

	public static void Load() {
		Instance?.Load();
	}

	public static void Save() {
		Instance?.Save();
	}

	public static void Delete() {
		Instance?.Delete();
	}
}
