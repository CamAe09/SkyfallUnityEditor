using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

namespace TPSBR
{
    public class SimpleMapSystem : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private Sprite _mapImage;
        [SerializeField] private Vector2 _mapWorldSize = new Vector2(100f, 100f);
        [SerializeField] private Vector2 _mapWorldCenter = Vector2.zero;

        [Header("UI References")]
        [SerializeField] private GameObject _mapPanel;
        [SerializeField] private Image _mapDisplay;
        [SerializeField] private RectTransform _playerMarker;
        [SerializeField] private GameObject _poiMarkerPrefab;
        [SerializeField] private Transform _poiMarkersContainer;

        private bool _isMapOpen = false;
        private Transform _playerTransform;
        private readonly List<MapPOI> _pois = new List<MapPOI>();
        private readonly Dictionary<MapPOI, RectTransform> _poiMarkers = new Dictionary<MapPOI, RectTransform>();

        private void Start()
        {
            SetupMap();
            FindPOIs();

            if (_mapPanel != null)
            {
                _mapPanel.SetActive(false);
            }
        }

        private void SetupMap()
        {
            if (_mapDisplay != null && _mapImage != null)
            {
                _mapDisplay.sprite = _mapImage;
            }
        }

        private void FindPOIs()
        {
            _pois.Clear();
            _pois.AddRange(FindObjectsByType<MapPOI>(FindObjectsSortMode.None));
            CreatePOIMarkers();
        }

        private void CreatePOIMarkers()
        {
            if (_poiMarkerPrefab == null || _poiMarkersContainer == null)
                return;

            foreach (var kvp in _poiMarkers)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _poiMarkers.Clear();

            foreach (var poi in _pois)
            {
                GameObject markerObj = Instantiate(_poiMarkerPrefab, _poiMarkersContainer);
                RectTransform markerRect = markerObj.GetComponent<RectTransform>();

                if (markerRect != null)
                {
                    Text markerText = markerObj.GetComponent<Text>();
                    if (markerText != null)
                    {
                        markerText.text = poi.POIName;
                        markerText.color = poi.POIColor;
                    }
                    else
                    {
                        TextMeshProUGUI markerTMP = markerObj.GetComponent<TextMeshProUGUI>();
                        if (markerTMP != null)
                        {
                            markerTMP.text = poi.POIName;
                            markerTMP.color = poi.POIColor;
                        }
                    }

                    _poiMarkers[poi] = markerRect;
                }
            }
        }

        private void Update()
        {
            HandleInput();

            if (_isMapOpen)
            {
                UpdateMarkers();
            }
        }

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                ToggleMap();
            }
        }

        public void ToggleMap()
        {
            _isMapOpen = !_isMapOpen;

            if (_mapPanel != null)
            {
                _mapPanel.SetActive(_isMapOpen);
            }
        }

        private void UpdateMarkers()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
            }

            if (_playerTransform != null && _playerMarker != null)
            {
                UpdatePlayerMarker();
            }

            UpdatePOIMarkers();
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
        }

        private void UpdatePlayerMarker()
        {
            if (_playerTransform == null || _playerMarker == null || _mapDisplay == null)
                return;

            Vector2 mapPosition = WorldToMapPosition(_playerTransform.position);
            _playerMarker.anchoredPosition = mapPosition;

            float rotation = -_playerTransform.eulerAngles.y;
            _playerMarker.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        private void UpdatePOIMarkers()
        {
            foreach (var kvp in _poiMarkers)
            {
                MapPOI poi = kvp.Key;
                RectTransform marker = kvp.Value;

                if (poi != null && marker != null)
                {
                    Vector2 mapPosition = WorldToMapPosition(poi.transform.position);
                    marker.anchoredPosition = mapPosition;
                }
            }
        }

        private Vector2 WorldToMapPosition(Vector3 worldPosition)
        {
            if (_mapDisplay == null)
                return Vector2.zero;

            float normalizedX = (worldPosition.x - _mapWorldCenter.x + _mapWorldSize.x / 2f) / _mapWorldSize.x;
            float normalizedZ = (worldPosition.z - _mapWorldCenter.y + _mapWorldSize.y / 2f) / _mapWorldSize.y;

            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedZ = Mathf.Clamp01(normalizedZ);

            RectTransform mapRect = _mapDisplay.GetComponent<RectTransform>();
            float displayWidth = mapRect.rect.width;
            float displayHeight = mapRect.rect.height;

            float x = (normalizedX - 0.5f) * displayWidth;
            float y = (normalizedZ - 0.5f) * displayHeight;

            return new Vector2(x, y);
        }
    }
}
