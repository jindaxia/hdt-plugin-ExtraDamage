using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using CoreAPI = Hearthstone_Deck_Tracker.API.Core;
using CardIds = HearthDb.CardIds;
using static HearthDb.Enums.GameTag;
using System.Windows;
using HearthDb;
using System;
using Hearthstone_Deck_Tracker.Utility.BoardDamage;

namespace ExtraDamage
{
	class Damage
	{
		private int _mana = 0;
		private HearthstoneTextBlock control = null;
		private List<string> directDamageCards = null;
		private Canvas _canvas = CoreAPI.OverlayCanvas;

		public Damage(HearthstoneTextBlock damageControl)
		{
			control = damageControl;
		}

		// Reset on when a new game starts
		internal void GameStart()
		{
			_mana = 0;
			//_list.Update(new List<Card>());
		}

		// Need to handle hiding the element when in the game menu
		internal void InMenu()
		{
			if (Config.Instance.HideInMenu)
			{
				control.Visibility = Visibility.Hidden;
			}
		}

		// Update the card list on player's turn
		internal void TurnStart(ActivePlayer player)
		{
			_mana = Core.Game.PlayerEntity.GetTag(GameTag.RESOURCES) + 1;

			if (player == ActivePlayer.Player)
			{
				//contains hero, minions ,weapon 
				var playerBoard = Filter(new List<Entity>(Core.Game.Player.Board));
				var weapon = GetWeapon(playerBoard);
				int heroAttackRemain = 0;
				int maxSingleAttackRate = 0;
				int minionsAttackRemain = 0;
				foreach (var card in playerBoard)
				{
					int attacked = card.GetTag(NUM_ATTACKS_THIS_TURN);
					if (card.IsHero)
					{
						heroAttackRemain = weapon != null ? GetAttackRate(weapon, true) - attacked : 1 - attacked;
					}
					else if (!card.IsWeapon)
					{
						var attackRate = GetAttackRate(card) - attacked;
						maxSingleAttackRate = attackRate > maxSingleAttackRate ? attackRate : maxSingleAttackRate;
						minionsAttackRemain += attackRate;
					}
				}
				List<CostDamage> costDamagePairs = ExtraDamageWithCost(heroAttackRemain, minionsAttackRemain, maxSingleAttackRate);
				var totalCost = costDamagePairs.Sum(x => x.Cost);
				var totalDamage = costDamagePairs.Sum(x => x.Damage);
				if (totalCost <= _mana)
				{
					UpdateDamage(totalDamage);
				}
				else
				{
					UpdateDamage(BestValue(costDamagePairs, _mana, costDamagePairs.Count));
					
					Hearthstone_Deck_Tracker.Utility.Logging.Log.Debug($"Mana: {_mana}");
					foreach (var c in costDamagePairs.Where(x => x.isPicked))
					{
						Hearthstone_Deck_Tracker.Utility.Logging.Log.Debug($"Picked Card: {c.Name}");
					}
					
				}
			}
		}

		private int BestValue(List<CostDamage> pairs, int costLimit, int itemLimit)
		{
			if (itemLimit > 1)
			{
				var lastValue = BestValue(pairs, costLimit, itemLimit - 1);
				if (pairs[itemLimit-1].Cost > costLimit)
				{
					return lastValue;
				}
				else
				{
					var value = BestValue(pairs, costLimit - pairs[itemLimit - 1].Cost, itemLimit - 1);
					if (value + pairs[itemLimit-1].Damage > lastValue)
					{
						pairs[itemLimit - 1].isPicked = true;
						return value + pairs[itemLimit - 1].Damage;
					}
					else
					{
						return lastValue;
					}
				}
			}
			else
			{
				if (pairs[0].Cost > costLimit)
				{
					return 0;
				}
				else
				{
					pairs[0].isPicked = true;
					return pairs[0].Damage;
				}
				
			}
		}

