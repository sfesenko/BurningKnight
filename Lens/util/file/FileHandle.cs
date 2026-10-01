using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lens.assets;

namespace Lens.util.file;

public class FileHandle {
	private readonly string path;
	private readonly string? relative;

	public FileHandle(string path) {
		this.path = path;
	}

	private FileHandle(string path, string relative) {
		this.path = path;
		this.relative = relative;
	}

	// The real path under the content root. Writes and native loaders use it; reads do not, so a
	// generated or archived file is still found when this path does not exist.
	public string FullPath => Path.GetFullPath(path);
	public string NameWithoutExtension => Path.GetFileNameWithoutExtension(path);
	public string Name => Path.GetFileName(path);
	public string Extension => Path.GetExtension(path);
	private string? ParentName => Path.GetDirectoryName(path);
	public FileHandle Parent => new(ParentName!);

	public static FileHandle FromRoot(string path) {
		return new FileHandle(Assets.Root + path, path);
	}

	public void MakeDirectory() {
		Directory.CreateDirectory(path);
	}

	public string ReadAll() {
		using var stream = OpenRead();
		using var reader = new StreamReader(stream!);

		return reader.ReadToEnd();
	}

	public Stream? OpenRead() {
		return relative == null ? File.OpenRead(path) : Assets.Source.Open(relative);
	}

	public void Delete() {
		if (IsDirectory()) {
			Directory.Delete(path);
		} else {
			File.Delete(path);
		}
	}

	public IEnumerable<FileHandle> ListFileHandles() {
		return List(false);
	}

	public IEnumerable<FileHandle> ListDirectoryHandles() {
		return List(true);
	}

	private IEnumerable<FileHandle> List(bool directories) {
		if (relative == null) {
			var names = directories ? Directory.GetDirectories(path) : Directory.GetFiles(path);

			return names.Select(name => new FileHandle(name));
		}

		var prefix = relative.Length > 0 && !relative.EndsWith('/') ? relative + "/" : relative;

		return Assets.Source.List(relative)
			.Where(name => name.EndsWith('/') == directories)
			.Select(name => FromRoot(prefix + name.TrimEnd('/')));
	}

	public bool Exists() {
		return relative == null
			? IsDirectory() ? Directory.Exists(path) : File.Exists(path)
			: Assets.Source.Exists(relative);
	}

	public bool IsDirectory() {
		if (relative != null) {
			return Assets.Source.List(relative).Any();
		}

		try {
			return File.GetAttributes(path).HasFlag(FileAttributes.Directory);
		} catch (Exception) {
			return false;
		}
	}

	public override string ToString() => 
		path;
}
