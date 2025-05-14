using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

	public string ReadAll() => 
		File.ReadAllText(path);

	public void Delete() {
		if (IsDirectory()) {
			Directory.Delete(path);
		} else {
			File.Delete(path);
		}
	}

	public IEnumerable<FileHandle> ListFileHandles() => 
		List(Directory.GetFiles(path));

	public IEnumerable<FileHandle> ListDirectoryHandles() {
		return List(Directory.GetDirectories(path));
	}

	private static IEnumerable<FileHandle> List(IEnumerable<string> names) => 
		names.Select(n => new FileHandle(n));

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

	public override string ToString() => 
		path;
}
