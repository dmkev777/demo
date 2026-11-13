using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    public struct VoxelBlockDef
    {
        public string name;
        public Color color;
        public float smoothness;
        public bool emissive;
        public BrickShape shape;

        public VoxelBlockDef(string n, Color c, float s = 0.18f, bool em = false, BrickShape shp = BrickShape.FlatTile)
        {
            name = n;
            color = c;
            smoothness = s;
            emissive = em;
            shape = shp;
        }
    }

    /// <summary>
    /// Tag attached to any breakable/modifiable Minecraft voxel block in the world.
    /// </summary>
    public class WorldVoxelBlockTag : MonoBehaviour
    {
        public Color blockColor = Color.white;
    }

    /// <summary>
    /// Full Minecraft-style Voxel Building System, 9-Slot Hotbar [1..9], Empty Hand [0],
    /// Full Block Inventory [B / I], 1m Voxel Grid Highlight, Left-Click Break, Right-Click Place!
    /// </summary>
    public class MinecraftWorldBuilderAndInventory : MonoBehaviour
    {
        public static MinecraftWorldBuilderAndInventory Instance { get; private set; }

        public const float VOXEL_SIZE = 1.0f;
        public const float BUILD_REACH = 10.5f;

        public bool IsInventoryOpen { get; private set; } = false;
        public int SelectedHotbarSlot { get; private set; } = 0; // 0..8 = Hotbar slots, -1 = Empty Hand (Tay Không)

        // All 16 Minecraft-style blocks available in the Full Inventory [B / I]
        public static readonly VoxelBlockDef[] AllBlockCatalog = new VoxelBlockDef[]
        {
            new VoxelBlockDef("Grass Block", new Color(0.28f, 0.54f, 0.22f), 0.12f),
            new VoxelBlockDef("Dirt Block", new Color(0.46f, 0.31f, 0.18f), 0.10f),
            new VoxelBlockDef("Cobblestone", new Color(0.44f, 0.46f, 0.48f), 0.15f),
            new VoxelBlockDef("Stone Brick", new Color(0.58f, 0.60f, 0.63f), 0.22f),
            new VoxelBlockDef("Oak Planks", new Color(0.66f, 0.46f, 0.24f), 0.20f),
            new VoxelBlockDef("Birch Wood", new Color(0.84f, 0.76f, 0.56f), 0.20f),
            new VoxelBlockDef("Red House Brick", new Color(0.72f, 0.24f, 0.20f), 0.22f),
            new VoxelBlockDef("White Quartz", new Color(0.90f, 0.90f, 0.88f), 0.35f),
            new VoxelBlockDef("Dark Roof Tile", new Color(0.20f, 0.22f, 0.26f), 0.28f),
            new VoxelBlockDef("Glass Window", new Color(0.55f, 0.85f, 0.96f), 0.88f),
            new VoxelBlockDef("Glowstone Lamp", new Color(0.98f, 0.82f, 0.25f), 0.50f, true),
            new VoxelBlockDef("Gold Block", new Color(0.94f, 0.72f, 0.12f), 0.75f),
            new VoxelBlockDef("Leaf Block", new Color(0.18f, 0.48f, 0.18f), 0.12f),
            new VoxelBlockDef("Sand Block", new Color(0.82f, 0.74f, 0.50f), 0.12f),
            new VoxelBlockDef("Cyan Neon Block", new Color(0.12f, 0.82f, 0.95f), 0.60f, true),
            new VoxelBlockDef("Roof Slope", new Color(0.68f, 0.26f, 0.18f), 0.25f, false, BrickShape.SlopeFront)
        };

        // 9 Hotbar slots storing indices into AllBlockCatalog
        private readonly int[] _hotbarCatalogIndices = new int[9] { 0, 3, 4, 6, 7, 9, 8, 10, 11 };

        private Transform _placedBlocksRoot;
        private GameObject _previewWireObj;
        private bool _hasValidBuildTarget;
        private Vector3 _targetPlaceWorldPos;
        private WorldVoxelBlockTag _hoveredBreakableBlock;
        private Texture2D _uiTex;

        private void Awake()
        {
            Instance = this;
            _uiTex = new Texture2D(1, 1);
            _uiTex.SetPixel(0, 0, Color.white);
            _uiTex.Apply();

            GameObject rootObj = new GameObject("PlayerPlaced_MinecraftBlocks");
            _placedBlocksRoot = rootObj.transform;

            CreatePreviewHighlightCube();
        }

        private void CreatePreviewHighlightCube()
        {
            _previewWireObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(_previewWireObj.GetComponent<Collider>());
            _previewWireObj.name = "VoxelBuildPreview";
            _previewWireObj.transform.localScale = Vector3.one * (VOXEL_SIZE * 1.01f);
            _previewWireObj.SetActive(false);
        }

        public bool IsActiveOnFoot =>
            LegoRaceManagerAndHUD.Instance != null &&
            LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot &&
            (LegoGarageBuilder.Instance == null || !LegoGarageBuilder.Instance.IsGarageOpen);

        public void ToggleInventory()
        {
            IsInventoryOpen = !IsInventoryOpen;
            if (LegoGameInput.Instance != null) LegoGameInput.Instance.RefreshState();
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.5f);
        }

        public void CloseInventory() { IsInventoryOpen = false; }

        private void Update()
        {
            if (!IsActiveOnFoot)
            {
                if (_previewWireObj != null) _previewWireObj.SetActive(false);
                IsInventoryOpen = false;
                return;
            }

            if (!LegoGameInput.GameplayAllowed)
            {
                if (_previewWireObj != null) _previewWireObj.SetActive(false);
                return;
            }

            // Number keys 1..9 select Hotbar Slots; 0 selects Empty Hand (Tay Không)
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
                SetHotbarSlot(-1);
            for (int s = 0; s < 9; s++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + s) || Input.GetKeyDown(KeyCode.Keypad1 + s))
                {
                    SetHotbarSlot(s);
                }
            }

            // Mouse Scroll Wheel cycles Hotbar slots (-1..8)
            if (!IsInventoryOpen && Mathf.Abs(Input.mouseScrollDelta.y) > 0.05f)
            {
                int dir = Input.mouseScrollDelta.y > 0f ? -1 : 1;
                int next = SelectedHotbarSlot + dir;
                if (next < -1) next = 8;
                if (next > 8) next = -1;
                SetHotbarSlot(next);
            }

            bool mpOpen = LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen;
            if (IsInventoryOpen || mpOpen)
            {
                if (_previewWireObj != null) _previewWireObj.SetActive(false);
                return;
            }

            UpdateVoxelTargetRaycast();

            if (LegoGameInput.GameplayAllowed)
            {
                // Right-Click = Place Block (when holding a block in Hotbar 0..8)
                if (Input.GetMouseButtonDown(1) && SelectedHotbarSlot >= 0 && _hasValidBuildTarget)
                {
                    PlaceVoxelBlockAt(_targetPlaceWorldPos);
                }

                // Left-Click = Break/Remove Block under Crosshair!
                if (Input.GetMouseButtonDown(0) && _hoveredBreakableBlock != null)
                {
                    BreakVoxelBlock(_hoveredBreakableBlock);
                }
            }
        }

        private void SetHotbarSlot(int slot)
        {
            SelectedHotbarSlot = slot;
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.4f);
        }

        private void UpdateVoxelTargetRaycast()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Raycast from center of screen (Crosshair)
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            _hasValidBuildTarget = false;
            _hoveredBreakableBlock = null;

            RaycastHit hit = default;
            float nearest = float.MaxValue;
            foreach (RaycastHit candidate in Physics.RaycastAll(ray, BUILD_REACH, ~0, QueryTriggerInteraction.Ignore))
            {
                if (candidate.collider.GetComponentInParent<LegoBlockCharacterController>() != null) continue;
                if (candidate.distance < nearest) { nearest = candidate.distance; hit = candidate; }
            }
            if (nearest < float.MaxValue)
            {
                // Ignore hitting the player character itself
                if (hit.collider.GetComponent<LegoBlockCharacterController>() != null)
                {
                    if (_previewWireObj != null) _previewWireObj.SetActive(false);
                    return;
                }

                _hoveredBreakableBlock = hit.collider.GetComponent<WorldVoxelBlockTag>();

                // Snap placement position to the 1x1x1 world voxel grid along hit normal
                Vector3 rawPos = hit.point + hit.normal * (VOXEL_SIZE * 0.48f);
                Vector3 snapped = new Vector3(
                    Mathf.Round(rawPos.x / VOXEL_SIZE) * VOXEL_SIZE,
                    Mathf.Floor(rawPos.y / VOXEL_SIZE) * VOXEL_SIZE + VOXEL_SIZE * 0.5f,
                    Mathf.Round(rawPos.z / VOXEL_SIZE) * VOXEL_SIZE
                );

                // Don't place a block directly inside the player's body
                LegoBlockCharacterController playerChar = LegoRaceManagerAndHUD.Instance != null
                    ? LegoRaceManagerAndHUD.Instance.PlayerCharacter
                    : null;
                if (playerChar != null && Vector3.Distance(snapped, playerChar.transform.position + Vector3.up * 0.9f) < 0.95f)
                {
                    if (_previewWireObj != null) _previewWireObj.SetActive(false);
                    return;
                }

                _hasValidBuildTarget = true;
                _targetPlaceWorldPos = snapped;

                if (_previewWireObj != null)
                {
                    bool showPreview = SelectedHotbarSlot >= 0;
                    _previewWireObj.SetActive(showPreview);
                    if (showPreview)
                    {
                        _previewWireObj.transform.position = _targetPlaceWorldPos;
                        VoxelBlockDef def = AllBlockCatalog[_hotbarCatalogIndices[SelectedHotbarSlot]];
                        _previewWireObj.transform.rotation = playerChar != null
                            ? Quaternion.Euler(0f, Mathf.Round(playerChar.CurrentLookYaw / 90f) * 90f, 0f)
                            : Quaternion.identity;
                        _previewWireObj.GetComponent<MeshRenderer>().sharedMaterial =
                            LegoMeshAndMaterialFactory.GetPlasticMaterial(def.color, true, def.smoothness);
                    }
                }
            }
            else if (_previewWireObj != null)
            {
                _previewWireObj.SetActive(false);
            }
        }

        private void PlaceVoxelBlockAt(Vector3 worldPos)
        {
            if (SelectedHotbarSlot < 0 || SelectedHotbarSlot >= 9) return;
            int catIdx = _hotbarCatalogIndices[SelectedHotbarSlot];
            VoxelBlockDef def = AllBlockCatalog[catIdx];

            LegoBlockCharacterController playerChar = LegoRaceManagerAndHUD.Instance != null
                ? LegoRaceManagerAndHUD.Instance.PlayerCharacter
                : null;
            float snappedYaw = playerChar != null ? Mathf.Round(playerChar.CurrentLookYaw / 90f) * 90f : 0f;
            Quaternion rot = Quaternion.Euler(0f, snappedYaw, 0f);

            SpawnWorldVoxelBlock(_placedBlocksRoot, worldPos, Vector3.one * VOXEL_SIZE, def, rot);

            if (LegoMultiplayerRoomManager.Instance != null)
                LegoMultiplayerRoomManager.Instance.BroadcastVoxelBlockPlaced(worldPos, snappedYaw, catIdx);

            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.75f);
        }

        public void SpawnNetworkVoxelBlock(Vector3 worldPos, float yaw, int catalogIndex)
        {
            int idx = Mathf.Clamp(catalogIndex, 0, AllBlockCatalog.Length - 1);
            SpawnWorldVoxelBlock(_placedBlocksRoot, worldPos, Vector3.one * VOXEL_SIZE, AllBlockCatalog[idx], Quaternion.Euler(0f, yaw, 0f));
        }

        public void BreakNetworkVoxelBlockAt(Vector3 worldPos)
        {
            Collider[] cols = Physics.OverlapSphere(worldPos, 0.35f);
            foreach (var c in cols)
            {
                WorldVoxelBlockTag tag = c.GetComponent<WorldVoxelBlockTag>();
                if (tag != null)
                {
                    Destroy(tag.gameObject);
                    break;
                }
            }
        }

        public static GameObject SpawnWorldVoxelBlock(
            Transform parent,
            Vector3 worldCenterPos,
            Vector3 scale,
            VoxelBlockDef def,
            Quaternion rotation)
        {
            GameObject block;
            if (def.shape == BrickShape.SlopeFront)
            {
                block = new GameObject($"Voxel_{def.name}");
                block.transform.SetParent(parent, false);
                block.transform.SetPositionAndRotation(worldCenterPos - new Vector3(0f, scale.y * 0.5f, 0f), rotation);
                MeshFilter mf = block.AddComponent<MeshFilter>();
                mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 3, 2, BrickShape.SlopeFront);
                MeshRenderer mr = block.AddComponent<MeshRenderer>();
                mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(def.color, def.emissive, def.smoothness);
                BoxCollider bc = block.AddComponent<BoxCollider>();
                bc.size = new Vector3(0.8f, 0.84f, 0.8f);
                bc.center = new Vector3(0f, 0.42f, 0f);
                block.transform.localScale = new Vector3(scale.x / 0.8f, scale.y / 0.84f, scale.z / 0.8f);
            }
            else
            {
                block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"Voxel_{def.name}";
                block.transform.SetParent(parent, false);
                block.transform.SetPositionAndRotation(worldCenterPos, rotation);
                block.transform.localScale = scale;
                block.GetComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetPlasticMaterial(def.color, def.emissive, def.smoothness);
            }

            WorldVoxelBlockTag tag = block.AddComponent<WorldVoxelBlockTag>();
            tag.blockColor = def.color;
            return block;
        }

        private void BreakVoxelBlock(WorldVoxelBlockTag target)
        {
            if (target == null) return;
            Vector3 pos = target.transform.position;
            Destroy(target.gameObject);

            if (LegoMultiplayerRoomManager.Instance != null)
                LegoMultiplayerRoomManager.Instance.BroadcastVoxelBlockBroken(pos);

            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickCrash();
        }

        public void DrawMinecraftHotbarAndInventoryGUI()
        {
            if (!IsActiveOnFoot) return;

            // 1. Center Screen Crosshair (+)
            if (LegoGameInput.GameplayAllowed)
            {
                float cx = Screen.width * 0.5f;
                float cy = Screen.height * 0.5f;
                DrawRect(new Rect(cx - 8f, cy - 1f, 16f, 2f), Color.white);
                DrawRect(new Rect(cx - 1f, cy - 8f, 2f, 16f), Color.white);
            }

            // 2. Bottom 10-Slot Minecraft Hotbar ([0: Tay Khong] + [1..9: Blocks])
            float slotW = 58f;
            float slotH = 58f;
            float gap = 4f;
            int totalSlots = 10; // Slot 0 (Empty Hand) + Slots 1..9
            float barW = totalSlots * (slotW + gap) + 12f;
            float barX = (Screen.width - barW) * 0.5f;
            float barY = Screen.height - slotH - 14f;

            DrawRect(new Rect(barX, barY, barW, slotH + 10f), new Color(0.08f, 0.10f, 0.13f, 0.90f));

            GUIStyle keyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            keyStyle.normal.textColor = LegoPalette.BrightYellow;

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerCenter,
                wordWrap = true
            };
            nameStyle.normal.textColor = Color.white;

            // Slot 0: Empty Hand (Tay Không)
            Rect s0Rect = new Rect(barX + 6f, barY + 5f, slotW, slotH);
            bool s0Selected = (SelectedHotbarSlot == -1);
            DrawRect(s0Rect, s0Selected ? LegoPalette.BrightGreen : new Color(0.22f, 0.25f, 0.30f, 1f));
            DrawRect(new Rect(s0Rect.x + 3f, s0Rect.y + 3f, slotW - 6f, slotH - 6f), new Color(0.14f, 0.16f, 0.20f, 1f));
            GUI.Label(new Rect(s0Rect.x + 5f, s0Rect.y + 3f, 30f, 18f), "[0]", keyStyle);
            GUI.Label(new Rect(s0Rect.x + 2f, s0Rect.y + 18f, slotW - 4f, slotH - 20f), "Tay\nKhong", nameStyle);
            if (GUI.Button(s0Rect, "", GUIStyle.none))
                SetHotbarSlot(-1);

            // Slots 1..9: Building Blocks
            for (int i = 0; i < 9; i++)
            {
                float sx = barX + 6f + (i + 1) * (slotW + gap);
                Rect sRect = new Rect(sx, barY + 5f, slotW, slotH);
                bool selected = (SelectedHotbarSlot == i);
                VoxelBlockDef def = AllBlockCatalog[_hotbarCatalogIndices[i]];

                DrawRect(sRect, selected ? LegoPalette.BrightYellow : new Color(0.22f, 0.25f, 0.30f, 1f));
                DrawRect(new Rect(sRect.x + 3f, sRect.y + 3f, slotW - 6f, slotH - 6f), new Color(0.14f, 0.16f, 0.20f, 1f));

                // Block Color Swatch
                DrawRect(new Rect(sRect.x + 12f, sRect.y + 14f, slotW - 24f, 20f), def.color);

                GUI.Label(new Rect(sRect.x + 5f, sRect.y + 2f, 30f, 16f), $"[{i + 1}]", keyStyle);
                GUI.Label(new Rect(sRect.x + 2f, sRect.y + 33f, slotW - 4f, 24f), def.name, nameStyle);

                if (GUI.Button(sRect, "", GUIStyle.none))
                    SetHotbarSlot(i);
            }

            // Active Block Title above Hotbar
            string activeTitle = SelectedHotbarSlot < 0
                ? "Dang cam: TAY KHONG [0]  |  Bam [1-9] hoac Lan Chuot de chon Block xay nha  |  [B] Mo Tui Do Block"
                : $"Dang cam: [{SelectedHotbarSlot + 1}] {AllBlockCatalog[_hotbarCatalogIndices[SelectedHotbarSlot]].name}  |  Click Trai: Pha Block  |  Click Phai: Dat Block  |  [B] Tui Do";

            GUIStyle barTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            barTitleStyle.normal.textColor = LegoPalette.BrightYellow;
            GUI.Label(new Rect(barX, barY - 24f, barW, 22f), activeTitle, barTitleStyle);

            // 3. Full Minecraft Block Inventory Window ([B] or [I])
            if (IsInventoryOpen)
            {
                DrawFullInventoryModal();
            }
        }

        private void DrawFullInventoryModal()
        {
            float winW = 540f;
            float winH = 380f;
            Rect winRect = new Rect((Screen.width - winW) * 0.5f, (Screen.height - winH) * 0.5f - 20f, winW, winH);

            DrawRect(winRect, new Color(0.07f, 0.09f, 0.12f, 0.96f));
            DrawRect(new Rect(winRect.x, winRect.y, winW, 4f), LegoPalette.BrightYellow);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = LegoPalette.BrightYellow;

            GUI.Label(new Rect(winRect.x, winRect.y + 10f, winW, 28f),
                "TUI DO BLOCK XAY NHA (MINECRAFT INVENTORY)", titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = Color.white;
            int targetSlot = SelectedHotbarSlot >= 0 ? SelectedHotbarSlot : 0;
            GUI.Label(new Rect(winRect.x, winRect.y + 38f, winW, 22f),
                $"Click chon 1 loai Block ben duoi de gan vao O Phim [{targetSlot + 1}] (Hoac bam [B] de dong):", subStyle);

            int cols = 4;
            float cellW = 120f;
            float cellH = 58f;
            float startX = winRect.x + 20f;
            float startY = winRect.y + 70f;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            for (int i = 0; i < AllBlockCatalog.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                Rect cRect = new Rect(startX + c * (cellW + 8f), startY + r * (cellH + 8f), cellW, cellH);

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = AllBlockCatalog[i].color;
                if (GUI.Button(cRect, AllBlockCatalog[i].name, btnStyle))
                {
                    if (SelectedHotbarSlot < 0) SelectedHotbarSlot = 0;
                    _hotbarCatalogIndices[SelectedHotbarSlot] = i;
                    IsInventoryOpen = false;
                    LegoGameInput.Instance.RefreshState();
                    if (LegoProceduralAudio.Instance != null)
                        LegoProceduralAudio.Instance.PlayBrickClick(0.6f);
                }
                GUI.backgroundColor = prev;
            }

            if (GUI.Button(new Rect(winRect.x + (winW - 180f) * 0.5f, winRect.y + winH - 42f, 180f, 30f), "DONG TUI DO [B]", btnStyle))
            {
                IsInventoryOpen = false;
                LegoGameInput.Instance.RefreshState();
            }
        }

        private void DrawRect(Rect rect, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            if (_uiTex != null)
                GUI.DrawTexture(rect, _uiTex);
            GUI.color = prev;
        }
    }
}
