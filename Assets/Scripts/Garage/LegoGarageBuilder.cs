using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    public struct GaragePartDef
    {
        public PartCategory category;
        public string name;
        public Vector3Int size;
        public BrickShape shape;

        public GaragePartDef(PartCategory cat, string partName, Vector3Int sz, BrickShape shp)
        {
            category = cat;
            name = partName;
            size = sz;
            shape = shp;
        }
    }

    /// <summary>
    /// Interactive 3D Motorverse Workshop & Car Brick Builder.
    /// Enforces connectivity validation (no floating bricks), computes aerodynamic stats,
    /// supports selectable wheel sizes, and manages the fenced Test Drive Arena workflow.
    /// </summary>
    public class LegoGarageBuilder : MonoBehaviour
    {
        public static LegoGarageBuilder Instance { get; private set; }

        public bool IsGarageOpen { get; private set; }
        public bool IsInTestDrive { get; private set; }
        public bool IsCarInspectView { get; private set; } = false;
        public bool IsHoldingBrick => _selectedPartIdx >= 0;
        public string LastStatusMessage { get; private set; } = "";
        public Vector3Int AimedFaceNormal { get; private set; } = Vector3Int.up;
        public int AimedBrickIndex { get; private set; } = -1;
        public ArcadeKartController playerKart;

        public WheelSizeTier CurrentWheelSizeTier =>
            (playerKart != null && playerKart.Visuals != null && playerKart.Visuals.Loadout != null)
                ? playerKart.Visuals.Loadout.wheelSizeTier
                : WheelSizeTier.Medium;

        private PartCategory _activeCategory = PartCategory.Body;
        private Vector3Int _cursorGridPos = new Vector3Int(0, 3, 0);
        private int _selectedColorIdx = 4; // Vibrant Orange default like Image 1
        private int _selectedPartIdx = -1; // -1 = Tay Không / Inspection & Delete mode
        private bool _rotated90 = false;
        private bool _mirrorSymmetry = false;
        private bool _emissiveGlow = false;
        private bool _isPlacementValid = true;
        private float _statusTimer = 0f;

        private GameObject _cursorGhostObj;
        private GameObject _mirrorGhostObj;
        private GameObject _aimedFaceHighlightObj;
        private Vector2 _rightClickDownPos;
        private Texture2D _solidBgTex;

        private Rect _topLeftPanelRect;
        private Rect _topRightPanelRect;
        private Rect _bottomDockRect;

        private readonly GaragePartDef[] _partCatalog = new GaragePartDef[]
        {
            // 0..2: Khung xe (Chassis)
            new GaragePartDef(PartCategory.Chassis, "1x2 Khung",     new Vector3Int(1, 2, 2), BrickShape.StandardBrick),
            new GaragePartDef(PartCategory.Chassis, "2x4 Dam",       new Vector3Int(2, 2, 4), BrickShape.StandardBrick),
            new GaragePartDef(PartCategory.Chassis, "1x4 Thanh",     new Vector3Int(1, 2, 4), BrickShape.StandardBrick),
            // 3..6: Vỏ xe (Body)
            new GaragePartDef(PartCategory.Body,    "Mui Doc 2x2",   new Vector3Int(2, 2, 2), BrickShape.SlopeFront),
            new GaragePartDef(PartCategory.Body,    "Vanh Banh",     new Vector3Int(2, 2, 3), BrickShape.FenderArch),
            new GaragePartDef(PartCategory.Body,    "Op Hong",       new Vector3Int(1, 2, 4), BrickShape.SideSkirt),
            new GaragePartDef(PartCategory.Body,    "Tam Phang 2x4", new Vector3Int(2, 1, 4), BrickShape.FlatTile),
            // 7..10: Khí động (Aero)
            new GaragePartDef(PartCategory.Aero,    "Canh Gio",      new Vector3Int(4, 2, 2), BrickShape.SpoilerWing),
            new GaragePartDef(PartCategory.Aero,    "Chia Gio",      new Vector3Int(4, 1, 2), BrickShape.FrontSplitter),
            new GaragePartDef(PartCategory.Aero,    "Hut Gio Noc",   new Vector3Int(2, 1, 2), BrickShape.RoofScoop),
            new GaragePartDef(PartCategory.Aero,    "Kinh Chan Gio", new Vector3Int(2, 2, 3), BrickShape.Windshield),
            // 11..12: Nội thất (Interior)
            new GaragePartDef(PartCategory.Interior,"Ghe The Thao",  new Vector3Int(2, 2, 2), BrickShape.SportSeat),
            new GaragePartDef(PartCategory.Interior,"Den LED 1x1",   new Vector3Int(1, 1, 1), BrickShape.FlatTile),
            // 13..14: Động cơ (Engine)
            new GaragePartDef(PartCategory.Engine,  "Dong Co V8",    new Vector3Int(2, 2, 2), BrickShape.V8Engine),
            new GaragePartDef(PartCategory.Engine,  "Ong Xa Phan Luc",new Vector3Int(2, 2, 2), BrickShape.JetNozzle)
        };

        private static readonly string[] _categoryNames =
        {
            "Khung xe", "Vo xe", "Khi dong", "Noi that", "Dong co"
        };

        private void Awake()
        {
            Instance = this;
            _solidBgTex = new Texture2D(1, 1);
            _solidBgTex.SetPixel(0, 0, Color.white);
            _solidBgTex.Apply();
        }

        public Vector3Int GetCurrentPartSize()
        {
            if (_selectedPartIdx < 0 || _selectedPartIdx >= _partCatalog.Length)
                return new Vector3Int(2, 2, 2);

            Vector3Int s = _partCatalog[_selectedPartIdx].size;
            return _rotated90 ? new Vector3Int(s.z, s.y, s.x) : s;
        }

        public void SetSelectedPart(int partIndex)
        {
            _selectedPartIdx = partIndex;
            UpdateCursorGhostMesh();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.4f);
        }

        public void SelectPartInActiveCategory(int slotIndex)
        {
            int count = 0;
            for (int i = 0; i < _partCatalog.Length; i++)
            {
                if (_partCatalog[i].category != _activeCategory) continue;
                if (count == slotIndex)
                {
                    SetSelectedPart(i);
                    return;
                }
                count++;
            }
        }

        public void CycleActiveCategory(int delta)
        {
            int len = _categoryNames.Length;
            int next = (((int)_activeCategory + delta) % len + len) % len;
            _activeCategory = (PartCategory)next;
            SelectPartInActiveCategory(0);
        }

        public void ClearSelectedPartToNull()
        {
            SetSelectedPart(-1);
        }

        public void NotifyMustVisitGarageZone()
        {
            ShowStatusMessage("Hay di bo den cong Garage (vong sang xanh) de mo Phong Thiet Ke!", 3.2f);
        }

        private void ShowStatusMessage(string msg, float duration = 2.6f)
        {
            LastStatusMessage = msg;
            _statusTimer = duration;
        }

        public void SetWheelSizeTier(WheelSizeTier tier)
        {
            if (playerKart == null || playerKart.Visuals == null || playerKart.Visuals.Loadout == null) return;
            playerKart.Visuals.Loadout.wheelSizeTier = tier;
            playerKart.Visuals.Loadout.SaveToPrefs();
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.ResetPoseForGarage();
            ShowStatusMessage($"Da doi co banh xe: {tier}", 1.8f);
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        public void ToggleGarage()
        {
            if (IsInTestDrive)
            {
                ExitTestDriveToGarage();
                return;
            }

            if (IsGarageOpen)
                CloseGarageToOnFoot();
            else
                OpenGarageWorkshop();
        }

        public void ToggleCarInspectView()
        {
            SetCarInspectView(!IsCarInspectView);
        }

        public void SetCarInspectView(bool inspect)
        {
            IsCarInspectView = inspect;
            UpdateCursorGhostMesh();
            UpdateGhostTransforms();
            if (LegoGameInput.Instance != null)
                LegoGameInput.Instance.RefreshState();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.5f);
        }

        public void OpenGarageWorkshop()
        {
            IsGarageOpen = true;
            IsInTestDrive = false;
            IsCarInspectView = false;
            _activeCategory = PartCategory.Body;
            _selectedPartIdx = 3;

            if (LegoRaceManagerAndHUD.Instance != null)
                LegoRaceManagerAndHUD.Instance.StageCarInsideGarageWorkshop();

            if (playerKart != null)
            {
                playerKart.gameObject.SetActive(true);
                LegoMeshAndMaterialFactory.SetVelocity(playerKart.Rb, Vector3.zero);
                LegoMeshAndMaterialFactory.SetAngularVelocity(playerKart.Rb, Vector3.zero);
                playerKart.Visuals.ResetPoseForGarage();
            }

            EnsureCursorGhosts();
            EnsureAimedFaceHighlight();
            UpdateCursorGhostMesh();
            UpdateGhostTransforms();
            if (LegoGameInput.Instance != null) LegoGameInput.Instance.RefreshState();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        public void CloseGarageToOnFoot()
        {
            if (playerKart != null && playerKart.Visuals != null && playerKart.Visuals.Loadout != null)
                playerKart.Visuals.Loadout.SaveToPrefs();

            IsGarageOpen = false;
            IsInTestDrive = false;
            IsCarInspectView = false;
            if (_cursorGhostObj != null) _cursorGhostObj.SetActive(false);
            if (_mirrorGhostObj != null) _mirrorGhostObj.SetActive(false);
            HideAimedFaceHighlight();

            if (LegoRaceManagerAndHUD.Instance != null)
                LegoRaceManagerAndHUD.Instance.ExitGarageToDoorOnFoot();

            if (LegoGameInput.Instance != null) LegoGameInput.Instance.RefreshState();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        public void ForceExitAllGarageModes()
        {
            IsGarageOpen = false;
            IsInTestDrive = false;
            IsCarInspectView = false;
            _selectedPartIdx = -1;
            if (_cursorGhostObj != null) _cursorGhostObj.SetActive(false);
            if (_mirrorGhostObj != null) _mirrorGhostObj.SetActive(false);
            HideAimedFaceHighlight();
        }

        public void StartTestDrive()
        {
            if (playerKart == null) return;

            if (playerKart.Visuals != null && playerKart.Visuals.Loadout != null)
                playerKart.Visuals.Loadout.SaveToPrefs();

            IsGarageOpen = false;
            IsInTestDrive = true;
            IsCarInspectView = false;
            _selectedPartIdx = -1;
            if (_cursorGhostObj != null) _cursorGhostObj.SetActive(false);
            if (_mirrorGhostObj != null) _mirrorGhostObj.SetActive(false);
            HideAimedFaceHighlight();

            if (LegoRaceManagerAndHUD.Instance != null)
                LegoRaceManagerAndHUD.Instance.EnterTestDriveArenaMode();

            ShowStatusMessage("CHAY THU: [WASD] Lai | [Shift] Drift | Keo Chuot: Xoay Camera | [TAB]: Quay ve Garage", 4.0f);
            if (LegoGameInput.Instance != null) LegoGameInput.Instance.RefreshState();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBoost();
        }

        public void ExitTestDriveToGarage()
        {
            IsInTestDrive = false;
            if (playerKart != null)
            {
                LegoMeshAndMaterialFactory.SetVelocity(playerKart.Rb, Vector3.zero);
                LegoMeshAndMaterialFactory.SetAngularVelocity(playerKart.Rb, Vector3.zero);
            }
            OpenGarageWorkshop();
        }

        private void EnsureCursorGhosts()
        {
            if (_cursorGhostObj == null)
            {
                _cursorGhostObj = new GameObject("GarageBrickCursor");
                _cursorGhostObj.AddComponent<MeshFilter>();
                _cursorGhostObj.AddComponent<MeshRenderer>();
            }
            if (_mirrorGhostObj == null)
            {
                _mirrorGhostObj = new GameObject("GarageMirrorCursor");
                _mirrorGhostObj.AddComponent<MeshFilter>();
                _mirrorGhostObj.AddComponent<MeshRenderer>();
            }
        }

        private void EnsureAimedFaceHighlight()
        {
            if (_aimedFaceHighlightObj != null) return;
            GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh cubeMesh = prim.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(prim);

            _aimedFaceHighlightObj = new GameObject("GarageAimedFaceHighlight");
            MeshFilter mf = _aimedFaceHighlightObj.AddComponent<MeshFilter>();
            mf.sharedMesh = cubeMesh;
            MeshRenderer mr = _aimedFaceHighlightObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = LegoMeshAndMaterialFactory.GetHologramMaterial(true);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _aimedFaceHighlightObj.SetActive(false);
        }

        private void HideAimedFaceHighlight()
        {
            if (_aimedFaceHighlightObj != null)
                _aimedFaceHighlightObj.SetActive(false);
            AimedBrickIndex = -1;
        }

        private void UpdateCursorGhostMesh()
        {
            EnsureCursorGhosts();
            if (!IsGarageOpen || !IsHoldingBrick || playerKart == null)
            {
                if (_cursorGhostObj != null) _cursorGhostObj.SetActive(false);
                if (_mirrorGhostObj != null) _mirrorGhostObj.SetActive(false);
                return;
            }

            _cursorGhostObj.SetActive(true);
            Vector3Int sz = GetCurrentPartSize();
            BrickShape shp = _partCatalog[_selectedPartIdx].shape;
            Mesh mesh = LegoMeshAndMaterialFactory.GetBrickMesh(sz.x, sz.y, sz.z, shp);

            List<LegoBrickSpec> list = playerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);
            LegoBrickSpec candidate = new LegoBrickSpec(_cursorGridPos, sz, LegoPalette.GarageColors[_selectedColorIdx], shp, false, _emissiveGlow);
            _isPlacementValid = IsBrickPlacementConnected(candidate, list);

            Material holoMat = LegoMeshAndMaterialFactory.GetHologramMaterial(_isPlacementValid);
            _cursorGhostObj.GetComponent<MeshFilter>().sharedMesh = mesh;
            _cursorGhostObj.GetComponent<MeshRenderer>().sharedMaterial = holoMat;

            if (_mirrorGhostObj != null)
            {
                _mirrorGhostObj.GetComponent<MeshFilter>().sharedMesh = mesh;
                _mirrorGhostObj.GetComponent<MeshRenderer>().sharedMaterial = holoMat;
            }
        }

        private void Update()
        {
            if (_statusTimer > 0f)
                _statusTimer -= Time.unscaledDeltaTime;

            if (!IsGarageOpen || playerKart == null) return;

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.B))
                ToggleCarInspectView();

            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
                ClearSelectedPartToNull();
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                SelectPartInActiveCategory(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                SelectPartInActiveCategory(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                SelectPartInActiveCategory(2);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                SelectPartInActiveCategory(3);
            if (Input.GetKeyDown(KeyCode.Z))
                CycleActiveCategory(-1);
            if (Input.GetKeyDown(KeyCode.C))
                CycleActiveCategory(1);
            if (Input.GetKeyDown(KeyCode.F))
            {
                _selectedColorIdx = (_selectedColorIdx + 1) % LegoPalette.GarageColors.Length;
                UpdateCursorGhostMesh();
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayBrickClick(0.35f);
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                WheelSizeTier nextTier = (WheelSizeTier)(((int)CurrentWheelSizeTier + 1) % 3);
                SetWheelSizeTier(nextTier);
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                StartTestDrive();
                return;
            }

            Camera cam = Camera.main;
            bool aimingActive = LegoGameInput.IsGarageAimingActive;
            Ray aimRay = cam != null
                ? (aimingActive ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) : cam.ScreenPointToRay(Input.mousePosition))
                : default;

            Vector2 guiMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool mouseOverUI = !aimingActive && (
                _topLeftPanelRect.Contains(guiMousePos) ||
                _topRightPanelRect.Contains(guiMousePos) ||
                _bottomDockRect.Contains(guiMousePos));

            if (!mouseOverUI && cam != null)
            {
                UpdateCenterRayFaceSnapping(aimRay);

                if (aimingActive)
                {
                    if (Input.GetMouseButtonDown(1))
                        TryDeleteBrickAlongRay(aimRay);
                }
                else
                {
                    if (Input.GetMouseButtonDown(1))
                        _rightClickDownPos = Input.mousePosition;

                    if (Input.GetMouseButtonUp(1) && Vector2.Distance(_rightClickDownPos, Input.mousePosition) < 6f)
                        TryDeleteBrickAlongRay(aimRay);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (IsHoldingBrick)
                        PlaceBrickAtCursor();
                    else
                        TryDeleteBrickAlongRay(aimRay);
                }
            }

            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.R))
            {
                _rotated90 = !_rotated90;
                UpdateCursorGhostMesh();
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayBrickClick(0.35f);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                _mirrorSymmetry = !_mirrorSymmetry;
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayBrickClick(0.35f);
            }

            if (IsHoldingBrick && IsCarInspectView && Input.GetKeyDown(KeyCode.Space))
                PlaceBrickAtCursor();

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                RemoveLastCustomBrick();

            UpdateGhostTransforms();
        }

        public bool UpdateCenterRayFaceSnapping(Ray worldRay)
        {
            if (playerKart == null || playerKart.Visuals == null || playerKart.Visuals.Loadout == null)
            {
                HideAimedFaceHighlight();
                return false;
            }

            EnsureAimedFaceHighlight();
            Transform kartTr = playerKart.transform;
            float wheelScale = LegoMeshAndMaterialFactory.GetWheelScaleForTier(CurrentWheelSizeTier);
            float stanceLift = (wheelScale - 1.0f) * 0.24f;
            float baseLocalY = LegoVehicleVisualAndDamage.GROUND_CONTACT_OFFSET_Y + stanceLift;

            Vector3 localOrigin = kartTr.InverseTransformPoint(worldRay.origin) - new Vector3(0f, baseLocalY, 0f);
            Vector3 localDir = kartTr.InverseTransformDirection(worldRay.direction).normalized;
            Ray localRay = new Ray(localOrigin, localDir);

            List<LegoBrickSpec> bricks = playerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);
            float bestDist = float.MaxValue;
            int hitIdx = -1;
            Vector3 bestCenter = Vector3.zero;
            Vector3 bestExtents = Vector3.zero;

            for (int i = 0; i < bricks.Count; i++)
            {
                LegoBrickSpec b = bricks[i];
                if (b == null || b.shape == BrickShape.Wheel) continue;

                float w = b.size.x * LegoMeshAndMaterialFactory.STUD_UNIT;
                float h = b.size.y * LegoMeshAndMaterialFactory.PLATE_UNIT;
                float l = b.size.z * LegoMeshAndMaterialFactory.STUD_UNIT;
                Vector3 center = new Vector3(
                    b.gridPos.x * LegoMeshAndMaterialFactory.STUD_UNIT,
                    b.gridPos.y * LegoMeshAndMaterialFactory.PLATE_UNIT + h * 0.5f,
                    b.gridPos.z * LegoMeshAndMaterialFactory.STUD_UNIT
                );

                Bounds bounds = new Bounds(center, new Vector3(w, h, l));
                if (bounds.IntersectRay(localRay, out float dist) && dist >= 0f && dist < bestDist)
                {
                    bestDist = dist;
                    hitIdx = i;
                    bestCenter = center;
                    bestExtents = new Vector3(w * 0.5f, h * 0.5f, l * 0.5f);
                }
            }

            if (hitIdx >= 0)
            {
                LegoBrickSpec target = bricks[hitIdx];
                Vector3 hitLocal = localRay.GetPoint(bestDist);
                Vector3 rel = hitLocal - bestCenter;
                float nx = bestExtents.x > 0.0001f ? rel.x / bestExtents.x : 0f;
                float ny = bestExtents.y > 0.0001f ? rel.y / bestExtents.y : 0f;
                float nz = bestExtents.z > 0.0001f ? rel.z / bestExtents.z : 0f;

                Vector3Int normal;
                if (Mathf.Abs(nx) >= Mathf.Abs(ny) && Mathf.Abs(nx) >= Mathf.Abs(nz))
                    normal = new Vector3Int(nx >= 0f ? 1 : -1, 0, 0);
                else if (Mathf.Abs(ny) >= Mathf.Abs(nx) && Mathf.Abs(ny) >= Mathf.Abs(nz))
                    normal = new Vector3Int(0, ny >= 0f ? 1 : -1, 0);
                else
                    normal = new Vector3Int(0, 0, nz >= 0f ? 1 : -1);

                AimedBrickIndex = hitIdx;
                AimedFaceNormal = normal;

                Vector3 faceCenterLocal = new Vector3(bestCenter.x, bestCenter.y + baseLocalY, bestCenter.z) + new Vector3(
                    normal.x * (bestExtents.x + 0.008f),
                    normal.y * (bestExtents.y + 0.008f),
                    normal.z * (bestExtents.z + 0.008f)
                );
                _aimedFaceHighlightObj.SetActive(true);
                _aimedFaceHighlightObj.transform.position = kartTr.TransformPoint(faceCenterLocal);
                _aimedFaceHighlightObj.transform.rotation = kartTr.rotation;
                if (normal.x != 0)
                    _aimedFaceHighlightObj.transform.localScale = new Vector3(0.014f, bestExtents.y * 1.92f, bestExtents.z * 1.92f);
                else if (normal.y != 0)
                    _aimedFaceHighlightObj.transform.localScale = new Vector3(bestExtents.x * 1.92f, 0.014f, bestExtents.z * 1.92f);
                else
                    _aimedFaceHighlightObj.transform.localScale = new Vector3(bestExtents.x * 1.92f, bestExtents.y * 1.92f, 0.014f);

                Vector3Int sz = GetCurrentPartSize();
                float hitGridX = hitLocal.x / LegoMeshAndMaterialFactory.STUD_UNIT;
                float hitGridY = hitLocal.y / LegoMeshAndMaterialFactory.PLATE_UNIT;
                float hitGridZ = hitLocal.z / LegoMeshAndMaterialFactory.STUD_UNIT;

                int minSpanX = target.gridPos.x - Mathf.FloorToInt(target.size.x * 0.5f);
                int maxSpanX = target.gridPos.x + Mathf.FloorToInt(target.size.x * 0.5f);
                int minSpanZ = target.gridPos.z - Mathf.FloorToInt(target.size.z * 0.5f);
                int maxSpanZ = target.gridPos.z + Mathf.FloorToInt(target.size.z * 0.5f);

                int gx = Mathf.Clamp(Mathf.RoundToInt(hitGridX), minSpanX, maxSpanX);
                int gz = Mathf.Clamp(Mathf.RoundToInt(hitGridZ), minSpanZ, maxSpanZ);
                int gy = Mathf.Clamp(Mathf.RoundToInt(hitGridY - sz.y * 0.5f), target.gridPos.y - sz.y + 1, target.gridPos.y + target.size.y - 1);

                if (normal.y > 0)
                    gy = target.gridPos.y + target.size.y;
                else if (normal.y < 0)
                    gy = target.gridPos.y - sz.y;
                else if (normal.x != 0)
                {
                    float cx = target.gridPos.x + normal.x * (target.size.x * 0.5f + sz.x * 0.5f);
                    gx = normal.x > 0 ? Mathf.FloorToInt(cx) : Mathf.CeilToInt(cx);
                }
                else if (normal.z != 0)
                {
                    float cz = target.gridPos.z + normal.z * (target.size.z * 0.5f + sz.z * 0.5f);
                    gz = normal.z > 0 ? Mathf.FloorToInt(cz) : Mathf.CeilToInt(cz);
                }

                Vector3Int nextPos = new Vector3Int(
                    Mathf.Clamp(gx, -4, 4),
                    Mathf.Clamp(gy, 1, 12),
                    Mathf.Clamp(gz, -5, 5)
                );
                if (nextPos != _cursorGridPos)
                {
                    _cursorGridPos = nextPos;
                    UpdateCursorGhostMesh();
                }
                return true;
            }

            HideAimedFaceHighlight();
            float planeLocalY = baseLocalY + 3f * LegoMeshAndMaterialFactory.PLATE_UNIT;
            Plane buildPlane = new Plane(kartTr.up, kartTr.TransformPoint(new Vector3(0f, planeLocalY, 0f)));
            if (buildPlane.Raycast(worldRay, out float enterDist) && enterDist > 0f && enterDist < 55f)
            {
                Vector3 hitLocal = kartTr.InverseTransformPoint(worldRay.GetPoint(enterDist));
                if (Mathf.Abs(hitLocal.x) <= 2.8f && Mathf.Abs(hitLocal.z) <= 3.6f)
                {
                    int gx = Mathf.Clamp(Mathf.RoundToInt(hitLocal.x / LegoMeshAndMaterialFactory.STUD_UNIT), -4, 4);
                    int gz = Mathf.Clamp(Mathf.RoundToInt(hitLocal.z / LegoMeshAndMaterialFactory.STUD_UNIT), -5, 5);
                    int gy = GetRestingPlateHeightAt(gx, gz);
                    Vector3Int nextPos = new Vector3Int(gx, gy, gz);
                    if (nextPos != _cursorGridPos)
                    {
                        _cursorGridPos = nextPos;
                        UpdateCursorGhostMesh();
                    }
                }
            }
            return false;
        }

        private int GetRestingPlateHeightAt(int gx, int gz)
        {
            List<LegoBrickSpec> bricks = playerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);
            int maxTopY = 3;

            foreach (LegoBrickSpec b in bricks)
            {
                if (b.shape == BrickShape.Wheel) continue;
                float halfX = b.size.x * 0.5f;
                float halfZ = b.size.z * 0.5f;
                if (Mathf.Abs(gx - b.gridPos.x) < halfX + 0.1f && Mathf.Abs(gz - b.gridPos.z) < halfZ + 0.1f)
                {
                    int topY = b.gridPos.y + b.size.y;
                    if (topY > maxTopY) maxTopY = topY;
                }
            }
            return Mathf.Clamp(maxTopY, 1, 10);
        }

        private static Bounds GetSpecGridBounds(LegoBrickSpec spec)
        {
            Vector3 center = new Vector3(
                spec.gridPos.x,
                spec.gridPos.y + spec.size.y * 0.5f,
                spec.gridPos.z
            );
            Vector3 size = new Vector3(
                Mathf.Max(0.8f, spec.size.x),
                Mathf.Max(0.8f, spec.size.y),
                Mathf.Max(0.8f, spec.size.z)
            );
            return new Bounds(center, size);
        }

        public static bool AreBricksConnected(LegoBrickSpec a, LegoBrickSpec b)
        {
            if (a == null || b == null) return false;
            Bounds ba = GetSpecGridBounds(a);
            Bounds bb = GetSpecGridBounds(b);

            float overlapX = Mathf.Min(ba.max.x, bb.max.x) - Mathf.Max(ba.min.x, bb.min.x);
            float overlapY = Mathf.Min(ba.max.y, bb.max.y) - Mathf.Max(ba.min.y, bb.min.y);
            float overlapZ = Mathf.Min(ba.max.z, bb.max.z) - Mathf.Max(ba.min.z, bb.min.z);

            const float minArea = 0.05f;
            const float touchTol = -0.10f;

            // True face-to-face attachment or overlap: >=2 axes overlap strictly, 3rd axis touches within tolerance
            bool touchX = (overlapX >= touchTol && overlapY > minArea && overlapZ > minArea);
            bool touchY = (overlapY >= touchTol && overlapX > minArea && overlapZ > minArea);
            bool touchZ = (overlapZ >= touchTol && overlapX > minArea && overlapY > minArea);
            return touchX || touchY || touchZ;
        }

        public static bool IsBrickPlacementConnected(LegoBrickSpec candidate, List<LegoBrickSpec> existing)
        {
            if (candidate == null || existing == null || existing.Count == 0) return false;

            for (int i = 0; i < existing.Count; i++)
            {
                LegoBrickSpec other = existing[i];
                if (other == null) continue;
                if (AreBricksConnected(candidate, other))
                    return true;
            }
            return false;
        }

        public static bool WouldRemovalDisconnectBricks(int removeIndex, List<LegoBrickSpec> existing)
        {
            if (existing == null || removeIndex < 0 || removeIndex >= existing.Count) return false;

            int remainingCount = 0;
            bool[] visited = new bool[existing.Count];
            Queue<int> queue = new Queue<int>();

            for (int i = 0; i < existing.Count; i++)
            {
                if (i == removeIndex || existing[i] == null) continue;
                remainingCount++;
                if (existing[i].isCoreChassis)
                {
                    visited[i] = true;
                    queue.Enqueue(i);
                }
            }

            if (remainingCount == 0 || queue.Count == 0) return false;

            int reached = queue.Count;
            while (queue.Count > 0)
            {
                int curr = queue.Dequeue();
                for (int next = 0; next < existing.Count; next++)
                {
                    if (next == removeIndex || visited[next] || existing[next] == null) continue;
                    if (AreBricksConnected(existing[curr], existing[next]))
                    {
                        visited[next] = true;
                        queue.Enqueue(next);
                        reached++;
                    }
                }
            }

            return reached < remainingCount;
        }

        private void TryDeleteBrickAlongRay(Ray worldRay)
        {
            if (playerKart == null || playerKart.Visuals == null || playerKart.Visuals.Loadout == null) return;

            Transform kartTr = playerKart.transform;
            float wheelScale = LegoMeshAndMaterialFactory.GetWheelScaleForTier(CurrentWheelSizeTier);
            float stanceLift = (wheelScale - 1.0f) * 0.24f;
            float baseLocalY = LegoVehicleVisualAndDamage.GROUND_CONTACT_OFFSET_Y + stanceLift;

            Vector3 localOrigin = kartTr.InverseTransformPoint(worldRay.origin) - new Vector3(0f, baseLocalY, 0f);
            Vector3 localDir = kartTr.InverseTransformDirection(worldRay.direction).normalized;
            Ray localRay = new Ray(localOrigin, localDir);

            List<LegoBrickSpec> bricks = playerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);
            float bestDist = float.MaxValue;
            int hitIdx = -1;

            for (int i = 0; i < bricks.Count; i++)
            {
                LegoBrickSpec b = bricks[i];
                if (b == null || b.isCoreChassis || b.shape == BrickShape.Wheel) continue;

                float w = b.size.x * LegoMeshAndMaterialFactory.STUD_UNIT;
                float h = b.size.y * LegoMeshAndMaterialFactory.PLATE_UNIT;
                float l = b.size.z * LegoMeshAndMaterialFactory.STUD_UNIT;

                Vector3 center = new Vector3(
                    b.gridPos.x * LegoMeshAndMaterialFactory.STUD_UNIT,
                    b.gridPos.y * LegoMeshAndMaterialFactory.PLATE_UNIT + h * 0.5f,
                    b.gridPos.z * LegoMeshAndMaterialFactory.STUD_UNIT
                );

                Bounds bounds = new Bounds(center, new Vector3(w, h, l));
                if (bounds.IntersectRay(localRay, out float dist) && dist >= 0f && dist < bestDist)
                {
                    bestDist = dist;
                    hitIdx = i;
                }
            }

            if (hitIdx >= 0) RemoveBrickAtIndex(hitIdx);
        }

        private void UpdateGhostTransforms()
        {
            if (_cursorGhostObj == null || playerKart == null) return;

            if (!IsHoldingBrick)
            {
                _cursorGhostObj.SetActive(false);
                if (_mirrorGhostObj != null) _mirrorGhostObj.SetActive(false);
                return;
            }

            _cursorGhostObj.SetActive(true);
            float wheelScale = LegoMeshAndMaterialFactory.GetWheelScaleForTier(CurrentWheelSizeTier);
            float stanceLift = (wheelScale - 1.0f) * 0.24f;

            Vector3 localOffset = new Vector3(
                _cursorGridPos.x * LegoMeshAndMaterialFactory.STUD_UNIT,
                LegoVehicleVisualAndDamage.GROUND_CONTACT_OFFSET_Y + Mathf.Max(0.05f, _cursorGridPos.y * LegoMeshAndMaterialFactory.PLATE_UNIT + stanceLift),
                _cursorGridPos.z * LegoMeshAndMaterialFactory.STUD_UNIT
            );
            _cursorGhostObj.transform.position = playerKart.transform.TransformPoint(localOffset);
            _cursorGhostObj.transform.rotation = playerKart.transform.rotation;

            if (_mirrorGhostObj != null)
            {
                bool showMirror = _mirrorSymmetry && _cursorGridPos.x != 0;
                _mirrorGhostObj.SetActive(showMirror);
                if (showMirror)
                {
                    Vector3 mirrorOffset = new Vector3(
                        -_cursorGridPos.x * LegoMeshAndMaterialFactory.STUD_UNIT,
                        localOffset.y,
                        _cursorGridPos.z * LegoMeshAndMaterialFactory.STUD_UNIT
                    );
                    _mirrorGhostObj.transform.position = playerKart.transform.TransformPoint(mirrorOffset);
                    _mirrorGhostObj.transform.rotation = playerKart.transform.rotation;
                }
            }
        }

        public bool PlaceBrickAtCursor()
        {
            if (playerKart == null || !IsHoldingBrick) return false;
            List<LegoBrickSpec> list = playerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);

            Vector3Int sz = GetCurrentPartSize();
            BrickShape shp = _partCatalog[_selectedPartIdx].shape;
            Color col = LegoPalette.GarageColors[_selectedColorIdx];

            LegoBrickSpec candidate = new LegoBrickSpec(_cursorGridPos, sz, col, shp, false, _emissiveGlow);
            if (!IsBrickPlacementConnected(candidate, list))
            {
                _isPlacementValid = false;
                UpdateCursorGhostMesh();
                ShowStatusMessage("Khong the dat block lo lung! Phai gan dinh vao khung xe hoac block khac.", 2.8f);
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayBrickCrash();
                return false;
            }

            AddOrReplaceBrick(list, candidate);

            if (_mirrorSymmetry && _cursorGridPos.x != 0)
            {
                Vector3Int mirrorPos = new Vector3Int(-_cursorGridPos.x, _cursorGridPos.y, _cursorGridPos.z);
                LegoBrickSpec mirrorCand = new LegoBrickSpec(mirrorPos, sz, col, shp, false, _emissiveGlow);
                if (IsBrickPlacementConnected(mirrorCand, list))
                    AddOrReplaceBrick(list, mirrorCand);
            }

            playerKart.Visuals.Loadout.SaveToPrefs();
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.ResetPoseForGarage();
            UpdateCursorGhostMesh();

            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.8f);
            return true;
        }

        private static void AddOrReplaceBrick(List<LegoBrickSpec> list, LegoBrickSpec newBrick)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].isCoreChassis && list[i].gridPos == newBrick.gridPos)
                {
                    list.RemoveAt(i);
                }
            }
            list.Add(newBrick);
        }

        public bool RemoveBrickAtIndex(int idx)
        {
            if (playerKart == null) return false;
            List<LegoBrickSpec> list = playerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);
            if (idx < 0 || idx >= list.Count) return false;

            LegoBrickSpec target = list[idx];
            if (target.isCoreChassis) return false;

            if (WouldRemovalDisconnectBricks(idx, list))
            {
                ShowStatusMessage("Khong the xoa khoi nay vi se lam cac block gan tren no bi lo lung!", 2.8f);
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayBrickCrash();
                return false;
            }

            list.RemoveAt(idx);
            playerKart.Visuals.Loadout.SaveToPrefs();
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.ResetPoseForGarage();
            UpdateCursorGhostMesh();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.6f);
            return true;
        }

        public void RemoveLastCustomBrick()
        {
            if (playerKart == null) return;
            List<LegoBrickSpec> list = playerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].isCoreChassis)
                {
                    if (RemoveBrickAtIndex(i)) return;
                }
            }
        }

        public void StripToSmallBareBase()
        {
            if (playerKart == null) return;
            playerKart.Visuals.Loadout.ResetAllToSmallBareBase();
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.ResetPoseForGarage();
            UpdateCursorGhostMesh();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        public void LoadStarterTemplate()
        {
            if (playerKart == null) return;
            playerKart.Visuals.Loadout.LoadFullTemplateForForm(TerrainType.Street);
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.ResetPoseForGarage();
            UpdateCursorGhostMesh();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        private void DrawSolidRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            if (_solidBgTex != null) GUI.DrawTexture(r, _solidBgTex);
            GUI.color = prev;
        }

        private void DrawStatMeter(string label, int rating, Color fillCol)
        {
            GUILayout.BeginHorizontal();
            GUIStyle lbl = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            lbl.normal.textColor = new Color(0.90f, 0.94f, 0.98f);
            GUILayout.Label(label, lbl, GUILayout.Width(96f));
            Rect barRow = GUILayoutUtility.GetRect(132f, 18f);
            for (int s = 0; s < 6; s++)
            {
                Rect seg = new Rect(barRow.x + s * 22f, barRow.y + 3f, 19f, 12f);
                DrawSolidRect(seg, s < rating ? fillCol : new Color(0.20f, 0.24f, 0.30f, 1f));
            }
            GUILayout.EndHorizontal();
        }

        public void DrawGarageOverlayGUI()
        {
            if (_statusTimer > 0f && !string.IsNullOrEmpty(LastStatusMessage))
            {
                Rect toast = new Rect(Screen.width * 0.5f - 260f, 20f, 520f, 36f);
                DrawSolidRect(toast, new Color(0.10f, 0.13f, 0.18f, 0.92f));
                DrawSolidRect(new Rect(toast.x, toast.y + toast.height - 3f, toast.width, 3f), LegoPalette.BrightOrange);
                GUIStyle ts = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                ts.normal.textColor = LegoPalette.BrightYellow;
                GUI.Label(toast, LastStatusMessage, ts);
            }

            if (!IsGarageOpen || playerKart == null) return;

            float sw = Screen.width;
            float sh = Screen.height;

            // 1. Top-Left Header Badge
            _topLeftPanelRect = new Rect(16f, 16f, 520f, 118f);
            DrawSolidRect(_topLeftPanelRect, new Color(0.10f, 0.14f, 0.20f, 0.92f));
            DrawSolidRect(new Rect(_topLeftPanelRect.x, _topLeftPanelRect.y, _topLeftPanelRect.width, 3f), LegoPalette.CyanGlow);

            GUIStyle headerTitle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            headerTitle.normal.textColor = Color.white;
            GUIStyle headerSub = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            headerSub.normal.textColor = LegoPalette.BrightYellow;

            GUILayout.BeginArea(new Rect(_topLeftPanelRect.x + 12f, _topLeftPanelRect.y + 8f, _topLeftPanelRect.width - 24f, _topLeftPanelRect.height - 12f));
            GUILayout.Label("PHONG THIET KE", headerTitle);
            string holdTxt = IsCarInspectView
                ? "CHE DO: VIEW XE XOAY 360 (Di chuot de xoay)"
                : (IsHoldingBrick
                    ? $"CHE DO: DI BO LAP XE ({_partCatalog[_selectedPartIdx].name})"
                    : "CHE DO: DI BO - TAY KHONG | Nham [+] vao block de xoa");
            GUILayout.Label(holdTxt, headerSub);
            GUILayout.Label("[Q / B] Doi View Xe <-> Di Bo Lap | [WASD] Di chuyen | [V] Goc nhin 1/3", headerSub);
            GUILayout.Label("[1-4] Block | [Z/C] Nhom | [F] Mau | [N] Banh | [G] Chay Thu | Giu [Alt] UI", headerSub);
            GUILayout.EndArea();

            // 2. Top-Right Car Specs + Wheel Size Selector (Image 1 style)
            VehicleAeroStats stats = playerKart.Visuals.Loadout.CalculateVehicleStats();
            _topRightPanelRect = new Rect(sw - 278f, 16f, 262f, 162f);
            DrawSolidRect(_topRightPanelRect, new Color(0.10f, 0.14f, 0.20f, 0.92f));
            DrawSolidRect(new Rect(_topRightPanelRect.x, _topRightPanelRect.y, _topRightPanelRect.width, 3f), LegoPalette.BrightGreen);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
            GUILayout.BeginArea(new Rect(_topRightPanelRect.x + 12f, _topRightPanelRect.y + 8f, _topRightPanelRect.width - 24f, _topRightPanelRect.height - 12f));
            GUILayout.Label($"Thong so xe ({stats.brickCount} khoi)", headerTitle);
            DrawStatMeter("Toc do", stats.speedRating, LegoPalette.BrightGreen);
            DrawStatMeter("Trong luong", stats.weightRating, LegoPalette.BrightOrange);
            DrawStatMeter("Khi dong hoc", stats.aeroRating, LegoPalette.CyanGlow);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            WheelSizeTier curTier = CurrentWheelSizeTier;
            if (GUILayout.Button(curTier == WheelSizeTier.Small ? "[Nho]" : "Banh Nho", btnStyle, GUILayout.Height(24f)))
                SetWheelSizeTier(WheelSizeTier.Small);
            if (GUILayout.Button(curTier == WheelSizeTier.Medium ? "[Vua]" : "Banh Vua", btnStyle, GUILayout.Height(24f)))
                SetWheelSizeTier(WheelSizeTier.Medium);
            if (GUILayout.Button(curTier == WheelSizeTier.Large ? "[To]" : "Banh To", btnStyle, GUILayout.Height(24f)))
                SetWheelSizeTier(WheelSizeTier.Large);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 3. Bottom Modular Dock (5 Tabs + Parts + Colors + Test Drive Button)
            float dockW = Mathf.Min(920f, sw - 32f);
            float dockH = 192f;
            _bottomDockRect = new Rect((sw - dockW) * 0.5f, sh - dockH - 12f, dockW, dockH);
            DrawSolidRect(_bottomDockRect, new Color(0.11f, 0.16f, 0.24f, 0.94f));
            DrawSolidRect(new Rect(_bottomDockRect.x, _bottomDockRect.y, dockW, 3f), LegoPalette.BrightYellow);

            GUILayout.BeginArea(new Rect(_bottomDockRect.x + 10f, _bottomDockRect.y + 8f, dockW - 20f, dockH - 12f));

            // 5 Category Tabs across top of bottom dock (Image 1)
            GUILayout.BeginHorizontal();
            for (int c = 0; c < _categoryNames.Length; c++)
            {
                PartCategory cat = (PartCategory)c;
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = (_activeCategory == cat) ? LegoPalette.CyanGlow : new Color(0.24f, 0.30f, 0.40f);
                if (GUILayout.Button(_categoryNames[c], btnStyle, GUILayout.Height(28f)))
                    _activeCategory = cat;
                GUI.backgroundColor = oldBg;
            }
            GUI.backgroundColor = !IsHoldingBrick ? LegoPalette.BrightGreen : new Color(0.32f, 0.36f, 0.44f);
            if (GUILayout.Button("Tay Khong [0]", btnStyle, GUILayout.Height(28f), GUILayout.Width(105f)))
                ClearSelectedPartToNull();
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            // Parts inside active category + Tool options
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _partCatalog.Length; i++)
            {
                if (_partCatalog[i].category != _activeCategory) continue;
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = (_selectedPartIdx == i) ? LegoPalette.BrightYellow : new Color(0.22f, 0.28f, 0.38f);
                if (GUILayout.Button(_partCatalog[i].name, btnStyle, GUILayout.Height(40f), GUILayout.Width(122f)))
                    SetSelectedPart(i);
                GUI.backgroundColor = oldBg;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_rotated90 ? "Xoay [R]: 90" : "Xoay [R]: 0", btnStyle, GUILayout.Height(40f), GUILayout.Width(96f)))
            {
                _rotated90 = !_rotated90;
                UpdateCursorGhostMesh();
            }
            if (GUILayout.Button(_mirrorSymmetry ? "Doi Xung: BAT" : "Doi Xung: TAT", btnStyle, GUILayout.Height(40f), GUILayout.Width(104f)))
                _mirrorSymmetry = !_mirrorSymmetry;
            if (GUILayout.Button(_emissiveGlow ? "LED: BAT" : "LED: TAT", btnStyle, GUILayout.Height(40f), GUILayout.Width(82f)))
            {
                _emissiveGlow = !_emissiveGlow;
                UpdateCursorGhostMesh();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            // Color palette row
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Mau ({LegoPalette.GarageColorNames[_selectedColorIdx]}):", headerSub, GUILayout.Width(115f));
            for (int i = 0; i < LegoPalette.GarageColors.Length; i++)
            {
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = LegoPalette.GarageColors[i];
                string mark = (_selectedColorIdx == i) ? "O" : "";
                if (GUILayout.Button(mark, btnStyle, GUILayout.Width(32f), GUILayout.Height(24f)))
                {
                    _selectedColorIdx = i;
                    UpdateCursorGhostMesh();
                }
                GUI.backgroundColor = oldBg;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(IsCarInspectView ? "Trai: Lap | Phai: Xoa | [WASD] Dich Tam | [R] Xoay" : "Trai: Lap | Phai: Xoa | [WASD] Di chuyen | [R] Xoay", headerSub);
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            // Action Buttons + VIEW XE / DI BO + CHẠY THỬ + Thoát Garage
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Dat [Trai]", btnStyle, GUILayout.Height(34f), GUILayout.Width(100f)))
                PlaceBrickAtCursor();
            if (GUILayout.Button("- Hoan tac", btnStyle, GUILayout.Height(34f), GUILayout.Width(86f)))
                RemoveLastCustomBrick();
            if (GUILayout.Button("Khung Co Ban", btnStyle, GUILayout.Height(34f), GUILayout.Width(100f)))
                StripToSmallBareBase();
            if (GUILayout.Button("Mau The Thao", btnStyle, GUILayout.Height(34f), GUILayout.Width(100f)))
                LoadStarterTemplate();

            GUILayout.FlexibleSpace();
            Color oldTestBg = GUI.backgroundColor;
            GUI.backgroundColor = LegoPalette.CyanGlow;
            if (GUILayout.Button(IsCarInspectView ? "DI BO LAP XE [Q]" : "VIEW XE 360 [Q]", btnStyle, GUILayout.Height(34f), GUILayout.Width(142f)))
                ToggleCarInspectView();
            GUI.backgroundColor = LegoPalette.BrightGreen;
            if (GUILayout.Button("CHAY THU [G]", btnStyle, GUILayout.Height(34f), GUILayout.Width(142f)))
                StartTestDrive();
            GUI.backgroundColor = LegoPalette.BrightOrange;
            if (GUILayout.Button("THOAT GARAGE [TAB]", btnStyle, GUILayout.Height(34f), GUILayout.Width(155f)))
                CloseGarageToOnFoot();
            GUI.backgroundColor = oldTestBg;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
