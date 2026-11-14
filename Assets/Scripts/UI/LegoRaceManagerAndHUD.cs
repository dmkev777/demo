using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    /// <summary>
    /// Master Open-World & Race Coordinator, HUD Renderer, and Automatic Scene Bootstrapper.
    /// Supports exploring the Minecraft-style Voxel Open World on foot (1st & 3rd Person)
    /// and walking up to the Race Beacon to hop into your custom vehicle and race!
    /// </summary>
    public class LegoRaceManagerAndHUD : MonoBehaviour
    {
        public static LegoRaceManagerAndHUD Instance { get; private set; }

        public int totalLaps = 3;
        public bool IsPlayerOnFoot { get; private set; } = true;
        public bool CanRacersDrive { get; private set; } = false;
        public List<ArcadeKartController> AllRacers { get; private set; } = new List<ArcadeKartController>();
        public ArcadeKartController PlayerKart { get; private set; }
        public LegoBlockCharacterController PlayerCharacter { get; private set; }
        public IReadOnlyList<Vector3> TrackWaypoints => _trackData != null ? _trackData.waypoints : null;

        private LegoTrackAndWorldBuilder.BuiltTrackData _trackData;
        private string _centerBannerText = "";
        private float _centerBannerTimer = 0f;
        private float _raceElapsedTime = 0f;
        private bool _raceFinished = false;
        private int _playerFinishRank = 1;
        private Texture2D _whiteTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnPlay()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<LegoRaceManagerAndHUD>() != null)
                return;
#else
            if (Object.FindObjectOfType<LegoRaceManagerAndHUD>() != null)
                return;
#endif
            GameObject managerObj = new GameObject("Lego2KDrive_GameManager");
            managerObj.AddComponent<LegoRaceManagerAndHUD>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
#pragma warning disable CS0618
            Physics.autoSyncTransforms = true;
