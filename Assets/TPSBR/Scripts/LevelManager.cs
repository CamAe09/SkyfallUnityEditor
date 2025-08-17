using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("UI Settings")]
    public string nextSceneName = "NextLevel";

    [Header("UI Elements")]
    private Canvas gameCanvas;
    private GameObject uiPanel;
    private TextMeshProUGUI instructionText;

    [Header("Input")]
    private InputAction transitionAction;

    void Start()
    {
        SetupInput();
        CreateGameUI();
    }

    void SetupInput()
    {
        transitionAction = new InputAction("Transition", InputActionType.Button, "<Keyboard>/space");
        transitionAction.performed += OnTransitionPressed;
        transitionAction.Enable();
    }

    void OnTransitionPressed(InputAction.CallbackContext context)
    {
        LoadNextLevel();
    }

    void CreateGameUI()
    {
        CreateCanvas();
        CreateUIPanel();
        CreateInstructionText();
    }

    void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("GameCanvas");
        gameCanvas = canvasObject.AddComponent<Canvas>();
        gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    void CreateUIPanel()
    {
        GameObject panelObject = new GameObject("UIPanel");
        panelObject.transform.SetParent(gameCanvas.transform, false);

        uiPanel = panelObject;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
    }

    void CreateInstructionText()
    {
        GameObject textObject = new GameObject("InstructionText");
        textObject.transform.SetParent(uiPanel.transform, false);

        instructionText = textObject.AddComponent<TextMeshProUGUI>();
        instructionText.text = "Press SPACE to play";
        instructionText.fontSize = 48;
        instructionText.color = Color.white;
        instructionText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.4f);
        textRect.anchorMax = new Vector2(0.9f, 0.6f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    public void LoadNextLevel()
    {
        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{nextSceneName}' cannot be loaded. Make sure it's added to Build Settings.");

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.Log("No more levels available. Restarting from first scene.");
                SceneManager.LoadScene(0);
            }
        }
    }

    public void SetNextSceneName(string sceneName)
    {
        nextSceneName = sceneName;
        if (instructionText != null)
        {
            instructionText.text = "Press SPACE to play";
        }
    }

    void OnDestroy()
    {
        if (transitionAction != null)
        {
            transitionAction.performed -= OnTransitionPressed;
            transitionAction.Disable();
            transitionAction.Dispose();
        }

        if (gameCanvas != null)
        {
            Destroy(gameCanvas.gameObject);
        }
    }
}
