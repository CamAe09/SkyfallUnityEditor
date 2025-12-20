using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class XsollaInputSystemFixer
{
    static XsollaInputSystemFixer()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        
        if (eventSystem == null)
            return;

        var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
        var newModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        
        if (oldModule != null && newModule == null)
        {
            Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
            Undo.DestroyObjectImmediate(oldModule);
            
            EditorSceneManager.MarkSceneDirty(eventSystem.gameObject.scene);
            
            Debug.Log("Fixed EventSystem: Replaced StandaloneInputModule with InputSystemUIInputModule for New Input System compatibility.");
        }
    }

    [MenuItem("Tools/Xsolla/Fix Input System")]
    private static void FixAllEventSystems()
    {
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        
        int fixedCount = 0;
        foreach (var eventSystem in eventSystems)
        {
            var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            var newModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            
            if (oldModule != null && newModule == null)
            {
                Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
                Undo.DestroyObjectImmediate(oldModule);
                EditorSceneManager.MarkSceneDirty(eventSystem.gameObject.scene);
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"Fixed {fixedCount} EventSystem(s) to use InputSystemUIInputModule.");
        }
        else
        {
            Debug.Log("All EventSystems are already using InputSystemUIInputModule.");
        }
    }
}
