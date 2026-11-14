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

        public static bool HasModal => Instance != null && Instance.IsPauseMenuOpen ||
            LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen ||
            LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen ||
            LegoRaceLobby.Instance != null && LegoRaceLobby.Instance.IsPanelOpen;

        public static bool IsGarageAimingActive =>
            LegoGarageBuilder.Instance != null &&
            LegoGarageBuilder.Instance.IsGarageOpen &&
            !(Instance != null && Instance.IsPauseMenuOpen) &&
            !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt);

        public static bool GameplayAllowed => Instance != null && !HasModal &&
            Time.frameCount > Instance._blockedThroughFrame;

        public static bool CharacterControlAllowed =>
            Instance != null &&
            Time.frameCount > Instance._blockedThroughFrame &&
            (!HasModal || (IsGarageAimingActive && LegoGarageBuilder.Instance != null && !LegoGarageBuilder.Instance.IsCarInspectView));

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

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                var garage = LegoGarageBuilder.Instance;
                bool canOpenFromPause = garage != null && (garage.IsGarageOpen || garage.IsInTestDrive ||
                    (LegoRaceManagerAndHUD.Instance != null && LegoRaceManagerAndHUD.Instance.CanOpenGarageNow));
                if (!IsPauseMenuOpen || canOpenFromPause)
                {
                    ToggleGarage();
                }
            }
            else if (!IsPauseMenuOpen && Input.GetKeyDown(KeyCode.P))
            {
                ToggleRoom();
            }

            // Keep OS cursor hidden & locked during active gameplay and while aiming in Garage (Alt temporarily releases it for UI clicks).
            bool lockCursor = !HasModal || IsGarageAimingActive;
            if (lockCursor)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                    Cursor.lockState = CursorLockMode.Locked;
                if (Cursor.visible)
                    Cursor.visible = false;
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.None;
                if (!Cursor.visible)
                    Cursor.visible = true;
            }
            RefreshTimeScale();
        }

        public void ClosePanels()
        {
            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
                LegoGarageBuilder.Instance.CloseGarageToOnFoot();
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
            var garage = LegoGarageBuilder.Instance;
            if (garage == null) return;

            if (garage.IsInTestDrive)
            {
                ClosePanels();
                garage.ExitTestDriveToGarage();
                RefreshState();
                return;
            }

            bool wasOpen = garage.IsGarageOpen;
            if (wasOpen)
            {
                ClosePanels();
                RefreshState();
                return;
            }

            bool canOpen = LegoRaceManagerAndHUD.Instance == null || LegoRaceManagerAndHUD.Instance.CanOpenGarageNow;
            if (!canOpen)
            {
                garage.NotifyMustVisitGarageZone();
                return;
            }

            ClosePanels();
            garage.ToggleGarage();
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
            // World inventory removed in favor of in-Garage car customization.
        }

        public void RefreshState()
        {
            _blockedThroughFrame = Time.frameCount + 1; // Consume the click/key that closed a menu.
            bool lockCursor = !HasModal || IsGarageAimingActive;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
            RefreshTimeScale();
        }

        private void RefreshTimeScale()
        {
            bool online = LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsConnected;
            Time.timeScale = (!online && IsPauseMenuOpen) ? 0f : 1f;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && (!HasModal || IsGarageAimingActive))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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
            bool inTestDrive = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsInTestDrive;
            bool canGarage = inTestDrive || (LegoRaceManagerAndHUD.Instance != null && LegoRaceManagerAndHUD.Instance.CanOpenGarageNow);
            GUI.enabled = canGarage;
            string garageLabel = inTestDrive ? "THOAT LAI THU -> GARAGE [TAB]" : "GARAGE [TAB]";
            if (GUILayout.Button(garageLabel, GUILayout.Height(38))) ToggleGarage();
            GUI.enabled = true;
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
