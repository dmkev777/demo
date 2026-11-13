using UnityEngine;

namespace Lego2KDrive
{
    // Handle menus before any character, vehicle or building input reads this frame.
    [DefaultExecutionOrder(-2000)]
    public class LegoGameInput : MonoBehaviour
    {
        public static LegoGameInput Instance { get; private set; }
        public bool IsPauseMenuOpen { get; private set; }
        private int _blockedThroughFrame;
        private bool _hadPointerLock;

        public static bool HasModal => Instance != null && Instance.IsPauseMenuOpen ||
            LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen ||
            MinecraftWorldBuilderAndInventory.Instance != null && MinecraftWorldBuilderAndInventory.Instance.IsInventoryOpen ||
            LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen ||
            LegoRaceLobby.Instance != null && LegoRaceLobby.Instance.IsPanelOpen;

        public static bool GameplayAllowed => Instance != null && !HasModal &&
            Cursor.lockState == CursorLockMode.Locked && Time.frameCount > Instance._blockedThroughFrame;

        private void Awake() { Instance = this; }
        private void Start() { RefreshState(); }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsPauseMenuOpen) Resume();
                else if (HasModal) ClosePanels();
                else OpenPauseMenu();
                RefreshState();
                return;
            }

            if (!IsPauseMenuOpen)
            {
                if (Input.GetKeyDown(KeyCode.Tab)) ToggleGarage();
                else if (Input.GetKeyDown(KeyCode.P)) ToggleRoom();
                else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I)) ToggleInventory();
            }

            // Browser / Editor may release pointer lock themselves (Escape, tab switch).
            if (!HasModal && Cursor.lockState != CursorLockMode.Locked &&
                (_hadPointerLock || Time.frameCount > _blockedThroughFrame + 2))
                OpenPauseMenu();
            _hadPointerLock = Cursor.lockState == CursorLockMode.Locked;
            RefreshTimeScale();
        }

        public void ClosePanels()
        {
            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
                LegoGarageBuilder.Instance.ToggleGarage();
            if (MinecraftWorldBuilderAndInventory.Instance != null)
                MinecraftWorldBuilderAndInventory.Instance.CloseInventory();
            if (LegoMultiplayerRoomManager.Instance != null)
                LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen = false;
            if (LegoRaceLobby.Instance != null) LegoRaceLobby.Instance.ClosePanel();
            IsPauseMenuOpen = false;
            RefreshState();
        }

        public void OpenPauseMenu()
        {
            ClosePanels();
            IsPauseMenuOpen = true;
            RefreshState();
        }

        public void Resume()
        {
            IsPauseMenuOpen = false;
            RefreshState();
        }

        public void ToggleGarage()
        {
            bool wasOpen = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen;
            ClosePanels();
            if (!wasOpen && LegoGarageBuilder.Instance != null) LegoGarageBuilder.Instance.ToggleGarage();
            RefreshState();
        }

        public void ToggleRoom()
        {
            bool wasOpen = LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen;
            ClosePanels();
            if (LegoMultiplayerRoomManager.Instance != null) LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen = !wasOpen;
            RefreshState();
        }

        public void ToggleInventory()
        {
            var manager = LegoRaceManagerAndHUD.Instance;
            if (manager == null || !manager.IsPlayerOnFoot) return;
            bool wasOpen = MinecraftWorldBuilderAndInventory.Instance != null && MinecraftWorldBuilderAndInventory.Instance.IsInventoryOpen;
            ClosePanels();
            if (!wasOpen && MinecraftWorldBuilderAndInventory.Instance != null) MinecraftWorldBuilderAndInventory.Instance.ToggleInventory();
            RefreshState();
        }

        public void RefreshState()
        {
            _blockedThroughFrame = Time.frameCount + 1; // Consume the click/key that closed a menu.
            Cursor.lockState = HasModal ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = HasModal;
            _hadPointerLock = Cursor.lockState == CursorLockMode.Locked;
            RefreshTimeScale();
        }

        private void RefreshTimeScale()
        {
            bool online = LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsConnected;
            bool garage = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen;
            Time.timeScale = !online && (IsPauseMenuOpen || garage) ? 0f : 1f;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) OpenPauseMenu();
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Instance == this) Instance = null;
        }

        public void DrawPauseMenu()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            float width = Mathf.Min(400f, Screen.width - 24f);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2f, Mathf.Max(12f, (Screen.height - 400f) / 2f), width, 400f), GUI.skin.box);
            GUILayout.Label("MENU [ESC]", new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter });
            bool online = LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsConnected;
            GUILayout.Label(online ? "Dang online: cuoc dua van tiep tuc." : "Game dang tam dung.");
            if (GUILayout.Button("TIEP TUC [ESC]", GUILayout.Height(38))) Resume();
            if (GUILayout.Button("GARAGE [TAB]", GUILayout.Height(38))) ToggleGarage();
            if (LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot && GUILayout.Button("TUI DO [B / I]", GUILayout.Height(38))) ToggleInventory();
            if (GUILayout.Button("PHONG MULTIPLAYER [P]", GUILayout.Height(38))) ToggleRoom();
            if (LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot)
            {
                GUI.enabled = LegoRaceManagerAndHUD.Instance.IsInsideRaceZone;
                if (GUILayout.Button("DANG KY SLOT DUA XE", GUILayout.Height(38)))
                {
                    Resume();
                    LegoRaceLobby.Instance.OpenPanel();
                }
                GUI.enabled = true;
            }
            else if (GUILayout.Button("ROI CUOC DUA / VE THE GIOI MO", GUILayout.Height(38)))
            {
                LegoRaceManagerAndHUD.Instance.EnterOpenWorldOnFootMode(false);
                Resume();
            }
            GUILayout.Label("Di chuot: nhin quanh | [V]: doi goc nhin\nVao vong sang xanh de xac nhan slot dua.");
            GUILayout.EndArea();
        }
    }
}
