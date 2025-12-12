using System;
using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class LiveEventManager : NetworkBehaviour
    {
        public static LiveEventManager Instance { get; private set; }
        
        [Header("Event Configuration")]
        [SerializeField] private LiveEventData[] _liveEvents;
        
        [Networked] private int CurrentEventIndex { get; set; }
        [Networked] private TickTimer EventTriggerTimer { get; set; }
        [Networked] private NetworkBool EventHasTriggered { get; set; }
        
        private LiveEventData _activeEvent;
        private double _lastSyncTime;
        private const float SYNC_INTERVAL = 10f;
        private bool _initialized = false;
        
        public event Action<LiveEventData, float> OnCountdownUpdate;
        public event Action<LiveEventData> OnEventTriggered;
        public event Action<LiveEventData, float> OnEventStarted;
        
        public LiveEventData ActiveEvent => _activeEvent;
        public bool IsEventActive => _activeEvent != null;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            Debug.Log("[LiveEventManager] Awake - Instance set");
        }
        
        public override void Spawned()
        {
            Debug.Log($"[LiveEventManager] Spawned! HasStateAuthority: {HasStateAuthority}, Runner: {Runner != null}");
            
            if (!_initialized && HasStateAuthority)
            {
                _initialized = true;
                InitializeServerEvents();
            }
            
            Runner.SetIsSimulated(Object, true);
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void InitializeServerEvents()
        {
            if (_liveEvents == null || _liveEvents.Length == 0)
            {
                Debug.LogWarning("No live events configured!");
                return;
            }
            
            CurrentEventIndex = -1;
            FindNextUpcomingEvent();
        }
        
        private void FindNextUpcomingEvent()
        {
            if (!HasStateAuthority) return;
            
            LiveEventData nextEvent = null;
            int nextIndex = -1;
            double shortestTime = double.MaxValue;
            
            for (int i = 0; i < _liveEvents.Length; i++)
            {
                if (_liveEvents[i] == null) continue;
                
                double timeUntil = _liveEvents[i].GetSecondsUntilEvent();
                
                if (timeUntil > 0 && timeUntil < shortestTime)
                {
                    shortestTime = timeUntil;
                    nextEvent = _liveEvents[i];
                    nextIndex = i;
                }
            }
            
            if (nextEvent != null)
            {
                CurrentEventIndex = nextIndex;
                _activeEvent = nextEvent;
                
                float timerDuration = (float)shortestTime;
                EventTriggerTimer = TickTimer.CreateFromSeconds(Runner, timerDuration);
                EventHasTriggered = false;
                
                Debug.Log($"[LiveEventManager] Next event '{nextEvent.EventName}' scheduled in {timerDuration:F0} seconds");
                
                OnEventStarted?.Invoke(nextEvent, timerDuration);
            }
            else
            {
                Debug.LogWarning("[LiveEventManager] No upcoming events found!");
                _activeEvent = null;
            }
        }
        
        public override void FixedUpdateNetwork()
        {
            if (!IsEventActive) return;
            
            if (HasStateAuthority)
            {
                ServerUpdateEvent();
            }
            
            ClientUpdateEvent();
        }
        
        private void ServerUpdateEvent()
        {
            double currentTime = Runner.SimulationTime;
            
            if (currentTime - _lastSyncTime >= SYNC_INTERVAL)
            {
                _lastSyncTime = currentTime;
                SyncEventTimer();
            }
            
            if (!EventHasTriggered && EventTriggerTimer.Expired(Runner))
            {
                TriggerEvent();
            }
        }
        
        private void SyncEventTimer()
        {
            if (_activeEvent == null) return;
            
            double actualTimeUntil = _activeEvent.GetSecondsUntilEvent();
            
            if (actualTimeUntil <= 0)
            {
                if (!EventHasTriggered)
                {
                    TriggerEvent();
                }
                return;
            }
            
            float currentTimerRemaining = EventTriggerTimer.RemainingTime(Runner) ?? 0f;
            float drift = Mathf.Abs(currentTimerRemaining - (float)actualTimeUntil);
            
            if (drift > 2f)
            {
                Debug.Log($"[LiveEventManager] Syncing timer. Drift: {drift:F2}s. Resetting to {actualTimeUntil:F0}s");
                EventTriggerTimer = TickTimer.CreateFromSeconds(Runner, (float)actualTimeUntil);
            }
        }
        
        private void ClientUpdateEvent()
        {
            if (_activeEvent == null)
            {
                if (CurrentEventIndex >= 0 && CurrentEventIndex < _liveEvents.Length)
                {
                    _activeEvent = _liveEvents[CurrentEventIndex];
                    
                    if (_activeEvent != null)
                    {
                        float remainingTime = EventTriggerTimer.RemainingTime(Runner) ?? 0f;
                        OnEventStarted?.Invoke(_activeEvent, remainingTime);
                    }
                }
            }
            
            if (_activeEvent != null)
            {
                float remainingTime = EventTriggerTimer.RemainingTime(Runner) ?? 0f;
                OnCountdownUpdate?.Invoke(_activeEvent, remainingTime);
            }
        }
        
        private void TriggerEvent()
        {
            if (!HasStateAuthority) return;
            
            EventHasTriggered = true;
            
            Debug.Log($"[LiveEventManager] Event '{_activeEvent.EventName}' triggered!");
            
            RPC_TriggerEventEffects();
            
            FindNextUpcomingEvent();
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_TriggerEventEffects()
        {
            if (_activeEvent == null) return;
            
            Debug.Log($"[LiveEventManager] Playing event effects for '{_activeEvent.EventName}'");
            
            OnEventTriggered?.Invoke(_activeEvent);
        }
        
        public float GetRemainingTime()
        {
            if (!IsEventActive) return 0f;
            return EventTriggerTimer.RemainingTime(Runner) ?? 0f;
        }
        
        public bool IsCountdownActive()
        {
            return IsEventActive && GetRemainingTime() > 0f;
        }
    }
}
