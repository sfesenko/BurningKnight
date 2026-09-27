using System;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace Lens.assets {
	// MonoGame's ContentManager opens asset names through TitleContainer, which rejects an
	// absolute RootDirectory ("TitleContainer.OpenStream requires a relative path"), so the
	// stream is opened here directly instead. That lets the engine resolve content against the
	// root the host supplies rather than against the process working directory.
	public class ContentManagerWithRoot(IServiceProvider services) : ContentManager(services) {
		protected override Stream OpenStream(string assetName) {
			try {
				return File.OpenRead(Path.Combine(RootDirectory, assetName) + ".xnb");
			} catch (FileNotFoundException e) {
				throw new ContentLoadException("The content file was not found.", e);
			} catch (DirectoryNotFoundException e) {
				throw new ContentLoadException("The directory was not found.", e);
			}
		}
	}
}
