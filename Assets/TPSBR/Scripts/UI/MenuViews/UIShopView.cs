using System;
using TMPro;
using UnityEngine;

namespace TPSBR.UI
{
	public class UIShopView : UICloseView
	{
		[SerializeField]
		private UIList _shopItemsList;
		[SerializeField]
		private TextMeshProUGUI _cloudCoinsText;
		[SerializeField]
		private string _cloudCoinsFormat = "CloudCoins: {0}";
		[SerializeField]
		private AudioSetup _purchaseSound;
		[SerializeField]
		private AudioSetup _insufficientFundsSound;

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_shopItemsList.UpdateContent += OnListUpdateContent;
		}

		private void OnListUpdateContent(int index, MonoBehaviour content)
		{
			var shopItem = content as UIShopItem;
			var setup = Context.Settings.Agent.Agents[index];

			shopItem.SetData(setup, Context.PlayerData, OnPurchaseClicked);
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			_shopItemsList.Refresh(Context.Settings.Agent.Agents.Length, false);
			UpdateCloudCoinsDisplay();

			Context.PlayerData.CoinSystem.OnCloudCoinsChanged += OnCloudCoinsChanged;
		}

		protected override void OnClose()
		{
			Context.PlayerData.CoinSystem.OnCloudCoinsChanged -= OnCloudCoinsChanged;

			base.OnClose();
		}

		protected override void OnDeinitialize()
		{
			_shopItemsList.UpdateContent -= OnListUpdateContent;

			base.OnDeinitialize();
		}

		private void OnPurchaseClicked(AgentSetup agentSetup)
		{
			bool success = Context.PlayerData.ShopSystem.TryUnlockAgent(
				agentSetup.ID,
				agentSetup.CloudCoinCost,
				Context.PlayerData.CoinSystem
			);

			if (success)
			{
				PlaySound(_purchaseSound);
				_shopItemsList.Refresh(Context.Settings.Agent.Agents.Length, false);
				UpdateCloudCoinsDisplay();
			}
			else
			{
				PlaySound(_insufficientFundsSound);
			}
		}

		private void OnCloudCoinsChanged(int newAmount)
		{
			UpdateCloudCoinsDisplay();
		}

		private void UpdateCloudCoinsDisplay()
		{
			if (_cloudCoinsText != null)
			{
				_cloudCoinsText.text = string.Format(_cloudCoinsFormat, Context.PlayerData.CoinSystem.CloudCoins);
			}
		}
	}
}
