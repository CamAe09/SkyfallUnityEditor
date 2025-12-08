using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using TPSBR.UI;

namespace TPSBR
{
    public class ModernShopManager : UICloseView
    {
        [Header("Shop Data")]
        [SerializeField] private ShopDatabase _shopDatabase;
        
        [Header("UI References")]
        [SerializeField] private Transform _shopItemsContainer;
        [SerializeField] private GameObject _shopCardPrefab;
        [SerializeField] private TMPro.TextMeshProUGUI _coinsText;
        
        [Header("Settings")]
        [SerializeField] private string _coinsFormat = "{0}";
        
        private PlayerData _playerData;
        private List<ModernShopCard> _spawnedCards = new List<ModernShopCard>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            _playerData = Context.PlayerData;
            InitializeShop();
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
        }

        private void InitializeShop()
        {
            if (_shopDatabase == null)
            {
                Debug.LogError("ShopDatabase is not assigned!");
                return;
            }

            if (_shopCardPrefab == null)
            {
                Debug.LogError("Shop card prefab is not assigned!");
                return;
            }

            if (_shopItemsContainer == null)
            {
                Debug.LogError("Shop items container is not assigned!");
                return;
            }

            ClearShopItems();
            
            var sortedCharacters = _shopDatabase.characters
                .OrderByDescending(c => c.rarity)
                .ThenBy(c => c.price)
                .ToList();

            foreach (var character in sortedCharacters)
            {
                if (character == null) continue;
                
                GameObject cardObj = Instantiate(_shopCardPrefab, _shopItemsContainer);
                ModernShopCard card = cardObj.GetComponent<ModernShopCard>();
                
                if (card != null)
                {
                    card.Setup(character, _playerData, OnPurchaseClicked);
                    _spawnedCards.Add(card);
                }
            }

            UpdateCoinsDisplay();
        }

        private void ClearShopItems()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _spawnedCards.Clear();
        }

        private void OnPurchaseClicked(CharacterData character)
        {
            if (_playerData == null) return;

            bool isOwned = _playerData.ShopSystem.OwnsAgent(character.agentID);
            if (isOwned)
            {
                Debug.Log($"{character.displayName} is already owned!");
                return;
            }

            bool purchaseSuccessful = _playerData.ShopSystem.TryUnlockAgent(
                character.agentID, 
                character.price, 
                _playerData.CoinSystem
            );

            if (purchaseSuccessful)
            {
                Debug.Log($"Purchased {character.displayName} for {character.price} coins!");
                RefreshAllCards();
                UpdateCoinsDisplay();
            }
            else
            {
                int currentCoins = _playerData.CoinSystem.CloudCoins;
                Debug.Log($"Not enough coins! Need {character.price}, have {currentCoins}");
            }
        }

        private void RefreshAllCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null)
                {
                    card.Refresh(_playerData);
                }
            }
        }

        private void UpdateCoinsDisplay()
        {
            if (_coinsText != null && _playerData != null)
            {
                _coinsText.text = string.Format(_coinsFormat, _playerData.CoinSystem.CloudCoins);
            }
        }

        private void OnEnable()
        {
            if (_playerData != null)
            {
                RefreshAllCards();
                UpdateCoinsDisplay();
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshAllCards();
            UpdateCoinsDisplay();
        }
    }
}
