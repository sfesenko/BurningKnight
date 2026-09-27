namespace Lens.services;

public interface ICloudSave {
	void Delete();
}

public static class CloudSave {
	public static ICloudSave? Instance;

	public static void Delete() {
		Instance?.Delete();
	}
}