		private List<CostDamage> ExtraDamageWithCost(int hero, int minions, int maxSingleAttackRate)
		{
			List<CostDamage> result = new List<CostDamage>();
			var handCards = CoreAPI.Game.Player.Hand;
			foreach (var card in handCards)
			{
				if (card.IsSpell)
				{
					switch (card.CardId)
					{
						case CardIds.Collectible.Shaman.Bloodlust:
							result.Add(new CostDamage("Bloodlust", card.Cost, 3 * minions));
							break;
						case CardIds.Collectible.Shaman.RockbiterWeapon:
							result.Add(new CostDamage("RockbiterWeapon", card.Cost, 3 * Math.Max(hero, maxSingleAttackRate)));
							break;
						case CardIds.Collectible.Druid.SavageRoar:
							result.Add(new CostDamage("SavageRoar", card.Cost, 2 * (minions + hero)));
							break;
						case CardIds.Collectible.Warlock.PowerOverwhelming:
							result.Add(new CostDamage(card.Name, card.Cost, 4 * maxSingleAttackRate));
							break;
						case CardIds.Collectible.Mage.Fireball:
						case CardIds.Collectible.Mage.Frostbolt:
						case CardIds.Collectible.Mage.Flamestrike:
						case CardIds.Collectible.Mage.ForgottenTorch:
						case CardIds.NonCollectible.Mage.ForgottenTorch_RoaringTorchToken:
							result.Add(new CostDamage(card.Name, card.Cost, card.Attack));
							break;
						default:
							break;
					}
				}
				else if (card.IsMinion && card.GetTag(GameTag.CHARGE) == 1)
				{
					result.Add(new CostDamage(card.Name, card.Cost, card.Attack));
				}
				else if (card.IsMinion)
				{
					switch (card.CardId)
					{
						case CardIds.Collectible.Neutral.AbusiveSergeant:
						case CardIds.Collectible.Neutral.DarkIronDwarf:
							result.Add(new CostDamage(card.Name, card.Cost, 2 * maxSingleAttackRate));
							break;
						default:
							break;
					}
				}

			}
			return result;
		}

		private int GetAttackRate(Entity e, bool isWeapon = false)
		{
			if (!string.IsNullOrEmpty(e.CardId) && e.CardId == "GVG_111t")
			{
				return 4;
			}
			bool windfury = e.GetTag(WINDFURY) == 1;
			if (windfury)
			{
				if (isWeapon && e.GetTag(DURABILITY) - e.GetTag(DAMAGE) < 2)
				{
					return 1;
				}
				return 2;
			}
			return 1;
		}

		private Entity GetWeapon(List<Entity> list)
		{
			var weapons = list.Where(x => x.IsWeapon).ToList();
			return weapons.Count == 1 ? weapons[0] : list.FirstOrDefault(x => x.HasTag(JUST_PLAYED) && x.GetTag(JUST_PLAYED) == 1);
		}

		private List<Entity> Filter(List<Entity> cards)
			=>
				cards.Where(x => x != null && x.GetTag(CARDTYPE) != (int)CardType.PLAYER &&
							x.GetTag(CARDTYPE) != (int)CardType.ENCHANTMENT && x.GetTag(CARDTYPE) != (int)CardType.HERO_POWER
							&& x.GetTag(ZONE) != (int)Zone.SETASIDE && x.GetTag(ZONE) != (int)Zone.GRAVEYARD).ToList();

		private void UpdateDamage(int damage)
		{
			BoardState board = new BoardState();
			Hearthstone_Deck_Tracker.Utility.Logging.Log.Debug($"Board Damage: {board.Player.Damage}");
			if (board.Player.Damage + damage >= board.Opponent.Hero.Health)
			{
				control.Text = $"+{damage} Kill!!";
				control.FontSize = 30;
			}
			else
			{
				control.Text = $"+{damage}";
				control.FontSize = 20;
			}
			double fromLeft = Helper.GetScaledXPos(Config.Instance.AttackIconPlayerHorizontalPosition / 100, (int)CoreAPI.OverlayCanvas.Width, (4.0 / 3.0) / (CoreAPI.OverlayCanvas.Width / CoreAPI.OverlayCanvas.Height)) + 52;
			double fromTop = CoreAPI.OverlayCanvas.Height * Config.Instance.AttackIconPlayerVerticalPosition / 100 + 26;

			Canvas.SetLeft(control, fromLeft);
			Canvas.SetTop(control, fromTop);
			control.Visibility = Visibility.Visible;
		}

	}
}
