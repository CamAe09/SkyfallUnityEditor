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
        [SerializeField] private float _searchTimeout = 10f;
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
                    Debug.LogWarning($"[UIFortniteLobbyView] Search timeout after {_searchTimeout} seconds - no games found");
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
            
            if (SeasonEndController.Instance != null && SeasonEndController.Instance.IsInDowntime)
            {
                Debug.LogWarning("[UIFortniteLobbyView] Cannot start game - Season downtime is active!");
                
                if (_playButtonText != null)
                {
                    _playButtonText.text = "SEASON DOWNTIME";
                }
                
                return;
            }
            
            _availableSessions.Clear();
            _isSearchingForGame = false;
            
            if (_playButtonText != null)
            {
                _playButtonText.text = "CONNECTING...";
            }
            
            Debug.Log("[UIFortniteLobbyView] Rejoining lobby to refresh session list...");
            Context.Matchmaking.JoinLobby(true);
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
            Debug.Log($"[UIFortniteLobbyView] Looking for {_gameplayType} games with max {_maxPlayers} players");
        }
        
        private void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Debug.Log($"[UIFortniteLobbyView] ========== SESSION LIST UPDATE ==========");
            Debug.Log($"[UIFortniteLobbyView] Total sessions received: {sessionList.Count}");
            Debug.Log($"[UIFortniteLobbyView] Is searching: {_isSearchingForGame}");
            
            foreach (var session in sessionList)
            {
                Debug.Log($"[UIFortniteLobbyView] RAW SESSION:");
                Debug.Log($"  Name: {session.Name}");
                Debug.Log($"  IsValid: {session.IsValid}");
                Debug.Log($"  IsOpen: {session.IsOpen}");
                Debug.Log($"  IsVisible: {session.IsVisible}");
                Debug.Log($"  PlayerCount: {session.PlayerCount}/{session.MaxPlayers}");
                Debug.Log($"  Region: {session.Region}");
                Debug.Log($"  HasMap: {session.HasMap()}");
                if (session.HasMap())
                {
                    var mapSetup = session.GetMapSetup();
                    Debug.Log($"  Map: {(mapSetup != null ? mapSetup.DisplayName : "NULL")}");
                }
                Debug.Log($"  GameplayType: {session.GetGameplayType()}");
            }
            
            if (!_isSearchingForGame)
            {
                Debug.Log("[UIFortniteLobbyView] Not searching, ignoring session update");
                Debug.Log($"[UIFortniteLobbyView] ========================================");
                return;
            }
            
            _availableSessions.Clear();
            
            int filteredOutCount = 0;
            string filterReasons = "";
            
            foreach (var session in sessionList)
            {
                bool filtered = false;
                string reason = "";
                
                if (session.IsValid == false)
                {
                    filtered = true;
                    reason = "not valid";
                }
                else if (session.IsOpen == false)
                {
                    filtered = true;
                    reason = "not open";
                }
                else if (session.IsVisible == false)
                {
                    filtered = true;
                    reason = "not visible";
                }
                else if (session.PlayerCount >= session.MaxPlayers)
                {
                    filtered = true;
                    reason = "full";
                }
                else if (session.HasMap() == false)
                {
                    filtered = true;
                    reason = "no map";
                }
                else
                {
                    var sessionGameplayType = session.GetGameplayType();
                    if (sessionGameplayType != _gameplayType)
                    {
                        filtered = true;
                        reason = $"wrong type ({sessionGameplayType} vs {_gameplayType})";
                    }
                }
                
                if (filtered)
                {
                    filteredOutCount++;
                    filterReasons += $"\n  - {session.GetDisplayName()}: {reason}";
                }
                else
                {
                    _availableSessions.Add(session);
                    Debug.Log($"[UIFortniteLobbyView] ✓ Found valid session: {session.GetDisplayName()} ({session.PlayerCount}/{session.MaxPlayers})");
                }
            }
            
            if (filteredOutCount > 0)
            {
                Debug.Log($"[UIFortniteLobbyView] Filtered out {filteredOutCount} sessions:{filterReasons}");
            }
            
            Debug.Log($"[UIFortniteLobbyView] {_availableSessions.Count} sessions match criteria");
            Debug.Log($"[UIFortniteLobbyView] ========================================");
            
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
            
            if (_playButtonText != null && _playButtonText.text == "CONNECTING...")
            {
                Debug.Log("[UIFortniteLobbyView] Lobby connected! Starting search...");
                StartQuickPlay();
            }
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
