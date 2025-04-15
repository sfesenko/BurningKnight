using System.Collections.Generic;
using BurningKnight;
using Desktop.integration;
using Desktop.integration.twitch;
using Lens;
using Microsoft.Xna.Framework;

namespace Desktop {
	public class DesktopApp() : BK(Display.Width * Scale, Display.Height * Scale, !Version.Dev)
	{
		private const int Scale = 3;
		public static string In = "20sw479alxyc1";
		
		private List<Integration> integrations = [];
		private TwitchIntegration twitchIntegration;

		protected override void Initialize() {
			base.Initialize();

			// Disable integrations for now

			// integrations.Add(new DiscordIntegration());
			// integrations.Add(new SteamIntegration());
			// integrations.Add(twitchIntegration = new TwitchIntegration());

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

		public override void RenderUi() {
			base.RenderUi();
			twitchIntegration?.Render();
		}
	}
}
