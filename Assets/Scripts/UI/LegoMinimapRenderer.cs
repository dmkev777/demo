using UnityEngine;
using UnityEngine.Rendering;

namespace Lego2KDrive
{
    /// <summary>
    /// Top-right orthographic Minimap with Garage [G], Race [R], Player marker, and [M] zoom toggle.
    /// </summary>
    public class LegoMinimapRenderer : MonoBehaviour
    {
        public static LegoMinimapRenderer Instance { get; private set; }

        public bool IsZoomedOutOverview { get; private set; }
        public float CurrentOrthoSize => IsZoomedOutOverview ? 215f : 72f;

        private Camera _minimapCamera;
        private RenderTexture _renderTex;
        private Texture2D _solidTex;
        private Vector3 _focusWorldPos = Vector3.zero;

        private void Awake()
        {
            Instance = this;
            _solidTex = new Texture2D(1, 1);
            _solidTex.SetPixel(0, 0, Color.white);
            _solidTex.Apply();

            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                _renderTex = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                _renderTex.Create();

                GameObject camObj = new GameObject("MinimapOrthoCamera");
                camObj.transform.SetParent(transform, false);
                camObj.transform.position = new Vector3(0f, 220f, 0f);
                camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                _minimapCamera = camObj.AddComponent<Camera>();
                _minimapCamera.orthographic = true;
                _minimapCamera.orthographicSize = CurrentOrthoSize;
                _minimapCamera.nearClipPlane = 5f;
                _minimapCamera.farClipPlane = 450f;
                _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
                _minimapCamera.backgroundColor = new Color(0.22f, 0.36f, 0.22f, 1f);
                _minimapCamera.targetTexture = _renderTex;
                _minimapCamera.depth = -10f;
            }
        }

        private void OnDestroy()
        {
            if (_renderTex != null)
            {
                if (_minimapCamera != null) _minimapCamera.targetTexture = null;
                _renderTex.Release();
                Destroy(_renderTex);
            }
            if (_solidTex != null) Destroy(_solidTex);
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool garageOpen = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen;
            bool holdingM = !garageOpen && LegoGameInput.GameplayAllowed && Input.GetKey(KeyCode.M);
            if (holdingM != IsZoomedOutOverview)
            {
                SetOverviewMode(holdingM);
            }
        }

        public void SetOverviewMode(bool expanded)
        {
            IsZoomedOutOverview = expanded;
            if (_minimapCamera != null)
                _minimapCamera.orthographicSize = CurrentOrthoSize;
        }

        public void ToggleZoom()
        {
            SetOverviewMode(!IsZoomedOutOverview);
        }

        public void UpdateTrackingTarget(Vector3 playerWorldPos)
        {
            _focusWorldPos = new Vector3(playerWorldPos.x, 0f, playerWorldPos.z);
            if (_minimapCamera != null)
            {
                _minimapCamera.transform.position = IsZoomedOutOverview
                    ? new Vector3(0f, 220f, 10f)
                    : new Vector3(playerWorldPos.x, 220f, playerWorldPos.z);
                _minimapCamera.orthographicSize = CurrentOrthoSize;
            }
        }

        public void DrawMinimapGUI(Vector3 playerPos, float playerYaw, Vector3 garagePos, Vector3 raceEntryPos)
        {
            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
                return;

            UpdateTrackingTarget(playerPos);

            Rect outer;
            Rect inner;
            float pinScale = IsZoomedOutOverview ? 1.4f : 1.0f;

            if (IsZoomedOutOverview)
            {
                DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.04f, 0.08f, 0.52f));

