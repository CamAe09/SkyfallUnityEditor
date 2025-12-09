using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;
using Fusion.Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

namespace TPSBR.UI
{
    public class UIMainMenuView : UIView
    {
        // PRIVATE MEMBERS

        [Header("Quick Play Settings")]
        [SerializeField]
        [Tooltip("Maximum players for created sessions")]
        private int _maxPlayers = 20;
        [SerializeField]
        [Tooltip("Create as dedicated server (otherwise Host)")]
        private bool _dedicatedServer = false;

        [Header("UI Elements")]
        [SerializeField]
        private UIButton _playButton;
        [SerializeField]
        private UIButton _settingsButton;
        [SerializeField]
        private UIButton _shopButton;
        [SerializeField]
        private UIButton _questsButton;
        [SerializeField]
        private UIButton _creditsButton;
        [SerializeField]
        private UIButton _changeNicknameButton;
        [SerializeField]
        private UIButton _quitButton;
        [SerializeField]
        private UIButton _playerButton;
        [SerializeField]
        private UIButton _partyButton;
        [SerializeField]
        private UIButton _replaysButton;
        [SerializeField]
        private UIPlayer _player;
        [SerializeField]
        private TextMeshProUGUI _agentName;
        [SerializeField]
        private TextMeshProUGUI _applicationVersion;

        private bool _quickPlayInProgress = false;

        // PUBLIC METHODS

        public void OnPlayerButtonPointerEnter()
        {
            Context.PlayerPreview.ShowOutline(true);
        }

        public void OnPlayerButtonPointerExit()
        {
            Context.PlayerPreview.ShowOutline(false);
        }

        // UIView INTEFACE

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _settingsButton.onClick.AddListener(OnSettingsButton);
            _playButton.onClick.AddListener(OnPlayButton);
            _creditsButton.onClick.AddListener(OnCreditsButton);
            _changeNicknameButton.onClick.AddListener(OnChangeNicknameButton);
            _quitButton.onClick.AddListener(OnQuitButton);
            _playerButton.onClick.AddListener(OnPlayerButton);

            if (_shopButton != null)
                _shopButton.onClick.AddListener(OnShopButton);

            if (_questsButton != null)
                _questsButton.onClick.AddListener(OnQuestsButton);

            if (_partyButton != null)
                _partyButton.onClick.AddListener(OnPartyButton);

            if (_replaysButton != null)
                _replaysButton.onClick.AddListener(OnReplaysButton);

            _applicationVersion.text = $"Version {Application.version}";
        }

