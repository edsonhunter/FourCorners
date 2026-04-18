using FourCorners.Scripts.Controller;
using UnityEngine;

namespace FourCorners.Scripts.Manager.Camera
{
    public class CameraBoundsCalculator : ICameraBoundsCalculator
    {
        private float _lastZoomCache = -999f;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private float _lastMapPaddingCache = -1f;

        private Vector3 _baseMin;
        private Vector3 _baseMax;
        private float _groundY;

        public Vector2 MapLimitX { get; private set; } = Vector2.zero;
        public Vector2 MapLimitZ { get; private set; } = Vector2.zero;

        public void Initialize(Vector3 mapMin, Vector3 mapMax)
        {
            _baseMin = mapMin;
            _baseMax = mapMax;
            _groundY = (mapMin.y + mapMax.y) * 0.5f;

            // Reset cache to force immediate recalculation when reinitialized
            _lastZoomCache = -999f;
        }

        public void CalculateDynamicBounds(UnityEngine.Camera camera, Transform cameraTransform, float mapPadding)
        {
            if (camera == null || cameraTransform == null) return;

            float currentRealZoom = camera.orthographic ? camera.orthographicSize : cameraTransform.localPosition.z;

            if (Mathf.Approximately(_lastZoomCache, currentRealZoom) && 
                _lastScreenWidth == Screen.width && 
                _lastScreenHeight == Screen.height && 
                Mathf.Approximately(_lastMapPaddingCache, mapPadding))
            {
                return;
            }

            _lastZoomCache = currentRealZoom;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastMapPaddingCache = mapPadding;

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, _groundY, 0));

            Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!groundPlane.Raycast(centerRay, out float dCenter) || dCenter < 0)
            {
                MapLimitX = new Vector2(_baseMin.x - mapPadding, _baseMax.x + mapPadding);
                MapLimitZ = new Vector2(_baseMin.z - mapPadding, _baseMax.z + mapPadding);
                return;
            }

            Vector3 centerHit = centerRay.GetPoint(dCenter);
            Vector2 rigToCenter = new Vector2(centerHit.x - cameraTransform.position.x, centerHit.z - cameraTransform.position.z);

            Vector3[] viewportCorners = {
                new Vector3(0f, 0f, 0f), 
                new Vector3(1f, 0f, 0f), 
                new Vector3(0f, 1f, 0f), 
                new Vector3(1f, 1f, 0f)  
            };

            float minViewX = float.MaxValue, maxViewX = float.MinValue;
            float minViewZ = float.MaxValue, maxViewZ = float.MinValue;

            foreach (var corner in viewportCorners)
            {
                Ray cornerRay = camera.ViewportPointToRay(corner);
                if (groundPlane.Raycast(cornerRay, out float dCorner) && dCorner > 0)
                {
                    Vector3 hit = cornerRay.GetPoint(dCorner);
                    if (hit.x < minViewX) minViewX = hit.x;
                    if (hit.x > maxViewX) maxViewX = hit.x;
                    if (hit.z < minViewZ) minViewZ = hit.z;
                    if (hit.z > maxViewZ) maxViewZ = hit.z;
                }
            }

            if (minViewX == float.MaxValue)
            {
                MapLimitX = new Vector2(_baseMin.x - mapPadding, _baseMax.x + mapPadding);
                MapLimitZ = new Vector2(_baseMin.z - mapPadding, _baseMax.z + mapPadding);
                return;
            }

            float spanLeft = centerHit.x - minViewX;
            float spanRight = maxViewX - centerHit.x;
            float spanDown = centerHit.z - minViewZ;
            float spanUp = maxViewZ - centerHit.z;

            float minCenterX = _baseMin.x - mapPadding + spanLeft;
            float maxCenterX = _baseMax.x + mapPadding - spanRight;
            float minCenterZ = _baseMin.z - mapPadding + spanDown;
            float maxCenterZ = _baseMax.z + mapPadding - spanUp;

            if (minCenterX > maxCenterX) { float mid = (minCenterX + maxCenterX) * 0.5f; minCenterX = mid; maxCenterX = mid; }
            if (minCenterZ > maxCenterZ) { float mid = (minCenterZ + maxCenterZ) * 0.5f; minCenterZ = mid; maxCenterZ = mid; }

            MapLimitX = new Vector2(minCenterX - rigToCenter.x, maxCenterX - rigToCenter.x);
            MapLimitZ = new Vector2(minCenterZ - rigToCenter.y, maxCenterZ - rigToCenter.y);
        }
    }
}
