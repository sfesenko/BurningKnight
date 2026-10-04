using System;
using System.IO;

namespace AndroidPort.core;

// A readable, seekable window over a larger file: the Content.zip bytes inside
// base.apk. ZipArchive seeks constantly (central directory at the end, then back
// and forth per entry), so the forward-only asset stream cannot serve it — but
// the asset is stored uncompressed, which makes its APK bytes directly readable.
public sealed class ApkRegionStream : Stream {
	private readonly Stream baseStream;
	private readonly long start;
	private readonly long length;
	private long position;
	private bool disposed;

	public ApkRegionStream(Stream baseStream, long start, long length) {
		this.baseStream = baseStream;
		this.start = start;
		this.length = length;
	}

	public override bool CanRead => !disposed;
	public override bool CanSeek => !disposed;
	public override bool CanWrite => false;
	public override long Length => length;

	public override long Position {
		get => position;
		set => Seek(value, SeekOrigin.Begin);
	}

	public override long Seek(long offset, SeekOrigin origin) {
		var target = origin switch {
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => position + offset,
			SeekOrigin.End => length + offset,
			_ => throw new ArgumentOutOfRangeException(nameof(origin))
		};

		if (target < 0 || target > length) {
			throw new IOException($"Seek out of range: {target} (length {length})");
		}

		position = target;
		baseStream.Position = start + position;

		return position;
	}

	public override int Read(byte[] buffer, int offset, int count) {
		if (disposed) {
			throw new ObjectDisposedException(nameof(ApkRegionStream));
		}

		var remaining = length - position;

		if (remaining <= 0) {
			return 0;
		}

		if (count > remaining) {
			count = (int) remaining;
		}

		// ZipArchive reads sequentially; skip the seek when already positioned.
		var want = start + position;

		if (baseStream.Position != want) {
			baseStream.Position = want;
		}

		var read = baseStream.Read(buffer, offset, count);
		position += read;

		return read;
	}

	public override void Flush() {
	}

	public override void SetLength(long value) {
		throw new NotSupportedException();
	}

	public override void Write(byte[] buffer, int offset, int count) {
		throw new NotSupportedException();
	}

	protected override void Dispose(bool disposing) {
		if (!disposed && disposing) {
			disposed = true;
			baseStream.Dispose();
		}

		base.Dispose(disposing);
	}
}
