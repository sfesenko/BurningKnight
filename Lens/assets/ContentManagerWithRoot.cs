using System;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace Lens.assets {
	// MonoGame's ContentManager opens asset names through TitleContainer, which rejects an
	// absolute RootDirectory ("TitleContainer.OpenStream requires a relative path") and knows
	// nothing about the content source, so the stream is opened here instead. That lets the engine
	// resolve content against the source the host supplies — including a packaged archive — rather
	// than against the process working directory.
	public class ContentManagerWithRoot(IServiceProvider services) : ContentManager(services) {
		protected override Stream OpenStream(string assetName) {
			var stream = Assets.Source.Open(assetName + ".xnb");

			if (stream == null) {
				throw new ContentLoadException($"The content file {assetName}.xnb was not found.");
			}

			return stream;
		}
	}
}