                float mapSize = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.72f, 360f, 640f);
                outer = new Rect((Screen.width - mapSize) * 0.5f, (Screen.height - mapSize) * 0.5f - 10f, mapSize, mapSize);
                inner = new Rect(outer.x + 6f, outer.y + 34f, mapSize - 12f, mapSize - 40f);

                DrawRect(outer, new Color(0.08f, 0.11f, 0.15f, 0.96f));
                DrawRect(new Rect(outer.x, outer.y, outer.width, 4f), LegoPalette.CyanGlow);

                GUIStyle hdrStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                hdrStyle.normal.textColor = LegoPalette.BrightYellow;
                GUI.Label(new Rect(outer.x, outer.y + 4f, outer.width, 28f), "BAN DO TOAN CANH MOTORVERSE [Tha M de thu nho]", hdrStyle);
            }
            else
            {
                float mapSize = 154f;
                outer = new Rect(16f, 14f, mapSize, mapSize);
                inner = new Rect(outer.x + 4f, outer.y + 4f, mapSize - 8f, mapSize - 8f);

                DrawRect(outer, new Color(0.08f, 0.11f, 0.15f, 0.94f));
                DrawRect(new Rect(outer.x, outer.y, outer.width, 3f), LegoPalette.CyanGlow);
            }

            if (_renderTex != null)
                GUI.DrawTexture(inner, _renderTex, ScaleMode.StretchToFill, false);
            else
                DrawRect(inner, new Color(0.22f, 0.36f, 0.22f, 1f));

            Vector2 centerWorld = IsZoomedOutOverview
                ? new Vector2(0f, 10f)
                : new Vector2(_focusWorldPos.x, _focusWorldPos.z);
            float halfSpan = CurrentOrthoSize;

            DrawMapPin(inner, centerWorld, halfSpan, raceEntryPos, "R", LegoPalette.BrightYellow, LegoPalette.CarbonBlack, pinScale);
            DrawMapPin(inner, centerWorld, halfSpan, garagePos, "G", LegoPalette.BrightOrange, Color.white, pinScale);
            DrawPlayerMarker(inner, centerWorld, halfSpan, playerPos, playerYaw, pinScale);

            if (IsZoomedOutOverview)
            {
                GUIStyle legendStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                legendStyle.normal.textColor = Color.white;
                Rect legend = new Rect(outer.x, outer.yMax + 4f, outer.width, 24f);
                DrawRect(legend, new Color(0.08f, 0.11f, 0.15f, 0.92f));
                GUI.Label(legend, "[G] Garage Thiet Ke   |   [R] Vong Dang Ky Dua   |   [Xanh] Vi Tri Cua Ban", legendStyle);
            }
            else
            {
                GUIStyle tagStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                tagStyle.normal.textColor = Color.white;
                Rect badge = new Rect(outer.x, outer.yMax + 2f, outer.width, 18f);
                DrawRect(badge, new Color(0.08f, 0.11f, 0.15f, 0.85f));
                GUI.Label(badge, "[Giu M] Phong To Ban Do", tagStyle);
            }
        }

        private Vector2 WorldToMapPixel(Rect mapRect, Vector2 centerXZ, float halfSpan, Vector3 worldPos)
        {
            float nx = Mathf.Clamp((worldPos.x - centerXZ.x) / halfSpan, -0.92f, 0.92f);
            float nz = Mathf.Clamp((worldPos.z - centerXZ.y) / halfSpan, -0.92f, 0.92f);
            float px = mapRect.center.x + nx * (mapRect.width * 0.5f);
            float py = mapRect.center.y - nz * (mapRect.height * 0.5f);
            return new Vector2(px, py);
        }

        private void DrawMapPin(Rect mapRect, Vector2 centerXZ, float halfSpan, Vector3 worldPos, string letter, Color bgCol, Color txtCol, float scale = 1f)
        {
            Vector2 p = WorldToMapPixel(mapRect, centerXZ, halfSpan, worldPos);
            float size = 16f * scale;
            Rect r = new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size);
            DrawRect(new Rect(r.x - 1f, r.y - 1f, size + 2f, size + 2f), LegoPalette.CarbonBlack);
            DrawRect(r, bgCol);

            GUIStyle st = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(10f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.textColor = txtCol;
            GUI.Label(r, letter, st);
        }

        private void DrawPlayerMarker(Rect mapRect, Vector2 centerXZ, float halfSpan, Vector3 playerPos, float yawDeg, float scale = 1f)
        {
            Vector2 p = WorldToMapPixel(mapRect, centerXZ, halfSpan, playerPos);
            float outerSz = 12f * scale;
            float innerSz = 8f * scale;
            DrawRect(new Rect(p.x - outerSz * 0.5f, p.y - outerSz * 0.5f, outerSz, outerSz), Color.white);
            DrawRect(new Rect(p.x - innerSz * 0.5f, p.y - innerSz * 0.5f, innerSz, innerSz), LegoPalette.CyanGlow);

            float rad = yawDeg * Mathf.Deg2Rad;
            Vector2 tip = p + new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad)) * (9f * scale);
            float tipSz = 4f * scale;
            DrawRect(new Rect(tip.x - tipSz * 0.5f, tip.y - tipSz * 0.5f, tipSz, tipSz), LegoPalette.BrightYellow);
        }

        private void DrawRect(Rect rect, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            if (_solidTex != null) GUI.DrawTexture(rect, _solidTex);
            GUI.color = prev;
        }
    }
}
