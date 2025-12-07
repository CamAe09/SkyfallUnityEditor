using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFixer : MonoBehaviour
{
    [Tooltip("Force camera to render all layers")]
    public bool forceRenderAllLayers = true;
    
    [Tooltip("Override far clip plane")]
    public bool overrideFarClipPlane = true;
    
    [Tooltip("Far clip plane distance")]
    public float farClipPlane = 90000f;
    
    [Tooltip("Ensure Agent layer is always rendered")]
    public bool ensureAgentLayer = true;
    
    private Camera _camera;
    private int _agentLayer = -1;
    
    void Awake()
    {
        _camera = GetComponent<Camera>();
        _agentLayer = LayerMask.NameToLayer("Agent");
        
        if (_agentLayer == -1)
        {
            Debug.LogWarning("CameraFixer: Agent layer not found!");
        }
    }
    
    void LateUpdate()
    {
        if (_camera == null)
            return;
            
        if (forceRenderAllLayers && _camera.cullingMask == 0)
        {
            _camera.cullingMask = -1;
        }
        
        if (ensureAgentLayer && _agentLayer != -1)
        {
            int agentLayerMask = 1 << _agentLayer;
            if ((_camera.cullingMask & agentLayerMask) == 0)
            {
                _camera.cullingMask |= agentLayerMask;
                Debug.Log($"CameraFixer: Added Agent layer to camera culling mask. New mask: {_camera.cullingMask}");
            }
        }
        
        if (overrideFarClipPlane)
        {
            _camera.farClipPlane = farClipPlane;
        }
    }
}
