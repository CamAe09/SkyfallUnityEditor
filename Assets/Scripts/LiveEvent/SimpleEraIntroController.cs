using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cinemachine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TPSBR
{
    public class SimpleEraIntroController : MonoBehaviour
    {
        public static SimpleEraIntroController Instance { get; private set; }
        public bool IsIntroActive => _isIntroActive;

        [Header("Player Spawn")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private GameObject _playerPrefab;

        [Header("Player Animations")]
        [SerializeField] private AnimationClip _layingDownIdleClip;
        [SerializeField] private AnimationClip _wakeUpClip;

        [Header("Locomotion Animations")]
        [SerializeField] private AnimationClip _idleClip;
        [SerializeField] private AnimationClip _walkClip;

        [Header("NPC")]
        [SerializeField] private GameObject _npcCharacter;
        [SerializeField] private Transform _npcTransform;
        [SerializeField] private Animator _npcAnimator;
        [SerializeField] private string _npcTalkStateName = "Talking";
        [SerializeField] private float _interactionDistance = 3f;

        [Header("UI")]
        [SerializeField] private Canvas _uiCanvas;
        [SerializeField] private Text _promptText;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private float _fadeDuration = 2f;
        [SerializeField] private Canvas[] _canvasesToHideDuringCutscene;

        [Header("Cameras")]
        [SerializeField] private CinemachineVirtualCamera _introCam;
        [SerializeField] private CinemachineVirtualCamera _playerFollowCam;
        [SerializeField] private CinemachineVirtualCamera _dialogueCam;
        [SerializeField] private CinemachineVirtualCamera _flybyCamera;
        [SerializeField] private Transform[] _flybyWaypoints;
        [SerializeField] private float _flybyDuration = 10f;
        [SerializeField] private Camera _mainCamera;

        [Header("Audio")]
        [SerializeField] private AudioClip _npcDialogueClip;
        [SerializeField] private AudioClip _theresYourStopClip;
        [SerializeField] private AudioClip _backgroundMusic;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioSource _musicAudioSource;

        [Header("Input")]
        [SerializeField] private float _holdDuration = 2f;

        private GameObject _spawnedPlayer;
        private Transform _cameraTarget;
        private Animator _playerAnimator;
        private CharacterController _characterController;
        private CinemachineBrain _cinemachineBrain;
        private PlayableGraph _playableGraph;
        private AnimationClipPlayable _currentClipPlayable;
        private List<GameObject> _hiddenUIObjects = new List<GameObject>();
        private bool _hasShownIntro = false;
        private bool _isIntroActive = false;
        private bool _isNearNPC = false;
        private float _holdTimer = 0f;
        private bool _hasSpokenToNPC = false;
        private bool _hasWokenUp = false;

        private const string PREF_LAST_ERA_SHOWN = "SimpleEraIntro_LastEraShown";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera != null)
            {
                _cinemachineBrain = _mainCamera.GetComponent<CinemachineBrain>();
            }

            SetupUI();

            if (_npcCharacter != null)
            {
                _npcCharacter.SetActive(false);
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0f;
            }

            if (_musicAudioSource == null)
            {
                _musicAudioSource = gameObject.AddComponent<AudioSource>();
                _musicAudioSource.playOnAwake = false;
                _musicAudioSource.spatialBlend = 0f;
                _musicAudioSource.loop = true;
                _musicAudioSource.volume = 0.3f;
            }

            Debug.Log("[SimpleEraIntroController] Initialized");
        }

        private void OnDestroy()
        {
            if (_playableGraph.IsValid())
            {
                _playableGraph.Destroy();
            }
        }

        private void SetupUI()
        {
            if (_uiCanvas == null)
            {
                GameObject canvasObj = new GameObject("CutsceneIntroCanvas");
                canvasObj.transform.SetParent(transform);

                _uiCanvas = canvasObj.AddComponent<Canvas>();
                _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _uiCanvas.sortingOrder = 9999;

                var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                Debug.Log("[SimpleEraIntroController] Created canvas");
            }

            if (_fadeImage == null && _uiCanvas != null)
            {
                GameObject fadeObj = new GameObject("FadeImage");
                fadeObj.transform.SetParent(_uiCanvas.transform, false);

                _fadeImage = fadeObj.AddComponent<Image>();
                _fadeImage.color = Color.black;

                RectTransform rt = fadeObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                Debug.Log("[SimpleEraIntroController] Created fade image");
            }

            if (_promptText == null && _uiCanvas != null)
            {
                GameObject textObj = new GameObject("PromptText");
                textObj.transform.SetParent(_uiCanvas.transform, false);

                _promptText = textObj.AddComponent<Text>();
                _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _promptText.fontSize = 36;
                _promptText.alignment = TextAnchor.MiddleCenter;
                _promptText.color = Color.white;
                _promptText.text = "";

                var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(2, -2);

                RectTransform rt = textObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.3f);
                rt.anchorMax = new Vector2(0.5f, 0.3f);
                rt.sizeDelta = new Vector2(800, 100);
                rt.anchoredPosition = Vector2.zero;

                Debug.Log("[SimpleEraIntroController] Created prompt text");
            }

            if (_uiCanvas != null)
            {
                _uiCanvas.gameObject.SetActive(false);
            }

            if (_fadeImage != null)
            {
                _fadeImage.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            CheckForNewEra();
        }

        private void Update()
        {
            if (!_isIntroActive)
                return;
                
            if (_spawnedPlayer == null)
                return;

            HideNetworkedPlayers();
            HideRuntimeSpawnedUI();

            if (!_hasWokenUp)
            {
                UpdateWakeUpInput();
                return;
            }

            CheckDistanceToNPC();

            if (_isNearNPC && !_hasSpokenToNPC)
            {
                UpdateHoldInput();
            }
        }

        private void HideRuntimeSpawnedUI()
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                if (canvas == null || canvas == _uiCanvas)
                    continue;

                if (_hiddenUIObjects.Contains(canvas.gameObject))
                    continue;

                if (canvas.gameObject.activeSelf)
                {
                    if (canvas.name.Contains("UIGameplayView") || canvas.name.Contains("Gameplay") ||
                        canvas.transform.parent != null && canvas.transform.parent.name.Contains("Gameplay"))
                    {
                        canvas.gameObject.SetActive(false);
                        _hiddenUIObjects.Add(canvas.gameObject);
                        Debug.Log($"[SimpleEraIntroController] Hidden runtime spawned UI: {canvas.name}");
                    }
                }
            }
        }

        private void CheckForNewEra()
        {
            if (_hasShownIntro)
                return;

            Debug.Log($"[SimpleEraIntroController] Starting intro sequence");
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            _isIntroActive = true;
            _hasShownIntro = true;

            HideGameUI();

            if (_backgroundMusic != null && _musicAudioSource != null)
            {
                _musicAudioSource.clip = _backgroundMusic;
                _musicAudioSource.Play();
                Debug.Log("[SimpleEraIntroController] Started background music");
            }

            yield return StartCoroutine(FadeFromBlack());

            SpawnPlayer();

            if (_npcCharacter != null)
            {
                _npcCharacter.SetActive(true);
            }

            SetActiveCamera(_introCam);

            if (_uiCanvas != null)
            {
                _uiCanvas.gameObject.SetActive(true);
            }

            PlayLayingDownAnimation();

            if (_promptText != null)
            {
                _promptText.text = "Press P";
            }

            Debug.Log("[SimpleEraIntroController] Player laying down - Press P to wake up");

            while (!_hasWokenUp)
            {
                yield return null;
            }

            Debug.Log("[SimpleEraIntroController] Player can now move - walk to NPC");

            while (!_hasSpokenToNPC)
            {
                yield return null;
            }

            yield return StartCoroutine(PlayNPCDialogue());

            CompleteIntro();

            var battleRoyaleMode = FindFirstObjectByType<BattleRoyaleGameplayMode>();
            if (battleRoyaleMode != null)
            {
                battleRoyaleMode.StartImmediately();
                Debug.Log("[SimpleEraIntroController] Starting Battle Royale drop sequence");
                
                yield return new WaitForSeconds(2f);
                
                if (battleRoyaleMode.Airplane != null)
                {
                    yield return StartCoroutine(PlayMapFlyby());
                }
            }
            else
            {
                yield return StartCoroutine(PlayMapFlyby());
            }
        }

        private void SpawnPlayer()
        {
            if (_playerPrefab == null)
            {
                Debug.LogError("[SimpleEraIntroController] Player prefab not assigned!");
                return;
            }

            if (_playerSpawnPoint == null)
            {
                Debug.LogError("[SimpleEraIntroController] Player spawn point not assigned!");
                return;
            }

            Vector3 spawnPosition = _playerSpawnPoint.position;
            Debug.Log($"[SimpleEraIntroController] Spawn point position: {spawnPosition}");

            RaycastHit hit;
            bool foundGround = false;

            if (spawnPosition.y > 10f)
            {
                if (Physics.Raycast(new Vector3(spawnPosition.x, 500f, spawnPosition.z), Vector3.down, out hit, 1000f))
                {
                    spawnPosition = hit.point;
                    foundGround = true;
                    Debug.Log($"[SimpleEraIntroController] High spawn point - raycasted from 500 units, found ground at {hit.point}");
                }
            }
            else
            {
                if (Physics.Raycast(spawnPosition + Vector3.up * 2f, Vector3.down, out hit, 50f))
                {
                    spawnPosition = hit.point;
                    foundGround = true;
                    Debug.Log($"[SimpleEraIntroController] Found ground at {hit.point}");
                }
            }

            if (!foundGround)
            {
                spawnPosition.y = 0f;
                Debug.LogWarning($"[SimpleEraIntroController] No ground found, spawning at Y=0");
            }

            Debug.Log($"[SimpleEraIntroController] Final spawn position: {spawnPosition}");

            _spawnedPlayer = Instantiate(_playerPrefab, spawnPosition, _playerSpawnPoint.rotation);

            _characterController = _spawnedPlayer.GetComponent<CharacterController>();
            if (_characterController == null)
            {
                _characterController = _spawnedPlayer.AddComponent<CharacterController>();
            }

            _characterController.radius = 0.5f;
            _characterController.height = 2f;
            _characterController.center = new Vector3(0, 1f, 0);
            _characterController.enabled = false;

            Rigidbody rb = _spawnedPlayer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            _playerAnimator = _spawnedPlayer.GetComponentInChildren<Animator>();
            if (_playerAnimator == null)
            {
                Debug.LogError("[SimpleEraIntroController] No Animator found on player prefab! Please ensure the prefab has an Animator component.");
                return;
            }

            if (_playerAnimator.avatar == null)
            {
                Debug.LogWarning("[SimpleEraIntroController] Animator has no Avatar assigned! Character will T-pose.");
            }
            else
            {
                Debug.Log($"[SimpleEraIntroController] Using avatar: {_playerAnimator.avatar.name}");
            }

            GameObject cameraTargetObj = new GameObject("CameraTarget");
            cameraTargetObj.transform.SetParent(_spawnedPlayer.transform);
            cameraTargetObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            cameraTargetObj.transform.localRotation = Quaternion.identity;
            _cameraTarget = cameraTargetObj.transform;
            Debug.Log("[SimpleEraIntroController] Created camera target at character upper body");

            _playableGraph = PlayableGraph.Create("CutsceneGraph");
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", _playerAnimator);

            if (_playerFollowCam != null)
            {
                _playerFollowCam.Follow = _cameraTarget;
                _playerFollowCam.LookAt = _cameraTarget;

                var transposer = _playerFollowCam.GetCinemachineComponent<Cinemachine.CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_FollowOffset = new Vector3(0, 0.3f, -3.5f);
                    transposer.m_XDamping = 1f;
                    transposer.m_YDamping = 1f;
                    transposer.m_ZDamping = 1f;
                    transposer.m_BindingMode = Cinemachine.CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
                    Debug.Log("[SimpleEraIntroController] Configured player follow camera - real player style");
                }

                var composer = _playerFollowCam.GetCinemachineComponent<Cinemachine.CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_TrackedObjectOffset = new Vector3(0, 0, 0);
                    composer.m_HorizontalDamping = 1f;
                    composer.m_VerticalDamping = 1f;
                    composer.m_ScreenX = 0.5f;
                    composer.m_ScreenY = 0.52f;
                    composer.m_DeadZoneWidth = 0.05f;
                    composer.m_DeadZoneHeight = 0.05f;
                    composer.m_SoftZoneWidth = 0.8f;
                    composer.m_SoftZoneHeight = 0.8f;
                    composer.m_BiasX = 0f;
                    composer.m_BiasY = 0f;
                }
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Debug.Log("[SimpleEraIntroController] Player spawned");
        }

        private void PlayLayingDownAnimation()
        {
            if (_layingDownIdleClip == null)
            {
                Debug.LogWarning("[SimpleEraIntroController] Laying down clip not assigned! Please assign it in the Inspector.");
                return;
            }

            if (!_playableGraph.IsValid())
            {
                Debug.LogWarning("[SimpleEraIntroController] Playable graph not initialized!");
                return;
            }

            _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _layingDownIdleClip);
            _currentClipPlayable.SetDuration(_layingDownIdleClip.length);
            _currentClipPlayable.Pause();

            var playableOutput = _playableGraph.GetOutput(0);
            ((AnimationPlayableOutput)playableOutput).SetSourcePlayable(_currentClipPlayable);

            _playableGraph.Play();

            Debug.Log($"[SimpleEraIntroController] Playing laying down idle animation '{_layingDownIdleClip.name}'");
        }

        private void UpdateWakeUpInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                Debug.LogWarning("[SimpleEraIntroController] Keyboard.current is null!");
                return;
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                Debug.Log("[SimpleEraIntroController] P key pressed - triggering wake up!");
                StartCoroutine(PlayWakeUpAnimation());
            }
        }

        private IEnumerator PlayWakeUpAnimation()
        {
            if (_promptText != null)
            {
                _promptText.text = "";
            }

            Debug.Log("[SimpleEraIntroController] Player waking up");

            if (_wakeUpClip == null)
            {
                Debug.LogWarning("[SimpleEraIntroController] Wake up clip not assigned! Please assign it in the Inspector.");
                yield return new WaitForSeconds(1f);
            }
            else if (!_playableGraph.IsValid())
            {
                Debug.LogWarning("[SimpleEraIntroController] Playable graph not initialized!");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                _currentClipPlayable.Destroy();

                _currentClipPlayable = AnimationClipPlayable.Create(_playableGraph, _wakeUpClip);
                _currentClipPlayable.SetDuration(_wakeUpClip.length);

                var playableOutput = _playableGraph.GetOutput(0);
                ((AnimationPlayableOutput)playableOutput).SetSourcePlayable(_currentClipPlayable);

                _currentClipPlayable.Play();

                Debug.Log($"[SimpleEraIntroController] Playing wake-up clip '{_wakeUpClip.name}' (duration: {_wakeUpClip.length}s)");

                yield return new WaitForSeconds(_wakeUpClip.length);

                _currentClipPlayable.Pause();

                Debug.Log("[SimpleEraIntroController] Wake up animation completed");

                _playableGraph.Stop();

                if (_characterController != null)
                {
                    Vector3 position = _spawnedPlayer.transform.position;
                    if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Default")))
                    {
                        position.y = hit.point.y;
                        _spawnedPlayer.transform.position = position;
                        Debug.Log($"[SimpleEraIntroController] Realigned player to ground: {position}");
                    }

                    _characterController.enabled = true;
                    Debug.Log("[SimpleEraIntroController] CharacterController enabled");
                }

                Rigidbody rb = _spawnedPlayer.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    Debug.Log("[SimpleEraIntroController] Set Rigidbody to non-kinematic for movement");
                }

                if (_playerAnimator != null)
                {
                    _playerAnimator.enabled = true;
                    _playerAnimator.Rebind();
                    _playerAnimator.Update(0f);
                    Debug.Log("[SimpleEraIntroController] Animator rebound for locomotion");
                }
            }

            _hasWokenUp = true;

            SetActiveCamera(_playerFollowCam);

            if (_playerFollowCam != null && _cameraTarget != null)
            {
                _playerFollowCam.Follow = _cameraTarget;
                _playerFollowCam.LookAt = _cameraTarget;

                var transposer = _playerFollowCam.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_FollowOffset = new Vector3(0, 0.3f, -3.5f);
                    transposer.m_BindingMode = Cinemachine.CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
                    transposer.m_XDamping = 1f;
                    transposer.m_YDamping = 1f;
                    transposer.m_ZDamping = 1f;
                    Debug.Log($"[SimpleEraIntroController] Set follow camera - offset: {transposer.m_FollowOffset}, real player style");
                }

                var composer = _playerFollowCam.GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_TrackedObjectOffset = new Vector3(0, 0, 0);
                    composer.m_HorizontalDamping = 1f;
                    composer.m_VerticalDamping = 1f;
                    composer.m_ScreenX = 0.5f;
                    composer.m_ScreenY = 0.52f;
                    composer.m_DeadZoneWidth = 0.05f;
                    composer.m_DeadZoneHeight = 0.05f;
                    composer.m_SoftZoneWidth = 0.8f;
                    composer.m_SoftZoneHeight = 0.8f;
                    composer.m_BiasX = 0f;
                    composer.m_BiasY = 0f;
                    Debug.Log($"[SimpleEraIntroController] Set composer - real player camera framing");
                }

                Debug.Log($"[SimpleEraIntroController] Player follow camera configured - Follow: {_playerFollowCam.Follow.name}, LookAt: {_playerFollowCam.LookAt.name}");
            }

            Debug.Log("[SimpleEraIntroController] Player control enabled - you should be able to move now");
        }

        private void CheckDistanceToNPC()
        {
            if (_spawnedPlayer == null || _npcTransform == null)
                return;

            float distance = Vector3.Distance(_spawnedPlayer.transform.position, _npcTransform.position);

            bool wasNear = _isNearNPC;
            _isNearNPC = distance <= _interactionDistance;

            if (_isNearNPC && !wasNear && !_hasSpokenToNPC)
            {
                if (_promptText != null)
                {
                    _promptText.text = "Hold P To Talk";
                }
            }
            else if (!_isNearNPC && wasNear)
            {
                if (_promptText != null)
                {
                    _promptText.text = "";
                }
                _holdTimer = 0f;
            }
        }

        private void UpdateHoldInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.pKey.isPressed)
            {
                _holdTimer += Time.deltaTime;

                if (_promptText != null)
                {
                    float progress = Mathf.Clamp01(_holdTimer / _holdDuration);
                    _promptText.text = $"Hold P To Talk [{(int)(progress * 100)}%]";
                }

                if (_holdTimer >= _holdDuration)
                {
                    OnTalkToNPC();
                }
            }
            else
            {
                _holdTimer = 0f;
                if (_promptText != null && _isNearNPC)
                {
                    _promptText.text = "Hold P To Talk";
                }
            }
        }

        private void OnTalkToNPC()
        {
            _hasSpokenToNPC = true;
            _holdTimer = 0f;

            if (_promptText != null)
            {
                _promptText.text = "";
            }

            Debug.Log("[SimpleEraIntroController] Player talked to NPC");
        }

        private IEnumerator PlayNPCDialogue()
        {
            SetActiveCamera(_dialogueCam);

            if (_npcAnimator != null)
            {
                _npcAnimator.Play(_npcTalkStateName, 0, 0f);
                Debug.Log($"[SimpleEraIntroController] Playing NPC animation state: {_npcTalkStateName}");
            }
            else
            {
                Debug.LogWarning("[SimpleEraIntroController] NPC Animator not found");
            }

            if (_npcDialogueClip != null && _audioSource != null)
            {
                _audioSource.clip = _npcDialogueClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_npcDialogueClip.length);
            }
            else
            {
                yield return new WaitForSeconds(5f);
            }
        }

        private IEnumerator PlayMapFlyby()
        {
            BattleRoyaleGameplayMode battleRoyaleMode = FindFirstObjectByType<BattleRoyaleGameplayMode>();
            
            if (battleRoyaleMode != null && battleRoyaleMode.Airplane != null)
            {
                Debug.Log("[SimpleEraIntroController] Following airplane for map flyby");
                yield return StartCoroutine(FollowAirplane(battleRoyaleMode.Airplane));
            }
            else
            {
                SetActiveCamera(_flybyCamera);

                if (_flybyWaypoints == null || _flybyWaypoints.Length == 0)
                {
                    Debug.LogWarning("[SimpleEraIntroController] No flyby waypoints assigned and no airplane found");
                    yield return new WaitForSeconds(5f);
                    yield break;
                }

                Transform cameraTransform = _flybyCamera.transform;

                if (_flybyWaypoints[0] != null)
                {
                    cameraTransform.position = _flybyWaypoints[0].position;
                    cameraTransform.rotation = _flybyWaypoints[0].rotation;
                }

                float elapsed = 0f;

                while (elapsed < _flybyDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / _flybyDuration;

                    int currentIndex = Mathf.FloorToInt(t * (_flybyWaypoints.Length - 1));
                    int nextIndex = Mathf.Min(currentIndex + 1, _flybyWaypoints.Length - 1);

                    float segmentT = (t * (_flybyWaypoints.Length - 1)) - currentIndex;

                    if (_flybyWaypoints[currentIndex] != null && _flybyWaypoints[nextIndex] != null)
                    {
                        cameraTransform.position = Vector3.Lerp(
                            _flybyWaypoints[currentIndex].position,
                            _flybyWaypoints[nextIndex].position,
                            segmentT
                        );

                        cameraTransform.rotation = Quaternion.Slerp(
                            _flybyWaypoints[currentIndex].rotation,
                            _flybyWaypoints[nextIndex].rotation,
                            segmentT
                        );
                    }

                    yield return null;
                }
            }
        }

        private IEnumerator FollowAirplane(Airplane airplane)
        {
            SetActiveCamera(_flybyCamera);
            Transform cameraTransform = _flybyCamera.transform;
            
            Vector3 cameraOffset = new Vector3(0, 8f, -15f);
            float followDuration = _flybyDuration;
            float elapsed = 0f;
            bool hasPlayedVoiceline = false;
            bool hasJumped = false;

            AirplaneAgent localPlayerAgent = null;
            Agent droppedPlayer = null;
            
            Gameplay gameplayScene = FindFirstObjectByType<Gameplay>();
            SceneContext context = gameplayScene != null ? gameplayScene.Context : null;

            while (elapsed < followDuration)
            {
                if (airplane == null)
                {
                    Debug.LogWarning("[SimpleEraIntroController] Airplane is null, ending flyby");
                    break;
                }

                Transform targetTransform = airplane.transform;
                Vector3 lookOffset = Vector3.forward * 20f;

                if (localPlayerAgent == null)
                {
                    AirplaneAgent[] agents = FindObjectsByType<AirplaneAgent>(FindObjectsSortMode.None);
                    foreach (var agent in agents)
                    {
                        if (agent.HasInputAuthority)
                        {
                            localPlayerAgent = agent;
                            Debug.Log("[SimpleEraIntroController] Found local player in airplane");
                            break;
                        }
                    }
                }

                if (localPlayerAgent == null && droppedPlayer == null)
                {
                    if (context != null && context.ObservedAgent != null)
                    {
                        droppedPlayer = context.ObservedAgent;
                        hasJumped = true;
                        Debug.Log($"[SimpleEraIntroController] Found ObservedAgent: {droppedPlayer.name} on layer: {LayerMask.LayerToName(droppedPlayer.gameObject.layer)}");
                        
                        if (!hasPlayedVoiceline)
                        {
                            PlayTheresYourStopAudio();
                            hasPlayedVoiceline = true;
                        }
                    }
                    else
                    {
                        Agent[] allAgents = FindObjectsByType<Agent>(FindObjectsSortMode.None);
                        foreach (var agent in allAgents)
                        {
                            if (agent.HasInputAuthority && agent.gameObject.layer == LayerMask.NameToLayer("Agent"))
                            {
                                droppedPlayer = agent;
                                hasJumped = true;
                                Debug.Log($"[SimpleEraIntroController] Player has jumped! Following player: {agent.name} on layer: {LayerMask.LayerToName(agent.gameObject.layer)}");
                                
                                if (!hasPlayedVoiceline)
                                {
                                    PlayTheresYourStopAudio();
                                    hasPlayedVoiceline = true;
                                }
                                break;
                            }
                        }
                    }
                }

                if (droppedPlayer != null)
                {
                    targetTransform = droppedPlayer.transform;
                    cameraOffset = new Vector3(0, 5f, -10f);
                    lookOffset = Vector3.forward * 5f;
                    
                    if (hasJumped && elapsed > 3f)
                    {
                        Debug.Log("[SimpleEraIntroController] Player jumped and 3 seconds passed, ending flyby early");
                        break;
                    }
                }
                else if (localPlayerAgent != null)
                {
                    targetTransform = localPlayerAgent.transform;
                }

                Vector3 targetPosition = targetTransform.position + targetTransform.rotation * cameraOffset;
                Vector3 lookAtPosition = targetTransform.position + targetTransform.forward * lookOffset.z;

                cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, Time.deltaTime * 3f);
                cameraTransform.LookAt(lookAtPosition);

                elapsed += Time.deltaTime;
                yield return null;
            }

            EndIntroAndRestoreCamera();
        }

        private void EndIntroAndRestoreCamera()
        {
            SetActiveCamera(null);
            
            if (_flybyCamera != null)
            {
                _flybyCamera.Priority = -100;
                _flybyCamera.enabled = false;
                Debug.Log("[SimpleEraIntroController] Disabled flyby camera");
            }

            if (_introCam != null)
            {
                _introCam.Priority = -100;
                _introCam.enabled = false;
            }

            if (_playerFollowCam != null)
            {
                _playerFollowCam.Priority = -100;
                _playerFollowCam.enabled = false;
            }

            if (_dialogueCam != null)
            {
                _dialogueCam.Priority = -100;
                _dialogueCam.enabled = false;
            }

            if (_cinemachineBrain != null)
            {
                _cinemachineBrain.enabled = false;
                Debug.Log("[SimpleEraIntroController] DISABLED CinemachineBrain - Agent will control camera directly");
            }

            _isIntroActive = false;

            Debug.Log("[SimpleEraIntroController] All intro cameras disabled, camera control returned to Agent");
        }

        private void PlayTheresYourStopAudio()
        {
            if (_theresYourStopClip != null && _audioSource != null)
            {
                _audioSource.clip = _theresYourStopClip;
                _audioSource.Play();
            }
        }

        private void CompleteIntro()
        {
            if (_spawnedPlayer != null)
            {
                Destroy(_spawnedPlayer);
            }

            if (_npcCharacter != null)
            {
                _npcCharacter.SetActive(false);
            }

            if (_uiCanvas != null)
            {
                _uiCanvas.gameObject.SetActive(false);
            }

            if (_musicAudioSource != null && _musicAudioSource.isPlaying)
            {
                _musicAudioSource.Stop();
                Debug.Log("[SimpleEraIntroController] Stopped background music");
            }

            ShowGameUI();

            SetActiveCamera(null);

            _isIntroActive = false;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var battleRoyaleMode = FindFirstObjectByType<BattleRoyaleGameplayMode>();
            if (battleRoyaleMode != null)
            {
                battleRoyaleMode.StartImmediately();
                Debug.Log("[SimpleEraIntroController] Auto-starting Battle Royale drop sequence");
            }
            else
            {
                Debug.LogWarning("[SimpleEraIntroController] BattleRoyaleGameplayMode not found - cannot auto-start game");
            }

            Debug.Log("[SimpleEraIntroController] Intro complete - transitioning to plane drop");
        }

        private void HideGameUI()
        {
            _hiddenUIObjects.Clear();

            if (_canvasesToHideDuringCutscene != null && _canvasesToHideDuringCutscene.Length > 0)
            {
                foreach (var canvas in _canvasesToHideDuringCutscene)
                {
                    if (canvas != null && canvas.gameObject.activeSelf)
                    {
                        canvas.gameObject.SetActive(false);
                        _hiddenUIObjects.Add(canvas.gameObject);
                        Debug.Log($"[SimpleEraIntroController] Hidden canvas: {canvas.name}");
                    }
                }
            }

            GameObject gameplayCanvas = GameObject.Find("Canvas");
            if (gameplayCanvas != null && gameplayCanvas.activeSelf && !_hiddenUIObjects.Contains(gameplayCanvas))
            {
                gameplayCanvas.SetActive(false);
                _hiddenUIObjects.Add(gameplayCanvas);
                Debug.Log("[SimpleEraIntroController] Hidden gameplay Canvas");
            }

            GameObject gameplayObj = GameObject.Find("Gameplay");
            if (gameplayObj != null)
            {
                Canvas[] canvases = gameplayObj.GetComponentsInChildren<Canvas>(true);
                foreach (var canvas in canvases)
                {
                    if (canvas.gameObject.activeSelf && !_hiddenUIObjects.Contains(canvas.gameObject))
                    {
                        canvas.gameObject.SetActive(false);
                        _hiddenUIObjects.Add(canvas.gameObject);
                        Debug.Log($"[SimpleEraIntroController] Hidden gameplay canvas: {canvas.name}");
                    }
                }
            }

            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                if (canvas == null || canvas == _uiCanvas)
                    continue;

                if (canvas.gameObject.activeSelf && !_hiddenUIObjects.Contains(canvas.gameObject))
                {
                    if (canvas.name.Contains("UIGameplayView") || canvas.name.Contains("Gameplay"))
                    {
                        canvas.gameObject.SetActive(false);
                        _hiddenUIObjects.Add(canvas.gameObject);
                        Debug.Log($"[SimpleEraIntroController] Hidden runtime UI canvas: {canvas.name}");
                    }
                }
            }

            HideNetworkedPlayers();
        }

        private void HideNetworkedPlayers()
        {
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                if (root == null)
                    continue;

                if (root == _spawnedPlayer || root == _npcCharacter)
                    continue;

                if (root.transform.IsChildOf(transform))
                    continue;

                if (root.name.Contains("Sci_Fi_Character") && root.activeSelf && !_hiddenUIObjects.Contains(root))
                {
                    root.SetActive(false);
                    _hiddenUIObjects.Add(root);
                    Debug.Log($"[SimpleEraIntroController] Hidden networked player: {root.name}");
                }
            }
        }

        private void ShowGameUI()
        {
            foreach (var uiObject in _hiddenUIObjects)
            {
                if (uiObject != null)
                {
                    if (uiObject.name.Contains("Error") || uiObject.name.Contains("Dialog") || 
                        uiObject.name.Contains("Popup") || uiObject.name.Contains("Message"))
                    {
                        Debug.Log($"[SimpleEraIntroController] Skipping non-gameplay UI: {uiObject.name}");
                        continue;
                    }

                    if (uiObject.name.Contains("Canvas") || uiObject.name.Contains("Gameplay") || 
                        uiObject.name.Contains("UI") || uiObject.name.Contains("Sci_Fi_Character"))
                    {
                        uiObject.SetActive(true);
                        Debug.Log($"[SimpleEraIntroController] Restored UI: {uiObject.name}");
                    }
                }
            }

            _hiddenUIObjects.Clear();
        }

        private void SetActiveCamera(CinemachineVirtualCamera camera)
        {
            if (_introCam != null) _introCam.Priority = 0;
            if (_playerFollowCam != null) _playerFollowCam.Priority = 0;
            if (_dialogueCam != null) _dialogueCam.Priority = 0;
            if (_flybyCamera != null) _flybyCamera.Priority = 0;

            if (camera != null)
            {
                camera.Priority = 100;
                Debug.Log($"[SimpleEraIntroController] Set active camera: {camera.name}");
            }
            else
            {
                Debug.Log("[SimpleEraIntroController] Cleared all cutscene cameras - game camera should take over");
            }
        }

        private IEnumerator FadeFromBlack()
        {
            if (_fadeImage == null)
                yield break;

            _fadeImage.gameObject.SetActive(true);
            Color color = _fadeImage.color;
            color.a = 1f;
            _fadeImage.color = color;

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - (elapsed / _fadeDuration);
                _fadeImage.color = color;
                yield return null;
            }

            color.a = 0f;
            _fadeImage.color = color;
            _fadeImage.gameObject.SetActive(false);
        }

        private IEnumerator FadeToBlack()
        {
            if (_fadeImage == null)
                yield break;

            _fadeImage.gameObject.SetActive(true);
            Color color = _fadeImage.color;
            color.a = 0f;
            _fadeImage.color = color;

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = elapsed / _fadeDuration;
                _fadeImage.color = color;
                yield return null;
            }

            color.a = 1f;
            _fadeImage.color = color;
        }

        [ContextMenu("Force Trigger Intro (Test)")]
        public void ForceTriggerIntro()
        {
            _hasShownIntro = false;
            _hasSpokenToNPC = false;
            _hasWokenUp = false;
            PlayerPrefs.DeleteKey(PREF_LAST_ERA_SHOWN);
            PlayerPrefs.Save();
            CheckForNewEra();
            Debug.Log("[SimpleEraIntroController] Force triggered intro for testing");
        }
    }
}
