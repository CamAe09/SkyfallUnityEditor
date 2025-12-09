using UnityEngine;
using UnityEngine.UI;
using TPSBR.UI;

namespace TPSBR
{
	[RequireComponent(typeof(Button))]
	public class UIQuickMatchButton : MonoBehaviour
	{
		private Button _button;

		private void Awake()
		{
			_button = GetComponent<Button>();
			_button.onClick.AddListener(OnButtonClick);
		}

		private void OnDestroy()
		{
			if (_button != null)
			{
				_button.onClick.RemoveListener(OnButtonClick);
			}
		}

		private void OnButtonClick()
		{
			var multiplayerView = FindObjectOfType<UIMultiplayerView>();
			if (multiplayerView != null)
			{
				multiplayerView.StartQuickMatch();
			}
			else
			{
				Debug.LogWarning("UIMultiplayerView not found in scene. Make sure you're in the menu scene.");
			}
		}
	}
}
