using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class InputSystemFixer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void FixEventSystem()
    {
        var eventSystem = FindFirstObjectByType<EventSystem>();
        
        if (eventSystem == null)
            return;

        var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
        {
            var newModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            DestroyImmediate(oldModule);
            Debug.Log("Replaced StandaloneInputModule with InputSystemUIInputModule");
        }
    }
}
