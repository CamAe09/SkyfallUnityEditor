using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TPSBR.UI
{
    public class UIRevivePrompt : UIWidget
    {
        [SerializeField]
        private GameObject _promptRoot;
        [SerializeField]
        private TextMeshProUGUI _promptText;
        [SerializeField]
        private Image _progressFill;
        [SerializeField]
        private TextMeshProUGUI _playerNameText;
        [SerializeField]
        private string _promptMessage = "Hold [E] to Revive";

        private ReviveInteraction _reviveInteraction;

        protected override void OnTick()
        {
            base.OnTick();

            if (_reviveInteraction == null && Context != null)
            {
                _reviveInteraction = FindObjectOfType<ReviveInteraction>();
            }

            if (_reviveInteraction == null)
            {
                HidePrompt();
                return;
            }

            var nearbyDowned = _reviveInteraction.GetNearbyDownedPlayer();
            if (nearbyDowned != null && nearbyDowned.IsDown)
            {
                ShowPrompt(nearbyDowned);
            }
            else
            {
                HidePrompt();
            }
        }

        private void ShowPrompt(ReviveSystem reviveSystem)
        {
            if (_promptRoot != null)
            {
                _promptRoot.SetActive(true);
            }

            if (_promptText != null)
            {
                _promptText.text = _promptMessage;
            }

            if (_playerNameText != null)
            {
                var player = reviveSystem.GetComponent<Player>();
                if (player != null)
                {
                    _playerNameText.text = player.Nickname;
                }
            }

            if (_progressFill != null)
            {
                float progress = reviveSystem.ReviveProgress;
                _progressFill.fillAmount = progress;
            }
        }

        private void HidePrompt()
        {
            if (_promptRoot != null)
            {
                _promptRoot.SetActive(false);
            }
        }
    }
}
