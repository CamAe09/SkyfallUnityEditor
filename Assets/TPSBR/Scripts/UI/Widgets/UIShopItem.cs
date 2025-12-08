using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
	public class UIShopItem : UIBehaviour
	{
		[SerializeField]
		private Image _agentIcon;
		[SerializeField]
		private TextMeshProUGUI _agentName;
		[SerializeField]
		private TextMeshProUGUI _costText;
		[SerializeField]
		private UIButton _purchaseButton;
		[SerializeField]
		private TextMeshProUGUI _purchaseButtonText;
		[SerializeField]
		private GameObject _ownedIndicator;
		[SerializeField]
		private string _costFormat = "{0} CloudCoins";
		[SerializeField]
		private string _purchaseText = "BUY";
		[SerializeField]
		private string _ownedText = "OWNED";
		[SerializeField]
		private string _selectedText = "SELECTED";

		private AgentSetup _agentSetup;
		private PlayerData _playerData;
		private Action<AgentSetup> _onPurchaseCallback;

		private void Awake()
		{
			if (_purchaseButton != null)
				_purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
		}

		private void OnDestroy()
		{
			if (_purchaseButton != null)
				_purchaseButton.onClick.RemoveListener(OnPurchaseButtonClicked);
		}

		public void SetData(AgentSetup agentSetup, PlayerData playerData, Action<AgentSetup> onPurchaseCallback)
		{
			_agentSetup = agentSetup;
			_playerData = playerData;
			_onPurchaseCallback = onPurchaseCallback;

			if (_agentIcon != null)
				_agentIcon.sprite = agentSetup.Icon;

			if (_agentName != null)
				_agentName.text = agentSetup.DisplayName;

			if (_costText != null)
				_costText.text = string.Format(_costFormat, agentSetup.CloudCoinCost);

			UpdateButtonState();
		}

		private void OnPurchaseButtonClicked()
		{
			if (_agentSetup == null || _playerData == null)
				return;

			bool isOwned = _playerData.ShopSystem.OwnsAgent(_agentSetup.ID);
			if (isOwned)
			{
				_playerData.AgentID = _agentSetup.ID;
				UpdateButtonState();
			}
			else
			{
				_onPurchaseCallback?.Invoke(_agentSetup);
				UpdateButtonState();
			}
		}

		private void UpdateButtonState()
		{
			if (_agentSetup == null || _playerData == null)
				return;

			bool isOwned = _playerData.ShopSystem.OwnsAgent(_agentSetup.ID);
			bool isSelected = _playerData.AgentID == _agentSetup.ID;
			bool canAfford = _playerData.CoinSystem.CanAfford(_agentSetup.CloudCoinCost);

			if (_ownedIndicator != null)
				_ownedIndicator.SetActive(isOwned);

			if (_purchaseButton != null)
			{
				_purchaseButton.interactable = isOwned || canAfford;
			}

			if (_purchaseButtonText != null)
			{
				if (isSelected)
					_purchaseButtonText.text = _selectedText;
				else if (isOwned)
					_purchaseButtonText.text = _ownedText;
				else
					_purchaseButtonText.text = _purchaseText;
			}
		}
	}
}
