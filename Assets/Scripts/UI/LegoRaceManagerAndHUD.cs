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

            // Spawn Player + 3 AI Racers parked at the Starting Grid
            AllRacers.Clear();
            PlayerKart = SpawnRacer("YOU (Clutch)", true, LegoPalette.BrightRed, LegoPalette.BrightYellow, 0, 0f, 1.0f);
            SpawnRacer("Shadow Z", false, LegoPalette.PurpleRival, LegoPalette.CyanGlow, 1, -4.0f, 0.96f);
            SpawnRacer("Brickbeard", false, LegoPalette.BrightGreen, LegoPalette.BrightYellow, 2, 4.0f, 0.94f);
            SpawnRacer("Sky Lane", false, LegoPalette.BrightBlue, LegoPalette.BrightOrange, 3, -1.8f, 0.92f);

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

            // Setup Minecraft World Block Builder & 9-Slot Hotbar Inventory
            if (GetComponent<MinecraftWorldBuilderAndInventory>() == null)
                gameObject.AddComponent<MinecraftWorldBuilderAndInventory>();
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
            existingLight.transform.rotation = Quaternion.Euler(46f, -38f, 0f);
            existingLight.color = new Color(1.0f, 0.97f, 0.92f);
            existingLight.intensity = 0.95f;
            existingLight.shadows = LightShadows.Soft;
            existingLight.shadowStrength = 0.80f;

            GameObject fillObj = new GameObject("Sky Fill Light");
            fillObj.transform.SetParent(transform, false);
            fillObj.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
            Light fillLight = fillObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.55f, 0.68f, 0.85f);
            fillLight.intensity = 0.25f;
            fillLight.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.38f, 0.44f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.30f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.20f, 0.22f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.68f, 0.84f, 0.98f);
            RenderSettings.fogDensity = 0.0012f;

            QualitySettings.shadowDistance = 150f;
            QualitySettings.antiAliasing = 4;
        }

        private ArcadeKartController SpawnRacer(
            string name,
            bool isPlayer,
            Color primary,
            Color accent,
            int gridIndex,
            float aiLaneOffset,
            float aiSkill)
        {
            GameObject kartObj = new GameObject($"LegoRacer_{name}");
            kartObj.transform.position = _trackData.startGridPositions[gridIndex];
            kartObj.transform.rotation = _trackData.startGridRotation;

            kartObj.SetActive(false);
            kartObj.AddComponent<Rigidbody>();
            kartObj.AddComponent<BoxCollider>();
            kartObj.AddComponent<LegoVehicleVisualAndDamage>();
            ArcadeKartController controller = kartObj.AddComponent<ArcadeKartController>();

            controller.racerName = name;
            controller.isPlayer = isPlayer;
            controller.primaryColor = primary;
            controller.accentColor = accent;
            kartObj.SetActive(true);

            controller.SetTrackWaypoints(_trackData.waypoints, 1);

            if (!isPlayer)
            {
                LegoAIRacer ai = kartObj.AddComponent<LegoAIRacer>();
                ai.InitializeAI(_trackData.waypoints, aiLaneOffset, aiSkill);
            }

            AllRacers.Add(controller);
            return controller;
        }

        /// <summary>
        /// Switches to On-Foot Open-World Mode (Character walking around the Minecraft voxel world).
        /// </summary>
        public void EnterOpenWorldOnFootMode(bool spawnAtVillage)
        {
            StopAllCoroutines();
            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
                LegoGarageBuilder.Instance.ToggleGarage();

            IsPlayerOnFoot = true;
            CanRacersDrive = false;

            // Park all vehicles at the Starting Grid so the player can see them waiting at the Race Hub!
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
                    // Place character right beside the Race Entry Beacon
                    PlayerCharacter.transform.position = _trackData.raceEntryBeaconPos + Vector3.up * 1.2f;
                }
            }

            _centerBannerText = "THE GIOI MO: DI BO TOI VONG SANG XANH DE DUA XE!";
            _centerBannerTimer = 3.5f;
        }

        /// <summary>
        /// Hops into the Player's custom LEGO vehicle and starts the Race Countdown!
        /// </summary>
        public void EnterVehicleAndStartRace()
        {
            StopAllCoroutines();
            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
                LegoGarageBuilder.Instance.ToggleGarage();

            IsPlayerOnFoot = false;
            if (PlayerCharacter != null)
            {
                PlayerCharacter.gameObject.SetActive(false);
            }

            for (int i = 0; i < AllRacers.Count; i++)
            {
                AllRacers[i].ResetForRaceStart(_trackData.startGridPositions[i], _trackData.startGridRotation);
            }

            _raceFinished = false;
            _raceElapsedTime = 0f;
            StartCoroutine(RaceCountdownRoutine());
        }

        public void RestartRace()
        {
            EnterVehicleAndStartRace();
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
                    _raceFinished = true;
                    _playerFinishRank = GetRacerPosition(PlayerKart);
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
            float dBeacon = Vector3.Distance(PlayerCharacter.transform.position, _trackData.raceEntryBeaconPos);
            float dCar = PlayerKart != null ? Vector3.Distance(PlayerCharacter.transform.position, PlayerKart.transform.position) : 999f;
            return Mathf.Min(dBeacon, dCar);
        }

        private void Update()
        {
            if (!IsPlayerOnFoot && CanRacersDrive && !_raceFinished)
            {
                _raceElapsedTime += Time.deltaTime;
            }

            if (_centerBannerTimer > 0f)
            {
                _centerBannerTimer -= Time.deltaTime;
            }

            bool garageClosed = (LegoGarageBuilder.Instance == null || !LegoGarageBuilder.Instance.IsGarageOpen);

            if (garageClosed)
            {
                if (IsPlayerOnFoot)
                {
                    // When near the Race Beacon or Parked Car, press [E] or [F] to hop in and Start Race!
                    if (GetDistanceToRaceZone() < 12.5f && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return)))
                    {
                        EnterVehicleAndStartRace();
                    }
                    // Or press [G] from anywhere to teleport straight to the Race!
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        EnterVehicleAndStartRace();
                    }
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        RestartRace();
                    }
                    // Press [J] while driving to exit vehicle and return to Open-World On-Foot mode!
                    if (Input.GetKeyDown(KeyCode.J))
                    {
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

            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
            {
                LegoGarageBuilder.Instance.DrawGarageOverlayGUI();
                return;
            }

            if (IsPlayerOnFoot)
            {
                DrawOpenWorldOnFootHUD();
            }
            else
            {
                DrawArcadeHUD();
            }
        }

        private void DrawOpenWorldOnFootHUD()
        {
            DrawSolidBox(new Rect(18f, 16f, 355f, 195f), new Color(0.08f, 0.10f, 0.14f, 0.86f));

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = LegoPalette.BrightGreen;

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;

            string camMode = (PlayerCharacter != null && PlayerCharacter.IsFirstPerson) ? "Goc Nhin Thu 1 (FPP)" : "Goc Nhin Thu 3 (TPP)";
            GUI.Label(new Rect(30f, 22f, 335f, 24f), "THANH PHO THE GIOI MO (MINECRAFT CITY)", headerStyle);
            GUI.Label(new Rect(30f, 46f, 335f, 20f), $"Camera: {camMode} (Bam [V] de doi)", textStyle);

            float distToRace = GetDistanceToRaceZone();
            textStyle.normal.textColor = LegoPalette.CyanGlow;
            GUI.Label(new Rect(30f, 68f, 335f, 20f), $"Khoang cach toi Khu Dua Xe: {distToRace:F0}m", textStyle);

            textStyle.normal.textColor = new Color(0.84f, 0.92f, 1.0f);
            GUI.Label(new Rect(30f, 92f, 335f, 20f), "[WASD] Di bo | [Shift] Chay nhanh | [Space] Nhay", textStyle);
            GUI.Label(new Rect(30f, 112f, 335f, 20f), "[1-9 / Lan Chuot] Chon Block | [0] Tay Khong", textStyle);
            GUI.Label(new Rect(30f, 132f, 335f, 20f), "[Click Trai] Dat Block Xay Nha | [Click Phai] Dap Block", textStyle);
            GUI.Label(new Rect(30f, 152f, 335f, 20f), "[B hoac I] Mo Tui Do 16 Block | [Giu Alt] Hien Chuot", textStyle);
            GUI.Label(new Rect(30f, 172f, 335f, 20f), "[E / F] Len Xe Dua | [TAB] Garage Do Xe", textStyle);

            // Top-Right Action Buttons
            GUIStyle topBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(Screen.width - 250f, 16f, 232f, 30f), "[V] DOI GOC NHIN THU 1 / THU 3", topBtnStyle))
            {
                if (PlayerCharacter != null)
                    PlayerCharacter.ToggleCameraPerspective();
            }
            if (GUI.Button(new Rect(Screen.width - 250f, 50f, 232f, 30f), "[B] MO TUI DO BLOCK XAY NHA", topBtnStyle))
            {
                if (MinecraftWorldBuilderAndInventory.Instance != null)
                    MinecraftWorldBuilderAndInventory.Instance.ToggleInventory();
            }
            if (GUI.Button(new Rect(Screen.width - 250f, 84f, 232f, 30f), "[G] LEN XE & BAT DAU DUA NGAY!", topBtnStyle))
            {
                EnterVehicleAndStartRace();
            }
            if (GUI.Button(new Rect(Screen.width - 250f, 118f, 232f, 28f), "[TAB] GARAGE DO XE", topBtnStyle))
            {
                if (LegoGarageBuilder.Instance != null)
                    LegoGarageBuilder.Instance.ToggleGarage();
            }

            // Draw Minecraft 9-Slot Bottom Hotbar, Crosshair & Full Inventory Modal
            if (MinecraftWorldBuilderAndInventory.Instance != null)
            {
                MinecraftWorldBuilderAndInventory.Instance.DrawMinecraftHotbarAndInventoryGUI();
            }

            // Proximity Prompt when standing near the Race Beacon or Parked Car!
            if (distToRace < 12.5f)
            {
                float boxW = 540f;
                Rect promptRect = new Rect((Screen.width - boxW) * 0.5f, Screen.height - 165f, boxW, 60f);
                DrawSolidBox(promptRect, new Color(0.06f, 0.12f, 0.18f, 0.92f));
                DrawSolidBox(new Rect(promptRect.x, promptRect.y, boxW, 4f), LegoPalette.CyanGlow);

                GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                promptStyle.normal.textColor = LegoPalette.BrightYellow;
                GUI.Label(promptRect, "BAN DA DEN KHU VUC DUA XE!\nNhan [E] hoac [F] de LEN XE & BAT DAU DUA!", promptStyle);
            }

            DrawCenterBannerIfActive();
        }

        private void DrawArcadeHUD()
        {
            int rank = GetRacerPosition(PlayerKart);

            // --- 1. TOP LEFT: RACE STANDINGS, LAP & CONTROLS ---
            DrawSolidBox(new Rect(18f, 16f, 315f, 195f), new Color(0.08f, 0.10f, 0.14f, 0.84f));

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            headerStyle.normal.textColor = LegoPalette.BrightYellow;

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;

            int dispLap = Mathf.Clamp(PlayerKart.CurrentLap, 1, totalLaps);
            GUI.Label(new Rect(30f, 22f, 290f, 30f), $"POS: {FormatRank(rank)} / {AllRacers.Count}    LAP: {dispLap}/{totalLaps}", headerStyle);
            GUI.Label(new Rect(30f, 52f, 290f, 22f), $"TIME: {_raceElapsedTime:F1}s", textStyle);

            textStyle.normal.textColor = new Color(0.82f, 0.90f, 1.0f);
            GUI.Label(new Rect(30f, 80f, 295f, 20f), "[WASD / Arrows] Drive & Steer", textStyle);
            GUI.Label(new Rect(30f, 100f, 295f, 20f), "[Shift] Drift (Charges Boost) | [Space] Jump", textStyle);
            GUI.Label(new Rect(30f, 120f, 295f, 20f), "[E / Ctrl] Boost  |  [F / Enter] Fire Weapon", textStyle);
            GUI.Label(new Rect(30f, 140f, 295f, 20f), "[J] Xuong Xe Di Bo  |  [K] Bat/Tat Pha Huy", textStyle);
            GUI.Label(new Rect(30f, 160f, 295f, 20f), "[TAB] Garage Do Xe  |  [R] Restart Race", textStyle);

            // --- 2. TOP CENTER: CURRENT SHAPESHIFTING VEHICLE FORM BADGE ---
            TerrainType activeForm = PlayerKart.Visuals.CurrentForm;
            Color badgeColor = activeForm == TerrainType.Street ? LegoPalette.BrightRed :
                               (activeForm == TerrainType.OffRoad ? LegoPalette.BrightOrange : LegoPalette.CyanGlow);
            string formTitle = activeForm == TerrainType.Street ? "FORM: STREET SUPERCAR (ASPHALT)" :
                               (activeForm == TerrainType.OffRoad ? "FORM: OFF-ROAD MONSTER BUGGY (DIRT)" : "FORM: HYDRO SPEEDBOAT (WATER)");

            float centerW = 370f;
            DrawSolidBox(new Rect((Screen.width - centerW) * 0.5f, 16f, centerW, 40f), new Color(0.08f, 0.10f, 0.14f, 0.88f));
            DrawSolidBox(new Rect((Screen.width - centerW) * 0.5f, 16f, centerW, 5f), badgeColor);

            GUIStyle centerBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            centerBadgeStyle.normal.textColor = badgeColor;
            GUI.Label(new Rect((Screen.width - centerW) * 0.5f, 20f, centerW, 32f), formTitle, centerBadgeStyle);

            // --- 3. TOP RIGHT: ACTION BUTTONS ---
            GUIStyle topBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(Screen.width - 235f, 16f, 215f, 30f), "[TAB] GARAGE BUILDER", topBtnStyle))
            {
                if (LegoGarageBuilder.Instance != null)
                    LegoGarageBuilder.Instance.ToggleGarage();
            }
            if (GUI.Button(new Rect(Screen.width - 235f, 50f, 215f, 28f), "[J] XUONG XE DI BO (Open World)", topBtnStyle))
            {
                EnterOpenWorldOnFootMode(false);
            }
            if (GUI.Button(new Rect(Screen.width - 235f, 82f, 215f, 26f), "[R] RESTART RACE", topBtnStyle))
            {
                RestartRace();
            }

            Color oldBtnBg = GUI.backgroundColor;
            bool destOn = LegoVehicleVisualAndDamage.DestructionModeEnabled;
            GUI.backgroundColor = destOn ? LegoPalette.BrightOrange : LegoPalette.BrightGreen;
            string destLabel = destOn ? "[K] TRU MAU XE: BAT" : "[K] TRU MAU XE: TAT (Bat Tu)";
            if (GUI.Button(new Rect(Screen.width - 235f, 112f, 215f, 26f), destLabel, topBtnStyle))
            {
                ToggleDestructionMode();
            }
            GUI.backgroundColor = oldBtnBg;

            // --- 4. BOTTOM LEFT: VEHICLE HP BAR & POWER-UP SLOT ---
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
            GUI.Label(new Rect(30f, blY + 64f, 285f, 26f), $"WEAPON [F]: {itemText}", textStyle);

            // --- 5. BOTTOM RIGHT: SPEEDOMETER & BOOST NITRO BAR ---
            float brX = Screen.width - 290f;
            float brY = Screen.height - 125f;
            DrawSolidBox(new Rect(brX, brY, 272f, 105f), new Color(0.08f, 0.10f, 0.14f, 0.85f));

            GUIStyle speedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
            speedStyle.normal.textColor = PlayerKart.IsBoosting ? LegoPalette.BrightOrange : LegoPalette.BrightYellow;
            GUI.Label(new Rect(brX + 16f, brY + 10f, 240f, 34f), $"{PlayerKart.SpeedKmh:F0} KM/H", speedStyle);

            textStyle.normal.textColor = Color.white;
            string boostLabel = PlayerKart.IsDrifting ? "BOOST (DRIFT CHARGING!)" : "BOOST NITRO [E]";
            GUI.Label(new Rect(brX + 16f, brY + 48f, 240f, 20f), boostLabel, textStyle);
            DrawProgressBar(new Rect(brX + 16f, brY + 72f, 240f, 18f), PlayerKart.BoostMeter / 100f, LegoPalette.BrightOrange);

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
