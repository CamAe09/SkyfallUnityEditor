using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIDownedState : UIWidget
    {
        [Header("References")]
        [SerializeField]
        private GameObject _downedStateRoot;
        [SerializeField]
        private Image _healthBarFill;
        [SerializeField]
        private TextMeshProUGUI _statusText;
        [SerializeField]
        private TextMeshProUGUI _bleedOutTimerText;
        
        [Header("Being Revived")]
        [SerializeField]
        private GameObject _beingRevivedIndicator;
        [SerializeField]
        private TextMeshProUGUI _reviverNameText;
        [SerializeField]
        private Image _reviveProgressFill;
        
        [Header("Colors")]
        [SerializeField]
        private Color _bleedOutColor = new Color(0.8f, 0.1f, 0.1f);
        [SerializeField]
        private Color _revivingColor = new Color(0.1f, 0.8f, 0.1f);

        private ReviveSystem _localReviveSystem;
        private Player _localPlayer;
        private Color _originalHealthBarColor;
        private bool _wasDown = false;

        protected override void OnTick()
        {
            base.OnTick();

            UpdateLocalPlayer();

            if (_localReviveSystem == null || !_localReviveSystem.IsDown)
            {
                if (_wasDown)
                {
                    RestoreHealthBarColor();
                    _wasDown = false;
                }
                
                HideDownedState();
                return;
            }

            if (!_wasDown)
            {
                SaveHealthBarColor();
                _wasDown = true;
            }

            ShowDownedState();
            UpdateDownedState();
        }

        private void UpdateLocalPlayer()
        {
            if (_localPlayer != null && _localReviveSystem != null)
                return;

            if (Context == null || Context.NetworkGame == null)
                return;

            _localPlayer = Context.NetworkGame.GetPlayer(Context.LocalPlayerRef);
            if (_localPlayer != null)
            {
                _localReviveSystem = _localPlayer.GetComponent<ReviveSystem>();
            }
        }

        private void ShowDownedState()
        {
            if (_downedStateRoot != null)
            {
                _downedStateRoot.SetActive(true);
            }
        }

        private void HideDownedState()
        {
            if (_downedStateRoot != null)
            {
                _downedStateRoot.SetActive(false);
            }
        }

        private void SaveHealthBarColor()
        {
            if (_healthBarFill != null)
            {
                _originalHealthBarColor = _healthBarFill.color;
            }
        }

        private void RestoreHealthBarColor()
        {
            if (_healthBarFill != null)
            {
                _healthBarFill.color = _originalHealthBarColor;
            }
        }

        private void UpdateDownedState()
        {
            if (_localReviveSystem == null)
                return;

            float bleedOutTime = _localReviveSystem.BleedOutProgress;
            float bleedOutPercent = bleedOutTime / ReviveSettings.BLEED_OUT_DURATION;

            if (_healthBarFill != null)
            {
                _healthBarFill.fillAmount = bleedOutPercent;
                _healthBarFill.color = _localReviveSystem.IsBeingRevived ? _revivingColor : _bleedOutColor;
            }

            if (_bleedOutTimerText != null)
            {
                _bleedOutTimerText.text = $"{Mathf.CeilToInt(bleedOutTime)}s";
            }

            if (_localReviveSystem.IsBeingRevived)
            {
                if (_beingRevivedIndicator != null)
                {
                    _beingRevivedIndicator.SetActive(true);
                }

                var reviver = _localReviveSystem.GetRevivingPlayer();
                if (reviver != null && _reviverNameText != null)
                {
                    _reviverNameText.text = $"{reviver.Nickname} is reviving you...";
                }

                if (_reviveProgressFill != null)
                {
                    _reviveProgressFill.fillAmount = _localReviveSystem.ReviveProgress;
                }

                if (_statusText != null)
                {
                    _statusText.text = "Being Revived...";
                }
            }
            else
            {
                if (_beingRevivedIndicator != null)
                {
                    _beingRevivedIndicator.SetActive(false);
                }

                if (_statusText != null)
                {
                    _statusText.text = "You are down! Waiting for teammate...";
                }
            }
        }
    }
}