        protected override void OnDeinitialize()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsButton);
            _playButton.onClick.RemoveListener(OnPlayButton);
            _creditsButton.onClick.RemoveListener(OnCreditsButton);
            _changeNicknameButton.onClick.RemoveListener(OnChangeNicknameButton);
            _quitButton.onClick.RemoveListener(OnQuitButton);
            _playerButton.onClick.RemoveListener(OnPlayerButton);

            if (_shopButton != null)
                _shopButton.onClick.RemoveListener(OnShopButton);

            if (_questsButton != null)
                _questsButton.onClick.RemoveListener(OnQuestsButton);

            if (_partyButton != null)
                _partyButton.onClick.RemoveListener(OnPartyButton);

            if (_replaysButton != null)
                _replaysButton.onClick.RemoveListener(OnReplaysButton);

            base.OnDeinitialize();
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            Debug.Log("[UIMainMenuView] OnOpen called");

            UpdatePlayer();

            Global.PlayerService.PlayerDataChanged += OnPlayerDataChanged;
            Context.PlayerPreview.ShowAgent(Context.PlayerData.AgentID);
            Context.PlayerPreview.ShowOutline(false);

            _quickPlayInProgress = false;

            if (PhotonAppSettings.Global.AppSettings.AppIdFusion.HasValue())
            {
                Debug.Log("[UIMainMenuView] Joining Photon lobby...");
                Context.Matchmaking.JoinLobby(false);
            }
            else
            {
                Debug.LogWarning("[UIMainMenuView] No Fusion App ID configured - matchmaking disabled");
            }
        }

        protected override void OnClose()
        {
            Global.PlayerService.PlayerDataChanged -= OnPlayerDataChanged;
            Context.PlayerPreview.ShowOutline(false);

            if (_quickPlayInProgress)
            {
                Context.Matchmaking.SessionListUpdated -= OnQuickPlaySessionListUpdated;
                _quickPlayInProgress = false;
            }

            Context.Matchmaking.LeaveLobby();

            base.OnClose();
        }

        protected override bool OnBackAction()
        {
            if (IsInteractable == false)
                return false;

            OnQuitButton();
            return true;
        }

        // PRIVATE METHODS

        private void OnSettingsButton()
        {
            Open<UISettingsView>();
        }

        private void OnPlayButton()
        {
            Debug.Log("[Quick Play] Play button clicked!");

            if (_quickPlayInProgress)
            {
                Debug.LogWarning("[Quick Play] Quick play already in progress, ignoring click");
                return;
            }

            if (PhotonAppSettings.Global.AppSettings.AppIdFusion.HasValue() == false)
            {
                var errorDialog = Open<UIErrorDialogView>();
                errorDialog.Title.text = "Missing App Id";
                errorDialog.Description.text = "Fusion App Id is not assigned in the Photon App Settings asset.\n\nPlease follow instructions in the Fusion BR200 documentation on how to create and assign App Id.";
                
                #if UNITY_EDITOR
                errorDialog.HasClosed += () =>
                {
                    UnityEditor.Selection.activeObject = PhotonAppSettings.Global;
                    UnityEditor.EditorGUIUtility.PingObject(PhotonAppSettings.Global);
                };
                #endif
                return;
            }

            if (Context.Matchmaking.IsConnectedToLobby == false)
            {
                Debug.LogWarning("[Quick Play] Not connected to lobby. Connecting and will retry...");
                Context.Matchmaking.JoinLobby(true);
                StartCoroutine(WaitForLobbyAndRetry());
                return;
            }

            _quickPlayInProgress = true;
            _playButton.interactable = false;

            Debug.Log("[Quick Play] Starting Battle Royale quick match...");
            
            Context.Matchmaking.SessionListUpdated += OnQuickPlaySessionListUpdated;
            
            Context.Matchmaking.JoinLobby(true);
        }

        private void OnCreditsButton()
        {
            Open<UICreditsView>();
        }

        private void OnChangeNicknameButton()
        {
            var changeNicknameView = Open<UIChangeNicknameView>();
            changeNicknameView.SetData("CHANGE NICKNAME", false);
        }

        private void OnQuitButton()
        {
            var dialog = Open<UIYesNoDialogView>();

            dialog.Title.text = "EXIT GAME";
            dialog.Description.text = "Are you sure you want to exit the game?";

            dialog.YesButtonText.text = "EXIT";
            dialog.NoButtonText.text = "CANCEL";

            dialog.HasClosed += (result) =>
            {
                if (result == true)
                {
                    SceneUI.Scene.Quit();
                }
            };
        }

        private void OnPlayerButton()
        {
            var agentSelection = Open<UIAgentSelectionView>();
            agentSelection.BackView = this;

            Close();
        }

        private void OnShopButton()
        {
            var modernShop = Open<ModernShopManager>();

            if (modernShop == null)
            {
                Debug.LogWarning("[Modern Shop] ModernShopManager not found. Trying old UIShopView...");
                var shopView = Open<UIShopView>();
                
                if (shopView == null)
                {
                    Debug.LogWarning("[Shop System] No shop view found. Please run: TPSBR → 🎨 Create Modern Shop UI");
                    return;
                }

                shopView.BackView = this;
                Close();
                return;
            }

            modernShop.BackView = this;

            var agentSelection = SceneUI.Get<UIAgentSelectionView>();
            if (agentSelection != null && agentSelection.IsOpen == true)
            {
                agentSelection.Close();
            }

            Close();
        }

        private void OnPartyButton()
        {
            Open<UIPartyViewSimple>();
        }

        private void OnReplaysButton()
        {
            SceneManager.LoadScene("ReplayViewer");
        }

        private void OnQuestsButton()
        {
            var questView = Open<UIQuestView>();
            
            if (questView == null)
            {
                Debug.LogWarning("[Quest System] UIQuestView not found. Please run: TPSBR → Generate Quest UI");
                return;
            }

            questView.BackView = this;

            var agentSelection = SceneUI.Get<UIAgentSelectionView>();
            if (agentSelection != null && agentSelection.IsOpen == true)
            {
                agentSelection.Close();
            }

            Close();
        }

        private void OnPlayerDataChanged(PlayerData playerData)
        {
            UpdatePlayer();
        }

        private void UpdatePlayer()
        {
            _player.SetData(Context, Context.PlayerData);
            Context.PlayerPreview.ShowAgent(Context.PlayerData.AgentID);

            var setup = Context.Settings.Agent.GetAgentSetup(Context.PlayerData.AgentID);
            _agentName.text = setup != null ? $"Playing as {setup.DisplayName}" : string.Empty;
        }

        private void OnQuickPlaySessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Context.Matchmaking.SessionListUpdated -= OnQuickPlaySessionListUpdated;

            if (_quickPlayInProgress == false)
                return;

            var availableSessions = sessionList
                .Where(s => s.IsValid && s.IsOpen && s.IsVisible)
                .Where(s => s.GetGameplayType() == EGameplayType.BattleRoyale)
                .Where(s => s.PlayerCount < s.MaxPlayers)
                .Where(s => s.HasMap())
                .OrderByDescending(s => s.PlayerCount)
                .ToList();

            if (availableSessions.Count > 0)
            {
                var bestSession = availableSessions[0];
                Debug.Log($"[Quick Play] Joining session '{bestSession.Name}' with {bestSession.PlayerCount}/{bestSession.MaxPlayers} players...");
                Context.Matchmaking.JoinSession(bestSession);
            }
            else
            {
                Debug.Log("[Quick Play] No available sessions found. Creating new Battle Royale session...");
                CreateQuickPlaySession();
            }
        }

        private void CreateQuickPlaySession()
        {
            var mapSettings = Context.Settings.Map;
            if (mapSettings == null || mapSettings.Maps == null || mapSettings.Maps.Length == 0)
            {
                Debug.LogError("[Quick Play] No maps configured!");
                ResetQuickPlayState();
                
                var errorDialog = Open<UIErrorDialogView>();
                errorDialog.Title.text = "No Maps Available";
                errorDialog.Description.text = "No maps are configured in the game settings. Please configure maps in GlobalSettings.";
                return;
            }

            var availableMaps = mapSettings.Maps.Where(m => m != null && m.ShowInMapSelection).ToList();
            if (availableMaps.Count == 0)
            {
                Debug.LogError("[Quick Play] No valid maps found for selection!");
                ResetQuickPlayState();
                
                var errorDialog = Open<UIErrorDialogView>();
                errorDialog.Title.text = "No Valid Maps";
                errorDialog.Description.text = "No valid maps available. Please check map configuration in GlobalSettings.";
                return;
            }

            var randomMap = availableMaps[Random.Range(0, availableMaps.Count)];
            string sessionName = $"BR_{Random.Range(1000, 9999)}";

            var request = new SessionRequest
            {
                UserID = Context.PlayerData.UserID,
                GameMode = _dedicatedServer ? GameMode.Server : GameMode.Host,
                DisplayName = Context.PlayerData.Nickname,
                SessionName = sessionName,
                ScenePath = randomMap.ScenePath,
                GameplayType = EGameplayType.BattleRoyale,
                MaxPlayers = _maxPlayers,
            };

            Debug.Log($"[Quick Play] Creating new session '{sessionName}' on map '{randomMap.DisplayName}' (MaxPlayers: {_maxPlayers})...");
            Global.Networking.StartGame(request);
        }

        private void ResetQuickPlayState()
        {
            _quickPlayInProgress = false;
            _playButton.interactable = true;
        }

        private System.Collections.IEnumerator WaitForLobbyAndRetry()
        {
            float timeout = 5f;
            float elapsed = 0f;

            while (Context.Matchmaking.IsConnectedToLobby == false && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (Context.Matchmaking.IsConnectedToLobby)
            {
                Debug.Log("[Quick Play] Connected to lobby! Retrying Play...");
                OnPlayButton();
            }
            else
            {
                Debug.LogError("[Quick Play] Failed to connect to lobby within timeout");
                var errorDialog = Open<UIErrorDialogView>();
                errorDialog.Title.text = "Connection Failed";
                errorDialog.Description.text = "Failed to connect to Photon lobby. Please check your internet connection and try again.";
            }
        }
    }
}
