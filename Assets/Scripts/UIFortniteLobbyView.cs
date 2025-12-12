using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections.Generic;
using System.Linq;

namespace TPSBR.UI
{
    public class UIFortniteLobbyView : UIView
    {
        [Header("Top Navigation Buttons")]
        [SerializeField] private UIButton _shopButton;
        [SerializeField] private UIButton _questButton;
        [SerializeField] private UIButton _lockerButton;
        [SerializeField] private UIButton _battlePassButton;
        [SerializeField] private UIButton _settingsButton;
        
        [Header("Main Action Buttons")]
        [SerializeField] private UIButton _playButton;
        [SerializeField] private TextMeshProUGUI _playButtonText;
        
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _levelProgressBar;
        
        [Header("Quick Play Settings")]
        [SerializeField] private float _searchTimeout = 5f;
        [SerializeField] private EGameplayType _gameplayType = EGameplayType.BattleRoyale;
        [SerializeField] private int _maxPlayers = 100;
        [SerializeField] private string _defaultMapScenePath = "TPSBR/Scenes/Game";
        
        private bool _isSearchingForGame;
        private float _searchStartTime;
        private List<SessionInfo> _availableSessions = new List<SessionInfo>();
        private UIMatchmakerView _matchmakerView;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayButtonClicked);
            
            if (_shopButton != null)
                _shopButton.onClick.AddListener(OnShopButtonClicked);
            
            if (_questButton != null)
                _questButton.onClick.AddListener(OnQuestButtonClicked);
            
            if (_lockerButton != null)
                _lockerButton.onClick.AddListener(OnLockerButtonClicked);
            
            if (_battlePassButton != null)
                _battlePassButton.onClick.AddListener(OnBattlePassButtonClicked);
            
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }
        
        protected override void OnDeinitialize()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            
            if (_shopButton != null)
                _shopButton.onClick.RemoveListener(OnShopButtonClicked);
            
            if (_questButton != null)
                _questButton.onClick.RemoveListener(OnQuestButtonClicked);
            
            if (_lockerButton != null)
                _lockerButton.onClick.RemoveListener(OnLockerButtonClicked);
            
            if (_battlePassButton != null)
                _battlePassButton.onClick.RemoveListener(OnBattlePassButtonClicked);
            
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            
            base.OnDeinitialize();
        }
        
        protected override void OnOpen()
        {
            base.OnOpen();
            
            UpdatePlayerInfo();
            
            if (Context.PlayerPreview != null && Context.PlayerData != null)
            {
                Context.PlayerPreview.ShowAgent(Context.PlayerData.AgentID);
                Context.PlayerPreview.ShowOutline(false);
            }
            
            Context.Matchmaking.SessionListUpdated += OnSessionListUpdated;
            Context.Matchmaking.LobbyJoined += OnLobbyJoined;
            Context.Matchmaking.LobbyJoinFailed += OnLobbyJoinFailed;
            
            Context.Matchmaking.JoinLobby(false);
        }
        
        protected override void OnClose()
        {
            Context.Matchmaking.SessionListUpdated -= OnSessionListUpdated;
            Context.Matchmaking.LobbyJoined -= OnLobbyJoined;
            Context.Matchmaking.LobbyJoinFailed -= OnLobbyJoinFailed;
            
            base.OnClose();
        }
        
        protected override void OnTick()
        {
            base.OnTick();
            
            if (_isSearchingForGame)
            {
                float elapsedTime = Time.realtimeSinceStartup - _searchStartTime;
                
                if (elapsedTime >= _searchTimeout)
                {
                    _isSearchingForGame = false;
                    ShowCreateGameUI();
                }
            }
        }
        
        private void UpdatePlayerInfo()
        {
            if (_playerNameText != null)
            {
                _playerNameText.text = Context.PlayerData.Nickname;
            }
        }
        
        private void OnPlayButtonClicked()
        {
            Debug.Log("[UIFortniteLobbyView] Play button clicked - Starting Quick Play");
            StartQuickPlay();
        }
        
        private void StartQuickPlay()
        {
            _availableSessions.Clear();
            _isSearchingForGame = true;
            _searchStartTime = Time.realtimeSinceStartup;
            
            if (_playButtonText != null)
            {
                _playButtonText.text = "SEARCHING...";
            }
            
            Debug.Log("[UIFortniteLobbyView] Starting quick play search");
        }
        
        private void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (!_isSearchingForGame)
                return;
            
            _availableSessions.Clear();
            
            foreach (var session in sessionList)
            {
                if (session.IsValid == false || session.IsOpen == false || session.IsVisible == false)
                    continue;
                
                if (session.PlayerCount >= session.MaxPlayers)
                    continue;
                
                if (session.HasMap() == false)
                    continue;
                
                var sessionGameplayType = session.GetGameplayType();
                if (sessionGameplayType != _gameplayType)
                    continue;
                
                _availableSessions.Add(session);
            }
            
            if (_availableSessions.Count > 0)
            {
                JoinBestSession();
            }
        }
        
        private void JoinBestSession()
        {
            var bestSession = _availableSessions
                .OrderByDescending(s => s.PlayerCount)
                .ThenBy(s => s.MaxPlayers - s.PlayerCount)
                .First();
            
            Debug.Log($"[UIFortniteLobbyView] Found session: {bestSession.GetDisplayName()} - Joining...");
            
            if (_playButtonText != null)
            {
                _playButtonText.text = "JOINING...";
            }
            
            _isSearchingForGame = false;
            Context.Matchmaking.JoinSession(bestSession);
        }
        
        private void ShowCreateGameUI()
        {
            Debug.Log("[UIFortniteLobbyView] No sessions found - Opening Create Game UI");
            
            if (_playButtonText != null)
            {
                _playButtonText.text = "PLAY";
            }
            
            Open<UICreateSessionView>();
        }
        
        private void OnLobbyJoined()
        {
            Debug.Log("[UIFortniteLobbyView] Joined lobby successfully");
        }
        
        private void OnLobbyJoinFailed(string region)
        {
            Debug.LogWarning($"[UIFortniteLobbyView] Failed to join lobby in region: {region}");
            
            if (_playButtonText != null)
            {
                _playButtonText.text = "PLAY";
            }
        }
        
        private void OnShopButtonClicked()
        {
            Debug.Log("[UIFortniteLobbyView] Shop button clicked - Opening Modern Shop");
            Open<ModernShopManager>();
        }
        
        private void OnQuestButtonClicked()
        {
            Debug.Log("[UIFortniteLobbyView] Quest button clicked - Opening Quests");
            var questView = Open<UIQuestView>();
            if (questView != null)
            {
                questView.BackView = this;
            }
        }
        
        private void OnLockerButtonClicked()
        {
            Debug.Log("[UIFortniteLobbyView] Locker button clicked");
            Open<UIAgentSelectionView>();
        }
        
        private void OnBattlePassButtonClicked()
        {
            Debug.Log("[UIFortniteLobbyView] Battle Pass button clicked");
        }
        
        private void OnSettingsButtonClicked()
        {
            Debug.Log("[UIFortniteLobbyView] Settings button clicked - Opening Settings");
            Open<UISettingsView>();
        }
    }
}
