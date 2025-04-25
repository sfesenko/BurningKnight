using System;
using System.IO;
using Lens.assets;

namespace Lens.util.file;

public class FileHandle(string path)
{
	public string FullPath => Path.GetFullPath(path);
	public string NameWithoutExtension => Path.GetFileNameWithoutExtension(path);
	public string Name => Path.GetFileName(path);
	public string Extension => Path.GetExtension(path);
	private string ParentName => Path.GetDirectoryName(path);
	public FileHandle Parent => new(ParentName);

	public static FileHandle FromRoot(string path) {
		return new FileHandle(Assets.Root + path);
	}
	
	public static FileHandle FromNearRoot(string path) {
		return new FileHandle(Assets.NearRoot + path);
	}

	public void MakeDirectory() {
		Directory.CreateDirectory(path);
	}

	public string ReadAll() {
		return File.ReadAllText(path);
	}

	public void Delete() {
		if (IsDirectory()) {
			Directory.Delete(path);
		} else {
			File.Delete(path);
		}
	}

	private string[] ListFiles() {
		return Directory.GetFiles(path);
	}

	private string[] ListDirectories() {
		return Directory.GetDirectories(path);
	}

	public FileHandle[] ListFileHandles() {
		return List(ListFiles());
	}

	public FileHandle[] ListDirectoryHandles() {
		return List(ListDirectories());
	}

	private static FileHandle[] List(string[] names) {
		var handles = new FileHandle[names.Length];

		for (int i = 0; i < names.Length; i++) {
			handles[i] = new FileHandle(names[i]);
		}

		return handles;
	}

	public bool Exists() {
		return IsDirectory() ? Directory.Exists(path) : File.Exists(path);
	}

	public bool IsDirectory() {
		try {
			return File.GetAttributes(path).HasFlag(FileAttributes.Directory);
		} catch (Exception) {
			return false;
		}
	}

	public override string ToString()
	{
		return path;
	}
}