#pragma warning restore CS0618

            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        private void Start()
        {
            SetupCompleteGameWorld();
            // Start in Open-World On-Foot mode so the player can walk around trees, lake, hills & walk to the Race Zone!
            EnterOpenWorldOnFootMode(true);
        }

        public void SetupCompleteGameWorld()
        {
            SetupLighting();

            if (GetComponent<LegoProceduralAudio>() == null)
                gameObject.AddComponent<LegoProceduralAudio>();

            // Build the Bricklandia 3-Biome Circuit + Minecraft Voxel Open World Hub
            _trackData = LegoTrackAndWorldBuilder.BuildBricklandiaCircuit();

            if (GetComponent<LegoGameInput>() == null) gameObject.AddComponent<LegoGameInput>();
            if (GetComponent<LegoRaceLobby>() == null) gameObject.AddComponent<LegoRaceLobby>();

            // Only real players race. Empty slots stay empty.
            AllRacers.Clear();
            PlayerKart = SpawnRacer("YOU (Clutch)", LegoPalette.BrightOrange, LegoPalette.SilverMetallic);

            // Spawn Blocky Character for Open-World Exploration
            GameObject charObj = new GameObject("BlockyPlayerCharacter");
            charObj.transform.SetPositionAndRotation(_trackData.characterSpawnPos, _trackData.characterSpawnRot);
            PlayerCharacter = charObj.AddComponent<LegoBlockCharacterController>();

            // Setup Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            LegoArcadeCamera arcadeCam = mainCam.GetComponent<LegoArcadeCamera>();
            if (arcadeCam == null)
                arcadeCam = mainCam.gameObject.AddComponent<LegoArcadeCamera>();
            arcadeCam.targetKart = PlayerKart;
            arcadeCam.targetCharacter = PlayerCharacter;

            // Setup Garage Brick Builder
            LegoGarageBuilder garage = GetComponent<LegoGarageBuilder>();
            if (garage == null)
                garage = gameObject.AddComponent<LegoGarageBuilder>();
            garage.playerKart = PlayerKart;

            // Setup Top-Right Minimap Renderer
            if (GetComponent<LegoMinimapRenderer>() == null)
                gameObject.AddComponent<LegoMinimapRenderer>();

            // Setup Minecraft stub component (world building disabled)
            if (GetComponent<MinecraftWorldBuilderAndInventory>() == null)
                gameObject.AddComponent<MinecraftWorldBuilderAndInventory>();

            // Setup Cross-Platform PC Multiplayer Room Manager
            if (GetComponent<LegoMultiplayerRoomManager>() == null)
                gameObject.AddComponent<LegoMultiplayerRoomManager>();
        }

        private void SetupLighting()
        {
#if UNITY_2023_1_OR_NEWER
            Light existingLight = Object.FindFirstObjectByType<Light>();
#else
            Light existingLight = Object.FindObjectOfType<Light>();
#endif
            if (existingLight == null)
            {
                GameObject lightObj = new GameObject("Directional Sun Light");
                existingLight = lightObj.AddComponent<Light>();
                existingLight.type = LightType.Directional;
            }
            // Warm relaxed golden-hour sunlight matching LowPolySunriseSun in northern sky
            existingLight.transform.rotation = Quaternion.LookRotation(new Vector3(160f, -175f, -380f).normalized, Vector3.up);
            existingLight.color = new Color(1.0f, 0.88f, 0.72f);
            existingLight.intensity = 0.88f;
            existingLight.shadows = LightShadows.Soft;
            existingLight.shadowStrength = 0.68f;
            existingLight.shadowBias = 0.03f;
            existingLight.shadowNormalBias = 0.35f;

            GameObject fillObj = new GameObject("Sky Fill Light");
            fillObj.transform.SetParent(transform, false);
            fillObj.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            Light fillLight = fillObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.52f, 0.68f, 0.88f);
            fillLight.intensity = 0.14f;
            fillLight.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.48f, 0.64f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.34f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.22f, 0.18f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.82f, 0.78f, 0.74f);
            RenderSettings.fogDensity = 0.00055f;

            QualitySettings.shadowDistance = 220f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        }

        private ArcadeKartController SpawnRacer(
            string name,
            Color primary,
            Color accent)
        {
            GameObject kartObj = new GameObject($"LegoRacer_{name}");
            kartObj.transform.position = _trackData.startGridPositions[0];
            kartObj.transform.rotation = _trackData.startGridRotation;

            kartObj.SetActive(false);
            kartObj.AddComponent<Rigidbody>();
            kartObj.AddComponent<BoxCollider>();
            kartObj.AddComponent<LegoVehicleVisualAndDamage>();
            ArcadeKartController controller = kartObj.AddComponent<ArcadeKartController>();

            controller.racerName = name;
            controller.isPlayer = true;
            controller.primaryColor = primary;
            controller.accentColor = accent;
            kartObj.SetActive(true);

            controller.SetTrackWaypoints(_trackData.waypoints, 1);

            AllRacers.Add(controller);
            return controller;
        }

        private void SnapCameraImmediate()
        {
            if (Camera.main == null) return;
            LegoArcadeCamera arcadeCam = Camera.main.GetComponent<LegoArcadeCamera>();
            if (arcadeCam != null) arcadeCam.SnapToTargetImmediate();
        }

        /// <summary>
        /// Switches to On-Foot Open-World Mode (Character walking around the Minecraft voxel world).
        /// </summary>
        public void EnterOpenWorldOnFootMode(bool spawnAtVillage)
        {
            StopAllCoroutines();
            if (LegoRaceLobby.Instance != null) LegoRaceLobby.Instance.Leave();
            if (LegoGarageBuilder.Instance != null) LegoGarageBuilder.Instance.ForceExitAllGarageModes();

            IsPlayerOnFoot = true;
            CanRacersDrive = false;
            _raceFinished = false;

            for (int i = 0; i < AllRacers.Count; i++)
            {
                AllRacers[i].ResetForRaceStart(_trackData.startGridPositions[i], _trackData.startGridRotation);
            }

            if (PlayerCharacter != null)
            {
                PlayerCharacter.gameObject.SetActive(true);
                if (spawnAtVillage)
                {
                    PlayerCharacter.transform.SetPositionAndRotation(_trackData.characterSpawnPos, _trackData.characterSpawnRot);
                    PlayerCharacter.CurrentLookYaw = _trackData.characterSpawnRot.eulerAngles.y;
                }
                else
                {
                    PlayerCharacter.transform.position = _trackData.raceEntryBeaconPos +
                        _trackData.startGridRotation * Vector3.back * (LegoRaceLobby.EntryRadius + 2f) + Vector3.up * 1.2f;
                }
                LegoMeshAndMaterialFactory.SetVelocity(PlayerCharacter.GetComponent<Rigidbody>(), Vector3.zero);
            }
            if (PlayerKart != null) PlayerKart.gameObject.SetActive(false);
            SnapCameraImmediate();

            _centerBannerText = "MOTORVERSE: DEN CONG GARAGE [TAB] DE DO XE HOAC DEN VONG SANG DE DUA!";
            _centerBannerTimer = 3.5f;
        }

        public void StageCarInsideGarageWorkshop()
        {
            StopAllCoroutines();
            if (LegoRaceLobby.Instance != null) LegoRaceLobby.Instance.Leave();
            IsPlayerOnFoot = true;
            CanRacersDrive = false;

            if (PlayerKart != null && _trackData != null)
            {
                PlayerKart.gameObject.SetActive(true);
                PlayerKart.TeleportPoseImmediate(_trackData.garageInteriorCarPos, _trackData.garageInteriorCarRot);
            }

            if (PlayerCharacter != null && _trackData != null)
            {
                PlayerCharacter.gameObject.SetActive(true);
                Vector3 carPos = _trackData.garageInteriorCarPos;
                Vector3 toDoor = _trackData.garageEntryPos - carPos;
                toDoor.y = 0f;
                if (toDoor.sqrMagnitude < 0.01f) toDoor = Vector3.forward;
                toDoor.Normalize();
                Vector3 rightDir = Vector3.Cross(Vector3.up, toDoor).normalized;

                Vector2 charXZ = new Vector2(PlayerCharacter.transform.position.x, PlayerCharacter.transform.position.z);
                Vector2 carXZ = new Vector2(carPos.x, carPos.z);
                float distToCar = Vector2.Distance(charXZ, carXZ);
                if (distToCar > 6.2f || distToCar < 2.6f)
                {
                    Vector3 standPos = carPos + toDoor * 3.2f - rightDir * 2.2f + Vector3.up * 0.25f;
                    PlayerCharacter.transform.position = standPos;
                }

                Vector3 lookDir = (carPos + Vector3.up * 0.45f) - (PlayerCharacter.transform.position + Vector3.up * 1.5f);
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    PlayerCharacter.CurrentLookYaw = lookRot.eulerAngles.y;
                    PlayerCharacter.CurrentLookPitch = 14f;
                    PlayerCharacter.transform.rotation = Quaternion.Euler(0f, PlayerCharacter.CurrentLookYaw, 0f);
                }
                LegoMeshAndMaterialFactory.SetVelocity(PlayerCharacter.GetComponent<Rigidbody>(), Vector3.zero);
            }
            SnapCameraImmediate();
        }

        public void ExitGarageToDoorOnFoot()
        {
            StopAllCoroutines();
            IsPlayerOnFoot = true;
            CanRacersDrive = false;
            _raceFinished = false;

            if (PlayerKart != null)
            {
                if (_trackData != null && _trackData.startGridPositions != null && _trackData.startGridPositions.Length > 0)
                {
                    PlayerKart.TeleportPoseImmediate(_trackData.startGridPositions[0], _trackData.startGridRotation);
                }
                PlayerKart.gameObject.SetActive(false);
            }

            if (PlayerCharacter != null && _trackData != null)
            {
                PlayerCharacter.gameObject.SetActive(true);
                Vector3 fwd = _trackData.garageInteriorCarRot * Vector3.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                PlayerCharacter.transform.position = _trackData.garageEntryPos + fwd.normalized * 4.0f + Vector3.up * 1.15f;
                LegoMeshAndMaterialFactory.SetVelocity(PlayerCharacter.GetComponent<Rigidbody>(), Vector3.zero);
            }
            SnapCameraImmediate();
        }

        public void EnterTestDriveArenaMode()
        {
            StopAllCoroutines();
            if (LegoRaceLobby.Instance != null) LegoRaceLobby.Instance.Leave();
            IsPlayerOnFoot = false;
            CanRacersDrive = true;
            _raceFinished = false;

            if (PlayerCharacter != null)
                PlayerCharacter.gameObject.SetActive(false);

            if (PlayerKart != null && _trackData != null)
            {
                PlayerKart.gameObject.SetActive(true);
                PlayerKart.ResetForRaceStart(_trackData.testArenaSpawnPos, _trackData.testArenaSpawnRot);
            }
            SnapCameraImmediate();
        }

        /// <summary>
        /// Hops into the Player's custom LEGO vehicle and starts the Race Countdown!
        /// </summary>
        public void StartConfirmedRace(int slotIndex)
        {
            if (LegoRaceLobby.Instance == null || !LegoRaceLobby.Instance.OwnsGridSlot(slotIndex) ||
                slotIndex < 0 || slotIndex >= _trackData.startGridPositions.Length) return;
            StopAllCoroutines();
            if (LegoGarageBuilder.Instance != null)
                LegoGarageBuilder.Instance.ForceExitAllGarageModes();

            IsPlayerOnFoot = false;
            if (PlayerCharacter != null)
            {
                PlayerCharacter.gameObject.SetActive(false);
            }

            PlayerKart.gameObject.SetActive(true);
            PlayerKart.ResetForRaceStart(_trackData.startGridPositions[slotIndex], _trackData.startGridRotation);
            SnapCameraImmediate();

            _raceFinished = false;
            _raceElapsedTime = 0f;
            StartCoroutine(RaceCountdownRoutine());
        }

        public void RestartRace()
        {
            // Re-enter through the circle and confirm again; never restart the whole room.
            EnterOpenWorldOnFootMode(false);
        }

        private IEnumerator RaceCountdownRoutine()
        {
            CanRacersDrive = false;
            _centerBannerTimer = 10f;

            string[] steps = { "3", "2", "1", "BUILD & DRIVE!" };
            for (int i = 0; i < steps.Length; i++)
            {
                _centerBannerText = steps[i];
                bool isGo = (i == steps.Length - 1);
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayCountdownBeep(isGo);

                if (isGo)
                {
                    CanRacersDrive = true;
                    _centerBannerTimer = 1.2f;
                }
                yield return new WaitForSeconds(0.95f);
            }
        }

        public void OnRacerCompletedLap(ArcadeKartController kart)
        {
            if (kart.CurrentLap > totalLaps)
            {
                if (kart == PlayerKart && !_raceFinished)
                {
                    _playerFinishRank = GetRacerPosition(PlayerKart);
                    _raceFinished = true;
                    CanRacersDrive = false;
                    if (LegoRaceLobby.Instance != null) LegoRaceLobby.Instance.Finish();
                    _centerBannerText = $"FINISH! YOU PLACED {FormatRank(_playerFinishRank)}!";
                    _centerBannerTimer = 8f;
                    if (LegoProceduralAudio.Instance != null)
                        LegoProceduralAudio.Instance.PlayPowerUp();
                }
            }
            else if (kart == PlayerKart)
            {
                _centerBannerText = kart.CurrentLap == totalLaps ? "FINAL LAP!" : $"LAP {kart.CurrentLap} / {totalLaps}";
                _centerBannerTimer = 2.2f;
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayPowerUp();
            }
        }

        public int GetRacerPosition(ArcadeKartController target)
        {
            if (target == PlayerKart && _raceFinished) return _playerFinishRank;
            if (target == PlayerKart && LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsConnected)
                return LegoMultiplayerRoomManager.Instance.GetLocalRaceRank(target.TotalRaceProgressScore);
            int rank = 1;
            foreach (var r in AllRacers)
            {
                if (r != null && r != target && r.TotalRaceProgressScore > target.TotalRaceProgressScore)
                    rank++;
            }
            return rank;
        }

        public float GetDistanceToRaceZone()
        {
            if (PlayerCharacter == null || _trackData == null) return 999f;
            Vector3 offset = PlayerCharacter.transform.position - _trackData.raceEntryBeaconPos;
            return new Vector2(offset.x, offset.z).magnitude;
        }

        public Vector3 RaceEntryPosition => _trackData.raceEntryBeaconPos;
        public bool IsInsideRaceZone => IsPlayerOnFoot && PlayerCharacter != null && _trackData != null &&
            GetDistanceToRaceZone() <= LegoRaceLobby.EntryRadius &&
            Mathf.Abs(PlayerCharacter.transform.position.y - _trackData.raceEntryBeaconPos.y) <= 3f;

        public Vector3 GarageEntryPosition => _trackData != null ? _trackData.garageEntryPos : Vector3.zero;
        public Vector3 GarageInteriorCarPos => _trackData != null ? _trackData.garageInteriorCarPos : Vector3.zero;
        public Quaternion GarageInteriorCarRot => _trackData != null ? _trackData.garageInteriorCarRot : Quaternion.identity;
        public Bounds TestArenaBounds => _trackData != null ? _trackData.testArenaBounds : new Bounds();

        public float GetDistanceToGarageZone()
        {
            if (PlayerCharacter == null || _trackData == null) return float.MaxValue;
            Vector3 charPos = PlayerCharacter.transform.position;
            Vector2 p = new Vector2(charPos.x, charPos.z);
            float dGate = Vector2.Distance(p, new Vector2(_trackData.garageEntryPos.x, _trackData.garageEntryPos.z));
            float dBay  = Vector2.Distance(p, new Vector2(_trackData.garageInteriorCarPos.x, _trackData.garageInteriorCarPos.z));
            return Mathf.Min(dGate, dBay);
        }

        public bool IsInsideGarageZone => IsPlayerOnFoot && PlayerCharacter != null && _trackData != null &&
            GetDistanceToGarageZone() <= 8.5f &&
            Mathf.Abs(PlayerCharacter.transform.position.y - _trackData.garageEntryPos.y) <= 5.0f;

        public bool CanOpenGarageNow => IsInsideGarageZone;

        private void Update()
        {
            bool inTestDrive = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsInTestDrive;
            if (!IsPlayerOnFoot && !inTestDrive && CanRacersDrive && !_raceFinished)
            {
                _raceElapsedTime += Time.deltaTime;
            }

            if (_centerBannerTimer > 0f)
            {
                _centerBannerTimer -= Time.deltaTime;
            }

            if (IsPlayerOnFoot && IsInsideGarageZone && !LegoGameInput.HasModal && LegoGameInput.Instance != null)
            {
                // Garage only opens when the player explicitly presses [TAB], [E], [F], or [Enter] inside the zone.
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return))
                {
                    LegoGameInput.Instance.ToggleGarage();
                    return;
                }
            }

            if (!LegoGameInput.GameplayAllowed) return;
            if (LegoGameInput.GameplayAllowed)
            {
                if (IsPlayerOnFoot)
                {
                    if (IsInsideRaceZone && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return)))
                    {
                        LegoRaceLobby.Instance.OpenPanel();
                    }
                }
                else
                {
                    // During Test Drive: block exiting car [J] and restarting race [R]; player uses [TAB] to return to Garage
                    if (!inTestDrive)
                    {
                        if (Input.GetKeyDown(KeyCode.R))
                            RestartRace();
                        if (Input.GetKeyDown(KeyCode.J))
                            EnterOpenWorldOnFootMode(false);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleDestructionMode();
            }
        }

        public void ToggleDestructionMode()
        {
            LegoVehicleVisualAndDamage.DestructionModeEnabled = !LegoVehicleVisualAndDamage.DestructionModeEnabled;
            if (!LegoVehicleVisualAndDamage.DestructionModeEnabled)
            {
                foreach (var r in AllRacers)
                {
                    if (r != null && r.Visuals != null)
                        r.Visuals.FullRepairInstant();
                }
            }
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.6f);
        }

        private void OnGUI()
        {
            if (PlayerKart == null) return;
            GUI.enabled = true;
            if (LegoGameInput.Instance != null && LegoGameInput.Instance.IsPauseMenuOpen)
            {
                LegoGameInput.Instance.DrawPauseMenu();
                return;
            }
            if (LegoRaceLobby.Instance != null && LegoRaceLobby.Instance.IsPanelOpen)
            {
                LegoRaceLobby.Instance.DrawPanel();
                return;
            }
            if (LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen)
            {
                LegoMultiplayerRoomManager.Instance.DrawMultiplayerGUI();
                return;
            }

            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
            {
                LegoGarageBuilder.Instance.DrawGarageOverlayGUI();
                if (LegoGameInput.IsGarageAimingActive)
                {
                    DrawCenterCrosshair();
                }
                return;
            }

            GUI.enabled = Cursor.lockState != CursorLockMode.Locked;
            if (IsPlayerOnFoot)
            {
                DrawOpenWorldOnFootHUD();
            }
            else
            {
                DrawArcadeHUD();
            }
            GUI.enabled = true;

            if (!LegoGameInput.HasModal)
            {
                DrawCenterCrosshair();
            }

            if (LegoGarageBuilder.Instance != null)
            {
                LegoGarageBuilder.Instance.DrawGarageOverlayGUI();
            }

            if (LegoMultiplayerRoomManager.Instance != null)
            {
                LegoMultiplayerRoomManager.Instance.DrawMultiplayerGUI();
            }
        }

        private void DrawOpenWorldOnFootHUD()
        {
            DrawSolidBox(new Rect(16f, 194f, 345f, 142f), new Color(0.08f, 0.11f, 0.16f, 0.88f));
            DrawSolidBox(new Rect(16f, 194f, 345f, 3f), LegoPalette.CyanGlow);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = LegoPalette.BrightYellow;

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;

            string camMode = (PlayerCharacter != null && PlayerCharacter.IsFirstPerson) ? "Goc Nhin Thu 1 (FPP)" : "Goc Nhin Thu 3 (TPP)";
            GUI.Label(new Rect(28f, 200f, 325f, 24f), "MOTORVERSE VALLEY", headerStyle);
            GUI.Label(new Rect(28f, 226f, 325f, 20f), $"Camera: {camMode} ([V] de doi)", textStyle);

            float distGarage = GetDistanceToGarageZone();
            float distToRace = GetDistanceToRaceZone();
            textStyle.normal.textColor = LegoPalette.BrightOrange;
            GUI.Label(new Rect(28f, 248f, 325f, 20f), $"Cong Garage [G]: {distGarage:F0}m   |   Khu Dua [R]: {distToRace:F0}m", textStyle);

            textStyle.normal.textColor = new Color(0.84f, 0.92f, 1.0f);
            GUI.Label(new Rect(28f, 272f, 325f, 20f), "[WASD] Di bo | [Shift] Chay | [Space] Nhay | [Giu M] Ban do", textStyle);
            GUI.Label(new Rect(28f, 292f, 325f, 20f), "[TAB / E] Mo Garage (tai cong) | [P] Phong Choi", textStyle);
            GUI.Label(new Rect(28f, 312f, 325f, 20f), "[ESC] Menu / Hien Chuot", textStyle);

            // Top-Right Action Buttons (Top-Left is dedicated to the default small minimap)
            GUIStyle topBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(Screen.width - 216f, 14f, 200f, 28f), "[V] DOI GOC NHIN 1 / 3", topBtnStyle))
            {
                if (PlayerCharacter != null)
                    PlayerCharacter.ToggleCameraPerspective();
            }
            if (GUI.Button(new Rect(Screen.width - 216f, 46f, 200f, 28f), "[ESC] MENU / THA CHUOT", topBtnStyle))
            {
                LegoGameInput.Instance.OpenPauseMenu();
            }

            bool prevEnabled = GUI.enabled;
            GUI.enabled = prevEnabled && CanOpenGarageNow;
            if (GUI.Button(new Rect(Screen.width - 216f, 78f, 200f, 28f), CanOpenGarageNow ? "[TAB] MO GARAGE" : "DEN CONG GARAGE DE DO XE", topBtnStyle))
            {
                if (LegoGameInput.Instance != null)
                    LegoGameInput.Instance.ToggleGarage();
            }
            GUI.enabled = prevEnabled;

            Color prevMpBg = GUI.backgroundColor;
            bool mpConn = LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsConnected;
            GUI.backgroundColor = mpConn ? LegoPalette.BrightGreen : LegoPalette.CyanGlow;
            string mpLabel = mpConn
                ? $"[P] PHONG: {LegoMultiplayerRoomManager.Instance.RoomCode} ({LegoMultiplayerRoomManager.Instance.RemotePlayerCount + 1})"
                : "[P] MULTIPLAYER (Choi Chung)";
            if (GUI.Button(new Rect(Screen.width - 216f, 110f, 200f, 28f), mpLabel, topBtnStyle))
            {
                if (LegoMultiplayerRoomManager.Instance != null)
                    LegoMultiplayerRoomManager.Instance.ToggleRoomPanel();
            }
            GUI.backgroundColor = prevMpBg;

            // Bottom-center quick shortcut pill (Image 3 style)
            float pillW = 300f;
            Rect pill = new Rect((Screen.width - pillW) * 0.5f, Screen.height - 44f, pillW, 30f);
            DrawSolidBox(pill, new Color(0.08f, 0.11f, 0.16f, 0.86f));
            GUIStyle pillStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            pillStyle.normal.textColor = Color.white;
            GUI.Label(pill, "[TAB] GARAGE (tai cong)   |   [Giu M] BAN DO", pillStyle);

            if (IsInsideRaceZone)
            {
                float boxW = 540f;
                Rect promptRect = new Rect((Screen.width - boxW) * 0.5f, Screen.height - 120f, boxW, 60f);
                DrawSolidBox(promptRect, new Color(0.06f, 0.12f, 0.18f, 0.92f));
                DrawSolidBox(new Rect(promptRect.x, promptRect.y, boxW, 4f), LegoPalette.CyanGlow);

                GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                promptStyle.normal.textColor = LegoPalette.BrightYellow;
                GUI.Label(promptRect, $"SLOT DUA XE: {LegoRaceLobby.Instance.Slots.Length}/4\nNhan [E] de XAC NHAN / XEM SLOT!", promptStyle);
            }
            else if (IsInsideGarageZone)
            {
                float boxW = 540f;
                Rect promptRect = new Rect((Screen.width - boxW) * 0.5f, Screen.height - 120f, boxW, 58f);
                DrawSolidBox(promptRect, new Color(0.08f, 0.12f, 0.18f, 0.92f));
                DrawSolidBox(new Rect(promptRect.x, promptRect.y, boxW, 4f), LegoPalette.BrightOrange);

                GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                promptStyle.normal.textColor = LegoPalette.BrightYellow;
                GUI.Label(promptRect, "CONG GARAGE MOTORVERSE\nNhan [TAB] hoac [E] de Vao Phong Thiet Ke & Chay Thu!", promptStyle);
            }

            DrawCenterBannerIfActive();

            if (LegoMinimapRenderer.Instance != null && PlayerCharacter != null)
            {
                LegoMinimapRenderer.Instance.DrawMinimapGUI(
                    PlayerCharacter.transform.position,
                    PlayerCharacter.CurrentLookYaw,
                    GarageEntryPosition,
                    RaceEntryPosition);
            }
        }

        private void DrawArcadeHUD()
        {
            bool inTestDrive = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsInTestDrive;
            int rank = GetRacerPosition(PlayerKart);

            // --- 1. LEFT STATUS PANEL (below Top-Left minimap): STANDINGS / TEST DRIVE STATUS ---
            float tlWidth = inTestDrive ? 380f : 320f;
            float tlLabelW = inTestDrive ? 355f : 295f;
            DrawSolidBox(new Rect(16f, 194f, tlWidth, inTestDrive ? 150f : 185f), new Color(0.08f, 0.10f, 0.14f, 0.86f));
            DrawSolidBox(new Rect(16f, 194f, tlWidth, 3f), inTestDrive ? LegoPalette.BrightGreen : LegoPalette.BrightYellow);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = inTestDrive ? 18 : 20,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = inTestDrive ? LegoPalette.BrightGreen : LegoPalette.BrightYellow;

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;

            if (inTestDrive)
            {
                GUI.Label(new Rect(28f, 202f, tlLabelW, 28f), "BAI TAP LAI THU (TEST DRIVE)", headerStyle);
                textStyle.normal.textColor = new Color(0.84f, 0.94f, 1.0f);
                GUI.Label(new Rect(28f, 236f, tlLabelW, 20f), "[WASD] Lai & Re  |  [Shift] Drift  |  [E] Boost", textStyle);
                GUI.Label(new Rect(28f, 258f, tlLabelW, 20f), "Di Chuot: Xoay Camera 360 kiem tra xe", textStyle);
                GUI.Label(new Rect(28f, 280f, tlLabelW, 20f), "Khong the xuong xe / Khong ra khoi bai tap", textStyle);
                textStyle.normal.textColor = LegoPalette.BrightYellow;
                GUI.Label(new Rect(28f, 302f, tlLabelW, 20f), "[TAB] Thoat Lai Thu -> Ve Phong Garage", textStyle);
            }
            else
            {
                int dispLap = Mathf.Clamp(PlayerKart.CurrentLap, 1, totalLaps);
                int racerCount = LegoRaceLobby.Instance != null ? Mathf.Max(1, LegoRaceLobby.Instance.Participants.Length) : 1;
                GUI.Label(new Rect(28f, 202f, 295f, 28f), $"VI TRI: {FormatRank(rank)} / {racerCount}   VONG: {dispLap}/{totalLaps}", headerStyle);
                GUI.Label(new Rect(28f, 232f, 295f, 20f), $"THOI GIAN: {_raceElapsedTime:F1}s", textStyle);

                textStyle.normal.textColor = new Color(0.82f, 0.90f, 1.0f);
                GUI.Label(new Rect(28f, 258f, 295f, 20f), "[WASD] Lai xe  |  [Shift] Drift  |  [Space] Nhay", textStyle);
                GUI.Label(new Rect(28f, 278f, 295f, 20f), "[E / Ctrl] Nitro Boost  |  [F / Enter] Vu khi", textStyle);
                GUI.Label(new Rect(28f, 298f, 295f, 20f), "[J] Xuong xe di bo  |  [Giu M] Ban do", textStyle);
                GUI.Label(new Rect(28f, 318f, 295f, 20f), "[ESC] Menu  |  [R] Ve khu dang ky dua", textStyle);
            }

            // --- 2. TOP CENTER BADGE ---
            WheelSizeTier wTier = (PlayerKart.Visuals != null && PlayerKart.Visuals.Loadout != null)
                ? PlayerKart.Visuals.Loadout.wheelSizeTier
                : WheelSizeTier.Medium;
            Color badgeColor = inTestDrive ? LegoPalette.BrightGreen :
                               (PlayerKart.CurrentTerrain == TerrainType.OffRoad ? LegoPalette.BrightOrange : LegoPalette.CyanGlow);
            string formTitle = inTestDrive
                ? $"CHAY THU TRONG BAI TAP | BANH XE: {wTier}"
                : (PlayerKart.CurrentTerrain == TerrainType.OffRoad ? $"DUONG DAT (OFF-ROAD) | BANH: {wTier}" : $"DUONG NHUA (ASPHALT) | BANH: {wTier}");

            float centerW = 360f;
            DrawSolidBox(new Rect((Screen.width - centerW) * 0.5f, 14f, centerW, 38f), new Color(0.08f, 0.10f, 0.14f, 0.88f));
            DrawSolidBox(new Rect((Screen.width - centerW) * 0.5f, 14f, centerW, 4f), badgeColor);

            GUIStyle centerBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            centerBadgeStyle.normal.textColor = badgeColor;
            GUI.Label(new Rect((Screen.width - centerW) * 0.5f, 18f, centerW, 30f), formTitle, centerBadgeStyle);

            // --- 3. TOP RIGHT: CONTEXTUAL BUTTONS ---
            GUIStyle topBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            Color oldBtnBg = GUI.backgroundColor;
            if (inTestDrive)
            {
                GUI.backgroundColor = LegoPalette.BrightOrange;
                if (GUI.Button(new Rect(Screen.width - 252f, 14f, 236f, 32f), "[TAB] THOAT LAI THU -> GARAGE", topBtnStyle))
                {
                    if (LegoGameInput.Instance != null)
                        LegoGameInput.Instance.ToggleGarage();
                }
                GUI.backgroundColor = oldBtnBg;
            }
            else
            {
                if (GUI.Button(new Rect(Screen.width - 216f, 14f, 200f, 28f), "[J] XUONG XE DI BO", topBtnStyle))
                    EnterOpenWorldOnFootMode(false);
                if (GUI.Button(new Rect(Screen.width - 216f, 46f, 200f, 26f), "[R] VE KHU DANG KY DUA", topBtnStyle))
                    RestartRace();

                bool destOn = LegoVehicleVisualAndDamage.DestructionModeEnabled;
                GUI.backgroundColor = destOn ? LegoPalette.BrightOrange : LegoPalette.BrightGreen;
                string destLabel = destOn ? "[K] TRU MAU XE: BAT" : "[K] TRU MAU XE: TAT";
                if (GUI.Button(new Rect(Screen.width - 216f, 76f, 200f, 26f), destLabel, topBtnStyle))
                    ToggleDestructionMode();

                bool mpConnRace = LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsConnected;
                GUI.backgroundColor = mpConnRace ? LegoPalette.BrightGreen : LegoPalette.CyanGlow;
                string mpRaceLabel = mpConnRace
                    ? $"[P] PHONG: {LegoMultiplayerRoomManager.Instance.RoomCode} ({LegoMultiplayerRoomManager.Instance.RemotePlayerCount + 1})"
                    : "[P] MULTIPLAYER ROOM";
                if (GUI.Button(new Rect(Screen.width - 216f, 106f, 200f, 28f), mpRaceLabel, topBtnStyle))
                {
                    if (LegoMultiplayerRoomManager.Instance != null)
                        LegoMultiplayerRoomManager.Instance.ToggleRoomPanel();
                }
                GUI.backgroundColor = oldBtnBg;
            }

            // --- 4. BOTTOM LEFT: VEHICLE HP BAR & POWER-UP SLOT ---
            if (!inTestDrive)
            {
                float blY = Screen.height - 125f;
                DrawSolidBox(new Rect(18f, blY, 310f, 105f), new Color(0.08f, 0.10f, 0.14f, 0.85f));

                float healthPct = PlayerKart.Visuals.HealthNormalized;
                Color hpColor = !LegoVehicleVisualAndDamage.DestructionModeEnabled ? LegoPalette.CyanGlow :
                                (PlayerKart.Visuals.IsStunned ? LegoPalette.BrightRed :
                                (healthPct > 0.45f ? LegoPalette.BrightGreen : LegoPalette.BrightOrange));

                string hpStatus = !LegoVehicleVisualAndDamage.DestructionModeEnabled
                    ? "CHE DO SAT THUONG: TAT (Xe Bat Tu)"
                    : (PlayerKart.Visuals.IsStunned
                        ? $"BI CHOANG! Hoi phuc sau: {PlayerKart.Visuals.StunTimeRemaining:F1}s"
                        : $"MAU XE (HP): {PlayerKart.Visuals.CurrentHP:F0} / {PlayerKart.Visuals.MaxHP:F0}");

                textStyle.normal.textColor = PlayerKart.Visuals.IsStunned ? LegoPalette.BrightYellow : Color.white;
                GUI.Label(new Rect(30f, blY + 10f, 285f, 22f), hpStatus, textStyle);
                DrawProgressBar(
                    new Rect(30f, blY + 34f, 285f, 18f),
                    PlayerKart.Visuals.IsStunned ? (1f - PlayerKart.Visuals.StunTimeRemaining / LegoVehicleVisualAndDamage.STUN_DURATION) : healthPct,
                    hpColor);

                string itemText = FormatPowerUpName(PlayerKart.HeldPowerUp);
                Color itemCol = PlayerKart.HeldPowerUp == PowerUpType.None ? LegoPalette.LightGrey : LegoPalette.CyanGlow;
                textStyle.normal.textColor = itemCol;
                GUI.Label(new Rect(30f, blY + 64f, 285f, 26f), $"VU KHI [F]: {itemText}", textStyle);
            }

            // --- 5. BOTTOM RIGHT: SPEEDOMETER & BOOST NITRO BAR ---
            float brX = Screen.width - 280f;
            float brY = Screen.height - 115f;
            DrawSolidBox(new Rect(brX, brY, 262f, 98f), new Color(0.08f, 0.10f, 0.14f, 0.88f));
            DrawSolidBox(new Rect(brX, brY, 262f, 3f), LegoPalette.CyanGlow);

            GUIStyle speedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
            speedStyle.normal.textColor = PlayerKart.IsBoosting ? LegoPalette.BrightOrange : LegoPalette.BrightYellow;
            GUI.Label(new Rect(brX + 16f, brY + 10f, 232f, 34f), $"{PlayerKart.SpeedKmh:F0} KM/H", speedStyle);

            textStyle.normal.textColor = Color.white;
            string boostLabel = PlayerKart.IsDrifting ? "NITRO (DRIFT CHARGING!)" : "NITRO [E]";
            GUI.Label(new Rect(brX + 16f, brY + 46f, 232f, 20f), boostLabel, textStyle);
            DrawProgressBar(new Rect(brX + 16f, brY + 68f, 232f, 16f), PlayerKart.BoostMeter / 100f, LegoPalette.BrightOrange);

            if (PlayerKart.Visuals.IsStunned)
            {
                GUIStyle stunBannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 30,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                string stunMsg = $"XE HET MAU! BI CHOANG TRONG {PlayerKart.Visuals.StunTimeRemaining:F1}s...";
                stunBannerStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(2f, Screen.height * 0.28f + 2f, Screen.width, 50f), stunMsg, stunBannerStyle);
                stunBannerStyle.normal.textColor = LegoPalette.BrightRed;
                GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 50f), stunMsg, stunBannerStyle);
            }

            DrawCenterBannerIfActive();

            if (LegoMinimapRenderer.Instance != null)
            {
                LegoMinimapRenderer.Instance.DrawMinimapGUI(
                    PlayerKart.transform.position,
                    PlayerKart.transform.eulerAngles.y,
                    GarageEntryPosition,
                    RaceEntryPosition);
            }
        }

        private void DrawCenterCrosshair()
        {
            if (_whiteTex == null) return;
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float halfSpan = 7f;
            float thickness = 2f;

            // Crisp Minecraft-style dark-outlined white/cyan '+' crosshair centered on screen
            Color outlineCol = new Color(0.04f, 0.06f, 0.09f, 0.90f);
            Color armCol = new Color(0.96f, 0.99f, 1.0f, 0.96f);

            DrawSolidBox(new Rect(cx - halfSpan - 1f, cy - 1.5f, halfSpan * 2f + 2f, thickness + 2f), outlineCol);
            DrawSolidBox(new Rect(cx - 1.5f, cy - halfSpan - 1f, thickness + 2f, halfSpan * 2f + 2f), outlineCol);

            DrawSolidBox(new Rect(cx - halfSpan, cy - 0.5f, halfSpan * 2f, thickness), armCol);
            DrawSolidBox(new Rect(cx - 0.5f, cy - halfSpan, thickness, halfSpan * 2f), armCol);
            DrawSolidBox(new Rect(cx - 1f, cy - 1f, 3f, 3f), LegoPalette.CyanGlow);
        }

        private void DrawCenterBannerIfActive()
        {
            if (_centerBannerTimer > 0f && !string.IsNullOrEmpty(_centerBannerText))
            {
                GUIStyle bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 36,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                Rect shadowRect = new Rect(2f, Screen.height * 0.20f + 2f, Screen.width, 70f);
                bannerStyle.normal.textColor = Color.black;
                GUI.Label(shadowRect, _centerBannerText, bannerStyle);

                Rect mainRect = new Rect(0f, Screen.height * 0.20f, Screen.width, 70f);
                bannerStyle.normal.textColor = LegoPalette.BrightYellow;
                GUI.Label(mainRect, _centerBannerText, bannerStyle);
            }
        }

        private void DrawSolidBox(Rect rect, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = prev;
        }

        private void DrawProgressBar(Rect rect, float normalizedValue, Color fillColor)
        {
            DrawSolidBox(rect, new Color(0.18f, 0.20f, 0.24f, 1f));
            float fillW = Mathf.Clamp01(normalizedValue) * (rect.width - 4f);
            if (fillW > 0f)
            {
                DrawSolidBox(new Rect(rect.x + 2f, rect.y + 2f, fillW, rect.height - 4f), fillColor);
            }
        }

        private static string FormatRank(int rank)
        {
            switch (rank)
            {
                case 1: return "1st";
                case 2: return "2nd";
                case 3: return "3rd";
                default: return $"{rank}th";
            }
        }

        private static string FormatPowerUpName(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.HomingBrickMissile: return "HOMING BRICK MISSILE (Ready!)";
                case PowerUpType.EnergyBrickShield: return "ENERGY BRICK SHIELD (Ready!)";
                case PowerUpType.SpiderWebMine: return "HAZARD BRICK MINE (Ready!)";
                case PowerUpType.HyperWarpBoost: return "HYPER WARP BOOST (Ready!)";
                default: return "Empty (Hit Cyan Mystery Brick)";
            }
        }
    }
}
