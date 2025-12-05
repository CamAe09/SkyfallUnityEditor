using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class ReviveSystem : NetworkBehaviour, IInteraction
    {
        public Action<Player> OnPlayerDowned;
        public Action<Player, Player> OnReviveStarted;
        public Action<Player, Player> OnReviveCompleted;
        public Action<Player> OnReviveCancelled;
        public Action<Player> OnPlayerBledOut;

        [Networked]
        public ReviveData ReviveData { get; set; }

        private Player _player;
        private Agent _agent;
        private NetworkGame _networkGame;

        public bool IsDown => ReviveData.IsDown;
        public bool IsBeingRevived => ReviveData.HasRevivingPlayer;
        public float BleedOutProgress => ReviveData.BleedOutTimer.RemainingTime(Runner) ?? 0f;
        public float ReviveProgress => 1f - (ReviveData.ReviveTimer.RemainingTime(Runner) ?? ReviveSettings.REVIVE_DURATION) / ReviveSettings.REVIVE_DURATION;

        public string Name => "";
        public string Description => "";
        public Vector3 HUDPosition => Vector3.zero;
        public bool IsActive => false;

        public override void Spawned()
        {
            _player = GetComponent<Player>();
            _networkGame = Runner.SimulationUnityScene.GetComponent<NetworkGame>(true);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (!ReviveData.IsDown)
                return;

            if (ReviveData.BleedOutTimer.Expired(Runner))
            {
                OnBleedOut();
                return;
            }

            if (ReviveData.HasRevivingPlayer && ReviveData.ReviveTimer.Expired(Runner))
            {
                CompleteRevive();
            }
        }

        public void EnterDownedState()
        {
            if (!HasStateAuthority)
            {
                RPC_EnterDownedState();
                return;
            }

            if (ReviveData.IsDown)
            {
                Debug.LogWarning("[ReviveSystem] Player already downed, ignoring EnterDownedState call");
                return;
            }

            var reviveData = ReviveData;
            reviveData.IsDown = true;
            reviveData.BleedOutTimer = TickTimer.CreateFromSeconds(Runner, ReviveSettings.BLEED_OUT_DURATION);
            reviveData.HasRevivingPlayer = false;
            ReviveData = reviveData;

            Debug.Log($"[ReviveSystem] Entered downed state. Bleed-out timer set to {ReviveSettings.BLEED_OUT_DURATION} seconds");

            _agent = _player.ActiveAgent;
            if (_agent != null)
            {
                _agent.Character.CharacterController.SetActive(false);
                DisableAgentActions();
                
                if (Physics.Raycast(_agent.transform.position, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Default", "Dirt", "Wood", "Metal")))
                {
                    _agent.transform.position = hit.point + Vector3.up * 0.1f;
                }
            }

            OnPlayerDowned?.Invoke(_player);
        }

        public void StartRevive(PlayerRef reviverRef)
        {
            if (!HasStateAuthority)
            {
                RPC_StartRevive(reviverRef);
                return;
            }

            if (!ReviveData.IsDown || ReviveData.HasRevivingPlayer)
                return;

            var reviver = _networkGame?.GetPlayer(reviverRef);
            if (reviver == null)
                return;

            if (!CanRevive(reviver))
                return;

            var reviveData = ReviveData;
            reviveData.RevivingPlayer = reviverRef;
            reviveData.ReviveTimer = TickTimer.CreateFromSeconds(Runner, ReviveSettings.REVIVE_DURATION);
            reviveData.HasRevivingPlayer = true;
            ReviveData = reviveData;

            OnReviveStarted?.Invoke(_player, reviver);
        }

        public void CancelRevive()
        {
            if (!HasStateAuthority)
            {
                RPC_CancelRevive();
                return;
            }

            if (!ReviveData.HasRevivingPlayer)
                return;

            var reviveData = ReviveData;
            reviveData.HasRevivingPlayer = false;
            reviveData.RevivingPlayer = PlayerRef.None;
            ReviveData = reviveData;

            OnReviveCancelled?.Invoke(_player);
        }

        private void CompleteRevive()
        {
            var reviver = _networkGame?.GetPlayer(ReviveData.RevivingPlayer);

            var reviveData = ReviveData;
            reviveData.IsDown = false;
            reviveData.HasRevivingPlayer = false;
            reviveData.RevivingPlayer = PlayerRef.None;
            reviveData.BleedOutTimer = TickTimer.None;
            ReviveData = reviveData;

            if (_agent != null)
            {
                _agent.Character.CharacterController.SetActive(true);
                EnableAgentActions();

                var hitData = new HitData
                {
                    Action = EHitAction.Heal,
                    Amount = ReviveSettings.REVIVE_HEALTH_RESTORED,
                    Target = _agent.Health
                };
                ((IHitTarget)_agent.Health).ProcessHit(ref hitData);
            }

            var statistics = _player.Statistics;
            statistics.IsAlive = true;
            _player.UpdateStatistics(statistics);

            OnReviveCompleted?.Invoke(_player, reviver);
        }

        private void OnBleedOut()
        {
            var reviveData = ReviveData;
            reviveData.IsDown = false;
            reviveData.HasRevivingPlayer = false;
            ReviveData = reviveData;

            if (_agent != null && _agent.Health != null)
            {
                var hitData = new HitData
                {
                    Action = EHitAction.Damage,
                    Amount = 9999f,
                    Target = _agent.Health,
                    InstigatorRef = PlayerRef.None
                };
                ((IHitTarget)_agent.Health).ProcessHit(ref hitData);
            }

            OnPlayerBledOut?.Invoke(_player);
        }

        private bool CanRevive(Player reviver)
        {
            if (reviver == null || _player == null)
                return false;

            if (reviver.ActiveAgent == null || _player.ActiveAgent == null)
                return false;

            if (!reviver.IsTeammateWith(_player))
                return false;

            float distance = Vector3.Distance(
                reviver.ActiveAgent.transform.position,
                _player.ActiveAgent.transform.position
            );

            return distance <= ReviveSettings.REVIVE_INTERACTION_DISTANCE;
        }

        private void DisableAgentActions()
        {
            if (_agent == null)
                return;

            if (_agent.Weapons != null)
            {
                _agent.Weapons.DisarmCurrentWeapon();
            }
        }

        private void EnableAgentActions()
        {
            if (_agent == null)
                return;

            if (_agent.Weapons != null && _agent.Weapons.PreviousWeaponSlot > 0)
            {
                _agent.Weapons.SetPendingWeapon(_agent.Weapons.PreviousWeaponSlot);
                _agent.Weapons.ArmPendingWeapon();
            }
        }

        public Player GetRevivingPlayer()
        {
            if (!ReviveData.HasRevivingPlayer)
                return null;

            return _networkGame?.GetPlayer(ReviveData.RevivingPlayer);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_EnterDownedState()
        {
            EnterDownedState();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_StartRevive(PlayerRef reviverRef)
        {
            StartRevive(reviverRef);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_CancelRevive()
        {
            CancelRevive();
        }
    }
}
