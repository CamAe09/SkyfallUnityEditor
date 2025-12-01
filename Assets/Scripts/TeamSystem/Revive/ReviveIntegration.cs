using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class ReviveIntegration : ContextBehaviour
    {
        [SerializeField]
        private bool _enableReviveSystem = true;

        private bool _initialized;

        public override void FixedUpdateNetwork()
        {
            if (!_initialized && Context != null && Context.GameplayMode != null)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            _initialized = true;

            if (!_enableReviveSystem)
                return;

            if (Context.GameplayMode != null)
            {
                Context.GameplayMode.OnAgentDeath += OnAgentDeath;
            }

            AddReviveSystemsToPlayers();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Context != null && Context.GameplayMode != null)
            {
                Context.GameplayMode.OnAgentDeath -= OnAgentDeath;
            }

            base.Despawned(runner, hasState);
        }

        private void OnAgentDeath(KillData killData)
        {
            if (!_enableReviveSystem)
                return;

            if (Context.NetworkGame == null)
                return;

            var victim = Context.NetworkGame.GetPlayer(killData.VictimRef);
            if (victim == null)
                return;

            if (TeamManager.Instance == null)
                return;

            var teamMode = TeamManager.Instance.GetTeamMode();
            if (teamMode == TeamMode.Solo)
            {
                return;
            }

            var reviveSystem = victim.GetComponent<ReviveSystem>();
            if (reviveSystem != null && !reviveSystem.IsDown)
            {
                reviveSystem.EnterDownedState();

                var statistics = victim.Statistics;
                statistics.IsAlive = true;
                victim.UpdateStatistics(statistics);
            }
        }

        private void AddReviveSystemsToPlayers()
        {
            if (Context.NetworkGame == null || !HasStateAuthority)
                return;

            foreach (var player in Context.NetworkGame.ActivePlayers)
            {
                if (player != null && player.GetComponent<ReviveSystem>() == null)
                {
                    player.gameObject.AddComponent<ReviveSystem>();
                }
            }
        }

        public void SetReviveSystemEnabled(bool enabled)
        {
            _enableReviveSystem = enabled;
        }
    }
}
