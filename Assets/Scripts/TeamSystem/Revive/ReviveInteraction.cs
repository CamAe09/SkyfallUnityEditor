using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSBR
{
    public class ReviveInteraction : ContextBehaviour
    {
        [SerializeField]
        private LayerMask _playerLayer;
        [SerializeField]
        private float _checkInterval = 0.2f;

        private Agent _localAgent;
        private Player _localPlayer;
        private ReviveSystem _nearbyDownedPlayer;
        private float _checkTimer;
        private bool _isReviving;
        private InputAction _reviveAction;
        private GUIStyle _guiStyle;

        private void Awake()
        {
            _guiStyle = new GUIStyle();
            _guiStyle.fontSize = 32;
            _guiStyle.fontStyle = FontStyle.Bold;
            _guiStyle.alignment = TextAnchor.MiddleCenter;
            _guiStyle.normal.textColor = Color.white;
        }

        private void Update()
        {
            if (Context == null || Context.Runner == null)
                return;

            UpdateLocalPlayer();

            if (_localAgent == null || _localPlayer == null)
                return;

            _checkTimer += Time.deltaTime;
            if (_checkTimer >= _checkInterval)
            {
                _checkTimer = 0f;
                CheckForDownedPlayers();
            }

            if (Keyboard.current != null)
            {
                HandleReviveInput();
            }
        }

        private void OnGUI()
        {
            if (_nearbyDownedPlayer != null && _nearbyDownedPlayer.IsDown && _localPlayer != null)
            {
                var downedPlayer = _nearbyDownedPlayer.GetComponent<Player>();
                if (downedPlayer != null)
                {
                    string message = _isReviving 
                        ? $"Reviving {downedPlayer.Nickname}... {(_nearbyDownedPlayer.ReviveProgress * 100f):F0}%"
                        : $"Hold [U] to Revive {downedPlayer.Nickname}";
                    
                    var rect = new Rect(Screen.width / 2 - 300, Screen.height * 0.35f, 600, 100);
                    
                    _guiStyle.normal.textColor = Color.black;
                    GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), message, _guiStyle);
                    
                    _guiStyle.normal.textColor = _isReviving ? Color.green : Color.yellow;
                    GUI.Label(rect, message, _guiStyle);
                }
            }
        }

        private void UpdateLocalPlayer()
        {
            if (_localPlayer != null && _localAgent != null)
                return;

            if (Context != null && Context.NetworkGame != null)
            {
                _localPlayer = Context.NetworkGame.GetPlayer(Context.LocalPlayerRef);
                if (_localPlayer != null)
                {
                    _localAgent = _localPlayer.ActiveAgent;
                }
            }
        }

        private void CheckForDownedPlayers()
        {
            if (_localAgent == null)
            {
                _nearbyDownedPlayer = null;
                return;
            }

            Collider[] colliders = Physics.OverlapSphere(
                _localAgent.transform.position,
                ReviveSettings.REVIVE_INTERACTION_DISTANCE,
                _playerLayer
            );

            Debug.Log($"[ReviveInteraction] Checking - Found {colliders.Length} colliders. LayerMask={_playerLayer.value}, Position={_localAgent.transform.position}");

            ReviveSystem closestDowned = null;
            float closestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                var reviveSystem = collider.GetComponentInParent<ReviveSystem>();
                if (reviveSystem == null || !reviveSystem.IsDown)
                    continue;

                var downedPlayer = reviveSystem.GetComponent<Player>();
                if (downedPlayer == null || downedPlayer == _localPlayer)
                    continue;

                if (!_localPlayer.IsTeammateWith(downedPlayer))
                    continue;

                if (reviveSystem.IsBeingRevived && reviveSystem.GetRevivingPlayer() != _localPlayer)
                    continue;

                float distance = Vector3.Distance(_localAgent.transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDowned = reviveSystem;
                }
            }

            if (closestDowned != _nearbyDownedPlayer)
            {
                _nearbyDownedPlayer = closestDowned;
                if (_nearbyDownedPlayer != null)
                {
                    Debug.Log($"[ReviveInteraction] Found nearby downed player at distance {closestDistance}");
                }
            }
        }

        private void HandleReviveInput()
        {
            if (_nearbyDownedPlayer == null)
            {
                if (_isReviving)
                {
                    StopReviving();
                }
                return;
            }

            if (Keyboard.current != null && Keyboard.current.uKey.isPressed)
            {
                if (!_isReviving)
                {
                    StartReviving();
                }
            }
            else
            {
                if (_isReviving)
                {
                    StopReviving();
                }
            }
        }

        private void StartReviving()
        {
            if (_nearbyDownedPlayer == null || _localPlayer == null)
                return;

            _isReviving = true;
            _nearbyDownedPlayer.StartRevive(_localPlayer.Object.InputAuthority);
        }

        private void StopReviving()
        {
            if (_nearbyDownedPlayer == null)
                return;

            _isReviving = false;
            _nearbyDownedPlayer.CancelRevive();
        }

        public ReviveSystem GetNearbyDownedPlayer()
        {
            return _nearbyDownedPlayer;
        }

        public bool IsReviving()
        {
            return _isReviving;
        }

        private void OnDisable()
        {
            if (_isReviving)
            {
                StopReviving();
            }
        }
    }
}
