using System;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker;
using CoreAPI = Hearthstone_Deck_Tracker.API.Core;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;

namespace ExtraDamage
{
	public class ExtraDamagePlugin : IPlugin
	{
		public string Author
		{
			get { return "andburn"; }
		}

		public string ButtonText
		{
			get { return "Settings"; }
		}

		public string Description
		{
			get { return "A simple example plugin showing the oppoents class cards on curve."; }
		}

		public MenuItem MenuItem
		{
			get { return null; }
		}

		public string Name
		{
			get { return "ExtraDamage"; }
		}

		public void OnButtonPress()
		{
		}

		private HearthstoneTextBlock damageControl = null;

		public void OnLoad()
		{
			damageControl = new HearthstoneTextBlock();
			CoreAPI.OverlayCanvas.Children.Add(damageControl);

			double fromLeft = Helper.GetScaledXPos(Config.Instance.AttackIconPlayerHorizontalPosition / 100, (int)CoreAPI.OverlayCanvas.Width, (4.0 / 3.0) / (CoreAPI.OverlayCanvas.Width / CoreAPI.OverlayCanvas.Height)) + 52;
			double fromTop = CoreAPI.OverlayCanvas.Height * Config.Instance.AttackIconPlayerVerticalPosition / 100 + 26;

			Canvas.SetLeft(damageControl, fromLeft);
			Canvas.SetTop(damageControl, fromTop);
			Damage damage = new Damage(damageControl);

			//GameEvents.OnGameStart.Add(curvy.GameStart);
			GameEvents.OnInMenu.Add(damage.InMenu);
			GameEvents.OnTurnStart.Add(damage.TurnStart);
		}

		public void OnUnload()
		{
			CoreAPI.OverlayCanvas.Children.Remove(damageControl);
		}

		public void OnUpdate()
		{

		}

		public Version Version
		{
			get { return new Version(0, 1, 1); }
		}
	}
}