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
#pragma warning disable CS0414
        private bool _hasValidBuildTarget;
        private Vector3 _targetPlaceWorldPos;
        private WorldVoxelBlockTag _hoveredBreakableBlock;
#pragma warning restore CS0414
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
            // Minecraft-style world block placement/breaking and hotbar inventory are disabled.
            IsInventoryOpen = false;
            SelectedHotbarSlot = -1;
            _hasValidBuildTarget = false;
            _hoveredBreakableBlock = null;
            if (_previewWireObj != null && _previewWireObj.activeSelf)
                _previewWireObj.SetActive(false);
        }

        private void SetHotbarSlot(int slot)
        {
            SelectedHotbarSlot = -1;
        }

        private void UpdateVoxelTargetRaycast()
        {
            _hasValidBuildTarget = false;
            _hoveredBreakableBlock = null;
            if (_previewWireObj != null) _previewWireObj.SetActive(false);
        }

        private void PlaceVoxelBlockAt(Vector3 worldPos) { }

        public void SpawnNetworkVoxelBlock(Vector3 worldPos, float yaw, int catalogIndex) { }

        public void BreakNetworkVoxelBlockAt(Vector3 worldPos) { }

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
                mr.sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(def.color, def.emissive, 0.22f);
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
                    LegoMeshAndMaterialFactory.GetLowPolyMaterial(def.color, def.emissive, 0.22f);
            }

            WorldVoxelBlockTag tag = block.AddComponent<WorldVoxelBlockTag>();
            tag.blockColor = def.color;
            return block;
        }

        private void BreakVoxelBlock(WorldVoxelBlockTag target) { }

        public void DrawMinecraftHotbarAndInventoryGUI()
        {
            // World building HUD & hotbar removed cleanly; vehicle assembly happens inside Garage only.
        }
    }
}
