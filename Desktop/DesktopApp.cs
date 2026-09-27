using System.Collections.Generic;
using BurningKnight;
using Desktop.core;
using Desktop.integration;
using Lens;
using Microsoft.Xna.Framework;

namespace Desktop {
	public class DesktopApp() : BK(BurningKnight.Display.Width * Scale, BurningKnight.Display.Height * Scale, !Version.Dev, () => new DesktopCore())
	{
		private const int Scale = 3;

		private List<Integration> integrations = [];

		protected override void Initialize() {
			base.Initialize();

			// Disable integrations for now

			// integrations.Add(new DiscordIntegration());
			// integrations.Add(new SteamIntegration());

			foreach (var i in integrations) {
				i.Init();
			}
			
			AssetsLoaded += () => {
				foreach (var i in integrations) {
					i.PostInit();
				}
			};
		}

		protected override void Destroy() {
			foreach (var i in integrations) {
				i.Destroy();
			}
			
			integrations.Clear();
			
			base.Destroy();
		}

		protected override void Update(GameTime gameTime) {
			base.Update(gameTime);
			
			foreach (var i in integrations) {
				i.Update(Delta);
			}
		}
	}
}
