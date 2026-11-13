using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    /// <summary>
    /// Interactive LEGO 2K Drive 3D Brick Studio & Garage Builder.
    /// Features:
    /// - "NULL / Tay Khong" mode (_selectedPartIdx = -1) so you don't have to hold a brick all the time!
    /// - Stable Horizontal-Plane Stud Grid Snapping with hysteresis & post-click lock so bricks NEVER jump wildly!
    /// - Symmetry Mirroring [M], 90-degree Rotation [R/X], and automatic saving.
    /// </summary>
    public class LegoGarageBuilder : MonoBehaviour
    {
        public static LegoGarageBuilder Instance { get; private set; }

        public bool IsGarageOpen { get; private set; }
        public bool IsHoldingBrick => _selectedPartIdx >= 0;
        public ArcadeKartController playerKart;

        private TerrainType _editingForm = TerrainType.Street;
        private Vector3Int _cursorGridPos = new Vector3Int(0, 3, 0);
        private int _selectedColorIdx = 0;
        private int _selectedPartIdx = -1; // -1 = NULL (Không cầm gạch / Tay không)
        private bool _rotated90 = false;
        private bool _mirrorSymmetry = false;
        private bool _emissiveGlow = false;

        private GameObject _cursorGhostObj;
        private GameObject _mirrorGhostObj;
        private Vector2 _rightClickDownPos;
        private Vector2 _lastPlacedMousePos;
        private bool _freezeSnapUntilMouseMoves = false;
        private Vector2 _smoothedGridXZ = Vector2.zero;
        private Texture2D _solidBgTex;

        private readonly Rect _uiPanelRect = new Rect(16f, 16f, 360f, 600f);

        private readonly Vector3Int[] _baseSizes = new Vector3Int[]
        {
            new Vector3Int(1, 1, 1), // 0: 1x1 Stud
            new Vector3Int(1, 2, 2), // 1: 1x2 Brick
            new Vector3Int(1, 2, 4), // 2: 1x4 Brick
            new Vector3Int(2, 2, 2), // 3: 2x2 Brick
            new Vector3Int(2, 2, 4), // 4: 2x4 Brick
            new Vector3Int(2, 2, 2), // 5: 2x2 Slope
            new Vector3Int(2, 2, 3), // 6: 2x3 Windshield
            new Vector3Int(2, 1, 4), // 7: 2x4 Flat Tile
            new Vector3Int(2, 2, 3), // 8: 2x3 Fender Arch
            new Vector3Int(4, 2, 2), // 9: 4x2 Spoiler Wing
            new Vector3Int(2, 2, 2)  // 10: 2x2 Jet Thruster
        };

        private readonly BrickShape[] _partShapes = new BrickShape[]
        {
            BrickShape.StandardBrick,
            BrickShape.StandardBrick,
            BrickShape.StandardBrick,
            BrickShape.StandardBrick,
            BrickShape.StandardBrick,
            BrickShape.SlopeFront,
            BrickShape.Windshield,
            BrickShape.FlatTile,
            BrickShape.FenderArch,
            BrickShape.SpoilerWing,
            BrickShape.JetNozzle
        };

        private readonly string[] _partNames = new string[]
        {
            "1x1 Stud",
            "1x2 Brick",
            "1x4 Brick",
            "2x2 Brick",
            "2x4 Brick",
            "2x2 Slope",
            "Windshield",
            "2x4 Tile",
            "Fender Arch",
            "Spoiler Wing",
            "Jet Thruster"
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
            if (_selectedPartIdx < 0 || _selectedPartIdx >= _baseSizes.Length)
                return new Vector3Int(2, 2, 2);

            Vector3Int s = _baseSizes[_selectedPartIdx];
            if (_rotated90)
                return new Vector3Int(s.z, s.y, s.x);
            return s;
        }

        public void SetSelectedPart(int partIndex)
        {
            _selectedPartIdx = partIndex;
            _freezeSnapUntilMouseMoves = false;
            UpdateCursorGhostMesh();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.4f);
        }

        public void ClearSelectedPartToNull()
        {
            SetSelectedPart(-1);
        }

        public void ToggleGarage()
        {
            IsGarageOpen = !IsGarageOpen;
            if (LegoGameInput.Instance != null) LegoGameInput.Instance.RefreshState();
            if (playerKart != null && LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot)
                playerKart.gameObject.SetActive(IsGarageOpen);

            if (IsGarageOpen && playerKart != null)
            {
                _editingForm = playerKart.Visuals.CurrentForm;
                playerKart.Visuals.ResetPoseForGarage();
                // Start in NULL (Tay Không) mode when opening Garage so no brick floats in the way!
                _selectedPartIdx = -1;
                EnsureCursorGhosts();
                UpdateCursorGhostMesh();
            }
            else
            {
                if (_cursorGhostObj != null) _cursorGhostObj.SetActive(false);
                if (_mirrorGhostObj != null) _mirrorGhostObj.SetActive(false);
            }

            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
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
            BrickShape shp = _partShapes[_selectedPartIdx];
            Color col = LegoPalette.GarageColors[_selectedColorIdx];
            Mesh mesh = LegoMeshAndMaterialFactory.GetBrickMesh(sz.x, sz.y, sz.z, shp);
            Material mat = LegoMeshAndMaterialFactory.GetPlasticMaterial(col, _emissiveGlow);

            _cursorGhostObj.GetComponent<MeshFilter>().sharedMesh = mesh;
            _cursorGhostObj.GetComponent<MeshRenderer>().sharedMaterial = mat;

            if (_mirrorGhostObj != null)
            {
                _mirrorGhostObj.GetComponent<MeshFilter>().sharedMesh = mesh;
                _mirrorGhostObj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        private void Update()
        {
            if (!IsGarageOpen || playerKart == null) return;

            // Escape is reserved for closing the garage; 0 clears the selected brick.
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                ClearSelectedPartToNull();
            }

            Vector2 guiMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool mouseOverUI = _uiPanelRect.Contains(guiMousePos);

            if (!mouseOverUI)
            {
                // Track Right-Click (Quick Right-Click = Cancel held brick to NULL, or Delete hovered brick if already NULL)
                if (Input.GetMouseButtonDown(1))
                {
                    _rightClickDownPos = Input.mousePosition;
                }
                if (Input.GetMouseButtonUp(1) && Vector2.Distance(_rightClickDownPos, Input.mousePosition) < 6f)
                {
                    if (IsHoldingBrick)
                    {
                        // Right-click while holding a brick -> drop brick back to NULL (Tay Không)!
                        ClearSelectedPartToNull();
                    }
                    else
                    {
                        // Right-click while in Tay Không mode -> remove brick under mouse ray!
                        TryDeleteBrickUnderMouseRay();
                    }
                }

                // Only snap and place when actually holding a brick!
                if (IsHoldingBrick)
                {
                    UpdateStableMouseGridSnapping();

                    if (Input.GetMouseButtonDown(0))
                    {
                        PlaceBrickAtCursor();
                    }
                }
            }

            // Keyboard Shortcuts
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

            if (IsHoldingBrick)
            {
                bool nudged = false;
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) { _cursorGridPos.z += 1; nudged = true; }
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) { _cursorGridPos.z -= 1; nudged = true; }
                if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) { _cursorGridPos.x += 1; nudged = true; }
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) { _cursorGridPos.x -= 1; nudged = true; }
                if (Input.GetKeyDown(KeyCode.E)) { _cursorGridPos.y += 1; nudged = true; }
                if (Input.GetKeyDown(KeyCode.Q)) { _cursorGridPos.y -= 1; nudged = true; }

                if (nudged)
                {
                    _freezeSnapUntilMouseMoves = true;
                    _lastPlacedMousePos = Input.mousePosition;
                    _cursorGridPos.x = Mathf.Clamp(_cursorGridPos.x, -3, 3);
                    _cursorGridPos.y = Mathf.Clamp(_cursorGridPos.y, 1, 10);
                    _cursorGridPos.z = Mathf.Clamp(_cursorGridPos.z, -4, 4);
                    if (LegoProceduralAudio.Instance != null)
                        LegoProceduralAudio.Instance.PlayBrickClick(0.35f);
                }

                if (Input.GetKeyDown(KeyCode.Space))
                    PlaceBrickAtCursor();
            }

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                RemoveLastCustomBrick();

            UpdateGhostTransforms();
        }

        /// <summary>
        /// Intersects mouse ray with a SINGLE flat horizontal reference plane at the chassis top deck
        /// so placing bricks NEVER alters the raycast plane or causes the cursor to jump wildly!
        /// </summary>
        private void UpdateStableMouseGridSnapping()
        {
            if (_freezeSnapUntilMouseMoves)
            {
                if (Vector2.Distance(Input.mousePosition, _lastPlacedMousePos) < 8f)
                    return;
                _freezeSnapUntilMouseMoves = false;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray worldRay = cam.ScreenPointToRay(Input.mousePosition);
            Transform kartTr = playerKart.transform;

            // Fixed horizontal build plane at chassis deck height (plate Y = 3)
            float planeLocalY = LegoVehicleVisualAndDamage.GROUND_CONTACT_OFFSET_Y + 3f * LegoMeshAndMaterialFactory.PLATE_UNIT;
            Vector3 planeWorldPoint = kartTr.TransformPoint(new Vector3(0f, planeLocalY, 0f));
            Plane buildPlane = new Plane(kartTr.up, planeWorldPoint);

            if (buildPlane.Raycast(worldRay, out float enterDist) && enterDist > 0f && enterDist < 50f)
            {
                Vector3 hitWorld = worldRay.GetPoint(enterDist);
                Vector3 hitLocal = kartTr.InverseTransformPoint(hitWorld);

                float exactStudX = hitLocal.x / LegoMeshAndMaterialFactory.STUD_UNIT;
                float exactStudZ = hitLocal.z / LegoMeshAndMaterialFactory.STUD_UNIT;

                // Hysteresis deadzone (0.22 studs) so cursor never flickers between two adjacent studs
                if (Mathf.Abs(exactStudX - _smoothedGridXZ.x) > 0.62f)
                    _smoothedGridXZ.x = Mathf.Round(exactStudX);
                if (Mathf.Abs(exactStudZ - _smoothedGridXZ.y) > 0.62f)
                    _smoothedGridXZ.y = Mathf.Round(exactStudZ);

                int gx = Mathf.Clamp(Mathf.RoundToInt(_smoothedGridXZ.x), -3, 3);
                int gz = Mathf.Clamp(Mathf.RoundToInt(_smoothedGridXZ.y), -4, 4);

                // Find the highest occupied plate at (gx, gz) so the ghost brick sits cleanly on top
                int gy = GetRestingPlateHeightAt(gx, gz);

                _cursorGridPos = new Vector3Int(gx, gy, gz);
            }
        }

        private int GetRestingPlateHeightAt(int gx, int gz)
        {
            List<LegoBrickSpec> bricks = playerKart.Visuals.Loadout.GetBricksForForm(_editingForm);
            int maxTopY = (_editingForm == TerrainType.OffRoad) ? 4 : 3;

            foreach (LegoBrickSpec b in bricks)
            {
                if (b.shape == BrickShape.Wheel) continue;

                float halfX = b.size.x * 0.5f;
                float halfZ = b.size.z * 0.5f;

                if (Mathf.Abs(gx - b.gridPos.x) < halfX + 0.1f &&
                    Mathf.Abs(gz - b.gridPos.z) < halfZ + 0.1f)
                {
                    int topY = b.gridPos.y + b.size.y;
                    if (topY > maxTopY)
                        maxTopY = topY;
                }
            }

            return Mathf.Clamp(maxTopY, 1, 10);
        }

        private void TryDeleteBrickUnderMouseRay()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray worldRay = cam.ScreenPointToRay(Input.mousePosition);
            Transform kartTr = playerKart.transform;
            Vector3 localOrigin = kartTr.InverseTransformPoint(worldRay.origin) - new Vector3(0f, LegoVehicleVisualAndDamage.GROUND_CONTACT_OFFSET_Y, 0f);
            Vector3 localDir = kartTr.InverseTransformDirection(worldRay.direction).normalized;
            Ray localRay = new Ray(localOrigin, localDir);

            List<LegoBrickSpec> bricks = playerKart.Visuals.Loadout.GetBricksForForm(_editingForm);
            float bestDist = float.MaxValue;
            int hitIdx = -1;

            for (int i = 0; i < bricks.Count; i++)
            {
                LegoBrickSpec b = bricks[i];
                if (b.isCoreChassis || b.shape == BrickShape.Wheel) continue;

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

            if (hitIdx >= 0)
            {
                RemoveBrickAtIndex(hitIdx);
            }
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

            Vector3 localOffset = new Vector3(
                _cursorGridPos.x * LegoMeshAndMaterialFactory.STUD_UNIT,
                LegoVehicleVisualAndDamage.GROUND_CONTACT_OFFSET_Y + _cursorGridPos.y * LegoMeshAndMaterialFactory.PLATE_UNIT,
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
                        LegoVehicleVisualAndDamage.GROUND_CONTACT_OFFSET_Y + _cursorGridPos.y * LegoMeshAndMaterialFactory.PLATE_UNIT,
                        _cursorGridPos.z * LegoMeshAndMaterialFactory.STUD_UNIT
                    );
                    _mirrorGhostObj.transform.position = playerKart.transform.TransformPoint(mirrorOffset);
                    _mirrorGhostObj.transform.rotation = playerKart.transform.rotation;
                }
            }
        }

        public void SelectEditingForm(TerrainType form)
        {
            _editingForm = form;
            if (playerKart != null)
            {
                playerKart.Visuals.SwitchToForm(form, false);
                playerKart.Visuals.ResetPoseForGarage();
            }
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        public void PlaceBrickAtCursor()
        {
            if (playerKart == null || !IsHoldingBrick) return;
            List<LegoBrickSpec> list = playerKart.Visuals.Loadout.GetBricksForForm(_editingForm);

            Vector3Int sz = GetCurrentPartSize();
            BrickShape shp = _partShapes[_selectedPartIdx];
            Color col = LegoPalette.GarageColors[_selectedColorIdx];

            AddOrReplaceBrick(list, new LegoBrickSpec(_cursorGridPos, sz, col, shp, false, _emissiveGlow));

            if (_mirrorSymmetry && _cursorGridPos.x != 0)
            {
                Vector3Int mirrorPos = new Vector3Int(-_cursorGridPos.x, _cursorGridPos.y, _cursorGridPos.z);
                AddOrReplaceBrick(list, new LegoBrickSpec(mirrorPos, sz, col, shp, false, _emissiveGlow));
            }

            // Lock cursor right after clicking so the newly placed brick doesn't cause the cursor to jump!
            _freezeSnapUntilMouseMoves = true;
            _lastPlacedMousePos = Input.mousePosition;

            playerKart.Visuals.Loadout.SaveToPrefs();
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.SwitchToForm(_editingForm, false);
            playerKart.Visuals.ResetPoseForGarage();

            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.8f);
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

        public void RemoveBrickAtIndex(int idx)
        {
            if (playerKart == null) return;
            List<LegoBrickSpec> list = playerKart.Visuals.Loadout.GetBricksForForm(_editingForm);
            if (idx < 0 || idx >= list.Count) return;

            LegoBrickSpec target = list[idx];
            if (target.isCoreChassis) return;

            Vector3Int pos = target.gridPos;
            list.RemoveAt(idx);

            if (_mirrorSymmetry && pos.x != 0)
            {
                Vector3Int mirrorPos = new Vector3Int(-pos.x, pos.y, pos.z);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!list[i].isCoreChassis && list[i].gridPos == mirrorPos)
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }

            playerKart.Visuals.Loadout.SaveToPrefs();
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.SwitchToForm(_editingForm, false);
            playerKart.Visuals.ResetPoseForGarage();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.6f);
        }

        public void RemoveLastCustomBrick()
        {
            if (playerKart == null) return;
            List<LegoBrickSpec> list = playerKart.Visuals.Loadout.GetBricksForForm(_editingForm);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].isCoreChassis)
                {
                    RemoveBrickAtIndex(i);
                    return;
                }
            }
        }

        public void StripToSmallBareBase()
        {
            if (playerKart == null) return;
            playerKart.Visuals.Loadout.ResetFormToSmallBareBase(_editingForm);
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.SwitchToForm(_editingForm, false);
            playerKart.Visuals.ResetPoseForGarage();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        public void LoadStarterTemplate()
        {
            if (playerKart == null) return;
            playerKart.Visuals.Loadout.LoadFullTemplateForForm(_editingForm);
            playerKart.Visuals.RebuildAllForms();
            playerKart.Visuals.SwitchToForm(_editingForm, false);
            playerKart.Visuals.ResetPoseForGarage();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayTransformSnap();
        }

        public void DrawGarageOverlayGUI()
        {
            if (!IsGarageOpen || playerKart == null) return;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = LegoPalette.BrightYellow;

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            subStyle.normal.textColor = Color.white;

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal
            };
            hintStyle.normal.textColor = new Color(0.75f, 0.92f, 1.0f);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            Color prevCol = GUI.color;
            GUI.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);
            if (_solidBgTex != null)
                GUI.DrawTexture(_uiPanelRect, _solidBgTex);
            GUI.color = LegoPalette.BrightYellow;
            if (_solidBgTex != null)
                GUI.DrawTexture(new Rect(_uiPanelRect.x, _uiPanelRect.y, _uiPanelRect.width, 4f), _solidBgTex);
            GUI.color = prevCol;

            GUILayout.BeginArea(new Rect(_uiPanelRect.x + 12f, _uiPanelRect.y + 10f, _uiPanelRect.width - 24f, _uiPanelRect.height - 16f));

            GUILayout.Label("LEGO CHASSIS BRICK BUILDER", titleStyle);
            string modeHint = IsHoldingBrick
                ? $"Dang cam: {_partNames[_selectedPartIdx]} | Click Trai: Dat gach | Click Phai: Bo chon (Null)"
                : "Che do: TAY KHONG (Null) | Keo Chuot de xoay xem xe | Click Phai len gach de Xoa";
            GUILayout.Label(modeHint, hintStyle);
            GUILayout.Space(4f);

            // 1. Select Form to Edit
            GUILayout.Label("1. Chon Dang Khung Gam (Terrain Form):", subStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_editingForm == TerrainType.Street ? "[ STREET ]" : "Street", btnStyle, GUILayout.Height(26f)))
                SelectEditingForm(TerrainType.Street);
            if (GUILayout.Button(_editingForm == TerrainType.OffRoad ? "[ OFF-ROAD ]" : "Off-Road", btnStyle, GUILayout.Height(26f)))
                SelectEditingForm(TerrainType.OffRoad);
            if (GUILayout.Button(_editingForm == TerrainType.Water ? "[ BOAT ]" : "Boat", btnStyle, GUILayout.Height(26f)))
                SelectEditingForm(TerrainType.Water);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 2. NULL / Deselect Button + Modular LEGO Parts Grid
            GUILayout.Label("2. Chon Khoi Gach LEGO (hoac Bo Chon):", subStyle);

            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = !IsHoldingBrick ? LegoPalette.BrightGreen : new Color(0.35f, 0.38f, 0.42f);
            string nullBtnText = !IsHoldingBrick
                ? ">> [ NULL - KHONG CHON GI / TAY KHONG ] <<"
                : "[ NULL - Bo Chon Gach / Tay Khong (Phim 0 / Chuot Phai) ]";
            if (GUILayout.Button(nullBtnText, btnStyle, GUILayout.Height(28f)))
            {
                ClearSelectedPartToNull();
            }
            GUI.backgroundColor = oldBgColor;

            GUILayout.Space(3f);

            for (int i = 0; i < _partNames.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 2 && (i + col) < _partNames.Length; col++)
                {
                    int idx = i + col;
                    string label = (_selectedPartIdx == idx) ? $"> {_partNames[idx]} <" : _partNames[idx];
                    if (GUILayout.Button(label, btnStyle, GUILayout.Height(23f), GUILayout.Width(164f)))
                    {
                        SetSelectedPart(idx);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);

            // 3. Options: Rotate 90, Mirror Left/Right, Emissive LED Glow
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_rotated90 ? "Xoay [R]: 90°" : "Xoay [R]: 0°", btnStyle, GUILayout.Height(25f)))
            {
                _rotated90 = !_rotated90;
                UpdateCursorGhostMesh();
            }
            if (GUILayout.Button(_mirrorSymmetry ? "Doi Xung [M]: BAT" : "Doi Xung [M]: TAT", btnStyle, GUILayout.Height(25f)))
            {
                _mirrorSymmetry = !_mirrorSymmetry;
            }
            if (GUILayout.Button(_emissiveGlow ? "Den LED: BAT" : "Den LED: TAT", btnStyle, GUILayout.Height(25f)))
            {
                _emissiveGlow = !_emissiveGlow;
                UpdateCursorGhostMesh();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 4. Select Brick Color
            GUILayout.Label($"3. Mau Gach ({LegoPalette.GarageColorNames[_selectedColorIdx]}):", subStyle);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < LegoPalette.GarageColors.Length; i++)
            {
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = LegoPalette.GarageColors[i];
                string mark = (_selectedColorIdx == i) ? "X" : "";
                if (GUILayout.Button(mark, btnStyle, GUILayout.Width(34f), GUILayout.Height(26f)))
                {
                    _selectedColorIdx = i;
                    UpdateCursorGhostMesh();
                }
                GUI.backgroundColor = oldBg;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 5. Actions: Place, Undo, Bare Base Reset, Full Template
            List<LegoBrickSpec> currentBricks = playerKart.Visuals.Loadout.GetBricksForForm(_editingForm);
            string cursorStatus = IsHoldingBrick ? $"Vi tri: ({_cursorGridPos.x}, {_cursorGridPos.y}, {_cursorGridPos.z}) [Q/E doi tang]" : "Tay khong (Click Phai len gach de xoa)";
            GUILayout.Label($"{cursorStatus} | So gach: {currentBricks.Count}", subStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ DAT GACH [Space]", btnStyle, GUILayout.Height(28f)))
                PlaceBrickAtCursor();
            if (GUILayout.Button("- HOAN TAC [Bksp]", btnStyle, GUILayout.Height(28f)))
                RemoveLastCustomBrick();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ve Khung Gam Nho (Bare Base)", btnStyle, GUILayout.Height(25f)))
                StripToSmallBareBase();
            if (GUILayout.Button("Lap Mau Co San", btnStyle, GUILayout.Height(25f)))
                LoadStarterTemplate();
            GUILayout.EndHorizontal();

            GUILayout.Space(3f);
            bool destOn = LegoVehicleVisualAndDamage.DestructionModeEnabled;
            string destBtn = destOn ? "Che Do Pha Huy Xe [K]: BAT (Co vang gach)" : "Che Do Pha Huy Xe [K]: TAT (Bat tu)";
            if (GUILayout.Button(destBtn, btnStyle, GUILayout.Height(25f)))
            {
                if (LegoRaceManagerAndHUD.Instance != null)
                    LegoRaceManagerAndHUD.Instance.ToggleDestructionMode();
                else
                    LegoVehicleVisualAndDamage.DestructionModeEnabled = !LegoVehicleVisualAndDamage.DestructionModeEnabled;
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("LUU XE & VAO DUA NGAY [TAB]", btnStyle, GUILayout.Height(30f)))
                ToggleGarage();

            GUILayout.EndArea();
        }
    }
}
