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

            HandleReviveInput();
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

            _nearbyDownedPlayer = closestDowned;
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

            if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
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
