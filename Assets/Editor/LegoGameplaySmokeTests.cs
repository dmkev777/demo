#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Lego2KDrive.EditorTools
{
    [InitializeOnLoad]
    public static class LegoGameplaySmokeTests
    {
        private const string Pending = "LegoGameplaySmokeTests.Pending";
        private const string StartTicksKey = "LegoGameplaySmokeTests.StartTicks";
        private const string LoadoutSaveKey = "Lego2KDrive_PlayerCustomLoadout_v6";
        private static int _smokePhase;
        private static int _targetFrame;
        private static bool _hadSavedLoadout;
        private static string _savedLoadoutJson = "";
        static LegoGameplaySmokeTests() { EditorApplication.update += Tick; }

        [MenuItem("LEGO 2K Drive/Validate ESC, Camera and Race Slots", false, 30)]
        public static void Run()
        {
            _smokePhase = 0;
            _targetFrame = 0;
            SessionState.SetBool(Pending, true);
            SessionState.SetString(StartTicksKey, DateTime.UtcNow.Ticks.ToString());
            EditorApplication.isPlaying = true;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Gameplay smoke check failed: " + message);
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || EditorApplication.isCompiling) return;
            if (long.TryParse(SessionState.GetString(StartTicksKey, "0"), out long ticks) && ticks > 0)
            {
                if ((DateTime.UtcNow - new DateTime(ticks)).TotalSeconds > 15.0)
                {
                    _smokePhase = 0;
                    SessionState.SetBool(Pending, false);
                    Debug.LogError("Gameplay smoke test timed out after 15s waiting for PlayMode initialization.");
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    else EditorApplication.isPlaying = false;
                    return;
                }
            }
            if (!EditorApplication.isPlaying) return;
            var manager = LegoRaceManagerAndHUD.Instance;
            if (manager == null || manager.PlayerCharacter == null) return;
            if (_smokePhase > 0 && Time.frameCount < _targetFrame) return;

            int exitCode = 0;
            bool finished = false;
            try
            {
                var input = LegoGameInput.Instance;
                var lobby = LegoRaceLobby.Instance;

                if (_smokePhase == 0)
                {
                    _hadSavedLoadout = PlayerPrefs.HasKey(LoadoutSaveKey);
                    _savedLoadoutJson = _hadSavedLoadout ? PlayerPrefs.GetString(LoadoutSaveKey, "") : "";
                    if (manager.PlayerKart != null && manager.PlayerKart.Visuals != null && manager.PlayerKart.Visuals.Loadout != null)
                    {
                        manager.PlayerKart.Visuals.Loadout.ResetAllToSmallBareBase();
                        manager.PlayerKart.Visuals.RebuildAllForms();
                    }

                    Check(manager.AllRacers.Count == 1, "NPC racers removed");
                    Check(UnityEngine.Object.FindObjectsByType<LegoAIRacer>(FindObjectsSortMode.None).Length == 0, "no AI controllers");
                    Check(UnityEngine.Object.FindObjectsByType<LegoRaceSlotMarker>(FindObjectsSortMode.None).Length == 4, "four visible grid slots");
                    Check(manager.IsPlayerOnFoot && !manager.PlayerKart.gameObject.activeSelf, "empty grid on spawn");
                    manager.StartConfirmedRace(0);
                    Check(manager.IsPlayerOnFoot, "unconfirmed direct start rejected");
                    lobby.Confirm();
                    Check(lobby.MySlot < 0, "cannot reserve from village");

                    input.SendMessage("Update");
                    Check(!LegoGameInput.HasModal && !Cursor.visible, "initial open-world gameplay hides OS cursor cleanly");

                    input.OpenPauseMenu();
                    Check(input.IsPauseMenuOpen && Time.timeScale == 0 && Cursor.visible && Cursor.lockState == CursorLockMode.None, "ESC menu pauses and releases cursor");
                    Check(!LegoGameInput.GameplayAllowed, "menu blocks gameplay input");
                    input.ClosePanels();
                    input.SendMessage("Update");
                    Check(!LegoGameInput.HasModal && Time.timeScale == 1 && !Cursor.visible, "closing menu restores simulation and hides OS cursor");

                    Check(!manager.IsInsideGarageZone, "village spawn starts outside garage entry zone");
                    input.ToggleGarage();
                    Check(!LegoGarageBuilder.Instance.IsGarageOpen, "cannot open garage outside entry zone");

                    manager.PlayerCharacter.transform.position = manager.GarageEntryPosition + Vector3.up * 0.5f;
                    manager.SendMessage("Update");
                    Check(manager.IsInsideGarageZone && manager.CanOpenGarageNow && !LegoGarageBuilder.Instance.IsGarageOpen,
                        "garage entry zone unlocks workshop without auto-opening it");
                    input.ToggleGarage();
                    Check(LegoGarageBuilder.Instance.IsGarageOpen && manager.PlayerKart.gameObject.activeSelf, "garage shows personal car at workshop");
                    Check(LegoGarageBuilder.Instance.IsHoldingBrick && LegoGameInput.IsGarageAimingActive && !Cursor.visible,
                        "garage hides OS cursor for center-arrow aiming while holding a default brick");

                    _smokePhase = 1;
                    _targetFrame = Time.frameCount + 2;
                    return;
                }

                if (_smokePhase == 1)
                {
                    Check(manager.IsPlayerOnFoot && manager.PlayerCharacter.gameObject.activeSelf &&
                          Time.timeScale == 1f && !LegoGarageBuilder.Instance.IsCarInspectView && LegoGameInput.CharacterControlAllowed,
                        "garage keeps character active and movable on foot around car by default");
                    LegoGarageBuilder.Instance.ToggleCarInspectView();
                    Check(LegoGarageBuilder.Instance.IsCarInspectView && !LegoGameInput.CharacterControlAllowed,
                        "toggling car inspect view switches to orbit camera and locks character movement");
                    LegoGarageBuilder.Instance.ToggleCarInspectView();

                    _smokePhase = 2;
                    _targetFrame = Time.frameCount + 2;
                    return;
                }

                Check(!LegoGarageBuilder.Instance.IsCarInspectView && LegoGameInput.CharacterControlAllowed,
                    "toggling car inspect view off returns to walk-and-build mode");

                Vector3 carAimTarget = manager.PlayerKart.transform.position + Vector3.up * 0.42f;
                Vector3 rayOrigin = carAimTarget + new Vector3(1.8f, 1.4f, -2.2f);
                Ray aimRay = new Ray(rayOrigin, (carAimTarget - rayOrigin).normalized);
                bool hitBlockFace = LegoGarageBuilder.Instance.UpdateCenterRayFaceSnapping(aimRay);
                Check(hitBlockFace && LegoGarageBuilder.Instance.AimedBrickIndex >= 0,
                    "center aim ray detects existing brick face for block snapping");
                Check(LegoGarageBuilder.Instance.PlaceBrickAtCursor(), "placing brick adjacent to aimed block face succeeds cleanly");
                LegoGarageBuilder.Instance.RemoveLastCustomBrick();

                if (LegoMinimapRenderer.Instance != null)
                {
                    LegoMinimapRenderer.Instance.SetOverviewMode(false);
                    Check(!LegoMinimapRenderer.Instance.IsZoomedOutOverview && LegoMinimapRenderer.Instance.CurrentOrthoSize < 120f,
                        "minimap stays compact zoomed-in by default");
                    LegoMinimapRenderer.Instance.SetOverviewMode(true);
                    Check(LegoMinimapRenderer.Instance.IsZoomedOutOverview && LegoMinimapRenderer.Instance.CurrentOrthoSize > 150f,
                        "holding [M] overview mode switches minimap to zoomed-out full map");
                    LegoMinimapRenderer.Instance.SetOverviewMode(false);
                }

                var carBricks = manager.PlayerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street);
                var floatingBrick = new LegoBrickSpec(new Vector3Int(0, 9, 0), new Vector3Int(2, 2, 2), Color.red, BrickShape.StandardBrick);
                var attachedBrick = new LegoBrickSpec(new Vector3Int(0, 4, 2), new Vector3Int(2, 2, 2), Color.red, BrickShape.StandardBrick);
                Check(!LegoGarageBuilder.IsBrickPlacementConnected(floatingBrick, carBricks), "floating brick rejected by connectivity check");
                Check(LegoGarageBuilder.IsBrickPlacementConnected(attachedBrick, carBricks), "attached brick accepted by connectivity check");

                LegoGarageBuilder.Instance.SetWheelSizeTier(WheelSizeTier.Large);
                Check(LegoGarageBuilder.Instance.CurrentWheelSizeTier == WheelSizeTier.Large &&
                      manager.PlayerKart.Visuals.Loadout.wheelSizeTier == WheelSizeTier.Large, "wheel size selector updates loadout");
                Check(manager.PlayerKart.Visuals.Loadout.CalculateVehicleStats().offRoadMultiplier > 1.1f, "large wheels improve off-road multiplier");

                LegoGarageBuilder.Instance.StartTestDrive();
                Check(!LegoGarageBuilder.Instance.IsGarageOpen && LegoGarageBuilder.Instance.IsInTestDrive, "test drive closes garage UI and enables test mode");
                Check(manager.PlayerKart.gameObject.activeSelf && manager.TestArenaBounds.Contains(manager.PlayerKart.transform.position), "test drive spawns player car inside fenced arena");
                input.ToggleGarage();
                Check(LegoGarageBuilder.Instance.IsGarageOpen && !LegoGarageBuilder.Instance.IsInTestDrive, "exiting test drive returns to garage");
                input.ClosePanels();
                Check(manager.IsPlayerOnFoot && !manager.PlayerKart.gameObject.activeSelf && Time.timeScale == 1, "closing garage returns on foot and hides unregistered car");

                input.ToggleRoom();
                Check(LegoMultiplayerRoomManager.Instance.IsRoomPanelOpen && !LegoGameInput.GameplayAllowed, "room text fields block character control");
                input.ClosePanels();

                manager.PlayerCharacter.transform.position = manager.RaceEntryPosition + Vector3.up;
                Check(manager.IsInsideRaceZone, "ring geometry accepts center");
                manager.PlayerCharacter.transform.position += Vector3.right * (LegoRaceLobby.EntryRadius + 0.1f);
                Check(!manager.IsInsideRaceZone, "ring rejects exterior (not a 12.5m proximity zone)");
                manager.PlayerCharacter.transform.position = manager.RaceEntryPosition + Vector3.up;
                lobby.OpenPanel();
                Check(lobby.IsPanelOpen && Cursor.visible, "entering lobby releases cursor");
                lobby.Confirm();
                Check(lobby.MySlot == 0 && manager.IsPlayerOnFoot, "confirm reserves without forcing immediate race");
                lobby.Leave();
                Check(lobby.MySlot < 0, "cancel frees slot");
                // Simulate a delayed server start arriving after local cancellation.
                var staleRoster = new[] { new RaceSlot { playerId = "LOCAL", playerName = "BAN", slotIndex = 0 } };
                lobby.ReceiveLobby(staleRoster, false);
                lobby.ReceiveStart(staleRoster, 1);
                Check(manager.IsPlayerOnFoot && lobby.MySlot < 0, "late start cannot override revoked consent");
                lobby.Confirm();
                lobby.StartRace();
                Check(!manager.IsPlayerOnFoot && manager.PlayerKart.gameObject.activeSelf, "confirmed player enters own car");
                Check(!manager.CanRacersDrive && !lobby.IsPanelOpen, "countdown precedes driving and closes lobby");
                Check(Vector3.Distance(manager.PlayerKart.transform.position, manager.GarageInteriorCarPos) > 40f,
                    "confirmed race start teleports car out of Garage workshop");
                Check(Vector3.Distance(manager.PlayerKart.transform.position, manager.PlayerKart.Rb.position) < 0.25f,
                    "confirmed race start syncs Rigidbody position to transform position");
                Check(Vector3.Distance(manager.PlayerKart.transform.position, manager.RaceEntryPosition) < 25f,
                    "confirmed race start places car on Starting Grid near Race Beacon");
                manager.EnterOpenWorldOnFootMode(false);
                Check(manager.IsPlayerOnFoot && !manager.IsInsideRaceZone && lobby.MySlot < 0, "leaving race resets slot outside the circle");

                manager.PlayerCharacter.SetFirstPerson(true);
                manager.PlayerCharacter.CurrentLookYaw = 135f;
                manager.PlayerCharacter.CurrentLookPitch = -40f;
                Camera.main.GetComponent<LegoArcadeCamera>().SendMessage("LateUpdate");
                Check(Quaternion.Angle(Camera.main.transform.rotation, Quaternion.Euler(-40, 135, 0)) < 0.01f, "first-person camera follows exact crosshair rotation");
                manager.PlayerCharacter.SetFirstPerson(false);
                Camera.main.GetComponent<LegoArcadeCamera>().SendMessage("LateUpdate");
                Check(Quaternion.Angle(Camera.main.transform.rotation, Quaternion.Euler(-40, 135, 0)) < 0.01f, "third-person camera follows exact crosshair rotation");
                Mesh pineMesh = LegoMeshAndMaterialFactory.GetLowPolyPineTreeMesh();
                Check(pineMesh != null && pineMesh.vertexCount > 0 && pineMesh.triangles.Length > 0, "low-poly pine tree mesh is readable and populated");
                Mesh rockMesh = LegoMeshAndMaterialFactory.GetLowPolyRockMesh();
                Check(rockMesh != null && rockMesh.vertexCount > 0 && rockMesh.triangles.Length > 0, "low-poly rock mesh is readable and populated");
                Mesh mountainMesh = LegoMeshAndMaterialFactory.GetLowPolyMountainMesh();
                Check(mountainMesh != null && mountainMesh.vertexCount > 0 && mountainMesh.triangles.Length > 0, "low-poly mountain mesh is readable and populated");
                Mesh spoilerMesh = LegoMeshAndMaterialFactory.GetBrickMesh(4, 2, 2, BrickShape.SpoilerWing);
                Check(spoilerMesh != null && spoilerMesh.vertexCount > 0 && spoilerMesh.triangles.Length > 0, "spoiler wing mesh is readable and populated");
                Mesh v8Mesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 2, 2, BrickShape.V8Engine);
                Check(v8Mesh != null && v8Mesh.vertexCount > 0 && v8Mesh.triangles.Length > 0, "V8 engine mesh is readable and populated");

                Vector2 arenaCenterXZ = new Vector2(manager.TestArenaBounds.center.x, manager.TestArenaBounds.center.z);
                Vector2 garageCarXZ = new Vector2(manager.GarageInteriorCarPos.x, manager.GarageInteriorCarPos.z);
                var trackWaypoints = manager.TrackWaypoints;
                Check(trackWaypoints != null && trackWaypoints.Count >= 16, "track waypoints are populated for overlap validation");

                int leafClusterCount = 0;
                int grassTuftCount = 0;
                int wildflowerCount = 0;
                int walkwayCount = 0;
                int windmillCount = 0;
                int churchCount = 0;
                int shopCount = 0;

                float MinTrackSegDist(Vector2 pXZ)
                {
                    float minSegDistSq = float.MaxValue;
                    int wpCount = trackWaypoints.Count;
                    for (int wi = 0; wi < wpCount; wi++)
                    {
                        Vector2 a = new Vector2(trackWaypoints[wi].x, trackWaypoints[wi].z);
                        Vector2 b = new Vector2(trackWaypoints[(wi + 1) % wpCount].x, trackWaypoints[(wi + 1) % wpCount].z);
                        Vector2 ab = b - a;
                        float lenSq = ab.sqrMagnitude;
                        float t = lenSq > 0.001f ? Mathf.Clamp01(Vector2.Dot(pXZ - a, ab) / lenSq) : 0f;
                        Vector2 closest = a + ab * t;
                        float dSq = (pXZ - closest).sqrMagnitude;
                        if (dSq < minSegDistSq) minSegDistSq = dSq;
                    }
                    return Mathf.Sqrt(minSegDistSq);
                }

                foreach (Transform tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if (tr == null) continue;
                    string n = tr.name;
                    if (n.StartsWith("LeafCluster_")) leafClusterCount++;
                    else if (n.StartsWith("LowPolyGrassTuft")) grassTuftCount++;
                    else if (n.StartsWith("LowPolyWildflowers")) wildflowerCount++;
                    else if (n.StartsWith("PedestrianWalkway_") || n.StartsWith("ParkStonePaver_")) walkwayCount++;
                    else if (n == "LowPolyWindmill") windmillCount++;
                    else if (n == "LowPolyVillageChurch") churchCount++;
                    else if (n == "LowPolyCommercialShop") shopCount++;

                    if (n.StartsWith("MeadowHill_") || n.StartsWith("Foothill_") || n.StartsWith("AlpinePeak_"))
                    {
                        Vector2 pXZ = new Vector2(tr.position.x, tr.position.z);
                        Check(Vector2.Distance(pXZ, arenaCenterXZ) >= 75f, $"hill/mountain '{n}' does not overlap Test Drive Arena");
                        Check(Vector2.Distance(pXZ, garageCarXZ) >= 75f, $"hill/mountain '{n}' does not overlap Garage Workshop");

                        float minSegDist = MinTrackSegDist(pXZ);
                        float footprintRadius = Mathf.Max(tr.localScale.x, tr.localScale.z) * 1.15f;
                        float requiredClearance = 9.0f + footprintRadius + 10.0f;
                        Check(minSegDist >= requiredClearance,
                            $"hill/mountain '{n}' clearance from track ({minSegDist:F1}m >= {requiredClearance:F1}m)");
                    }
                    else if (n == "LowPolyWindmill" || n == "LowPolyVillageChurch" || n == "LowPolyCommercialShop")
                    {
                        float minSegDist = MinTrackSegDist(new Vector2(tr.position.x, tr.position.z));
                        Check(minSegDist >= 24.0f, $"building '{n}' clear of track ({minSegDist:F1}m >= 24.0m)");
                    }
                }

                Check(leafClusterCount >= 50, $"trees spawn abundant swaying leaf clusters (count={leafClusterCount})");
                Check(grassTuftCount >= 280, $"open world & walkways spawn dense swaying grass tufts (count={grassTuftCount})");
                Check(wildflowerCount >= 50, $"open world & walkways spawn abundant wildflower clusters (count={wildflowerCount})");
                Check(walkwayCount >= 6, $"curbed pedestrian walkways and pavers exist (count={walkwayCount})");
                Check(windmillCount >= 5, $"world spawns animated windmills across infield & outskirts (count={windmillCount})");
                Check(churchCount >= 2, $"world spawns architectural churches (count={churchCount})");
                Check(shopCount >= 6, $"world spawns commercial shops across infield & outskirts (count={shopCount})");
                var sceneryAnim = UnityEngine.Object.FindFirstObjectByType<LegoWorldSceneryAnimator>();
                Check(sceneryAnim != null && sceneryAnim.windmillRotors.Count >= 5, "scenery animator tracks >= 5 windmill rotors");

                Check(pineMesh != null && pineMesh.vertexCount >= 180, $"detailed tiered pine mesh has rich geometry (verts={pineMesh?.vertexCount})");
                Check(mountainMesh != null && mountainMesh.vertexCount >= 300, $"craggy multi-peak mountain mesh has rich geometry (verts={mountainMesh?.vertexCount})");
                Mesh leafMesh = LegoMeshAndMaterialFactory.GetDetailedLeafClusterMesh();
                Check(leafMesh != null && leafMesh.vertexCount > 0 && leafMesh.triangles.Length > 0, "detailed leaf cluster mesh is populated");
                Mesh grassMesh = LegoMeshAndMaterialFactory.GetSwayingGrassTuftMesh();
                Check(grassMesh != null && grassMesh.vertexCount > 0 && grassMesh.triangles.Length > 0, "swaying grass tuft mesh is populated");
                Mesh flowerMesh = LegoMeshAndMaterialFactory.GetLowPolyFlowerPatchMesh(LegoPalette.BrightYellow);
                Check(flowerMesh != null && flowerMesh.vertexCount > 0 && flowerMesh.triangles.Length > 0, "vertex-colored flower patch mesh is populated");

                GameObject meadowObj = GameObject.Find("LowPolySculptedMeadow");
                Check(meadowObj != null, "LowPolySculptedMeadow terrain object exists in scene");
                MeshFilter meadowMf = meadowObj.GetComponent<MeshFilter>();
                Mesh meadowMesh = meadowMf != null ? meadowMf.sharedMesh : null;
                Check(meadowMesh != null && meadowMesh.vertexCount > 5000 && meadowMesh.colors.Length == meadowMesh.vertexCount,
                    "LowPolySculptedMeadow mesh has >5000 vertices and full per-vertex colors");
                Check(LegoMeshAndMaterialFactory.GetPatternTexture(SurfacePattern.MeadowTerrain) != null,
                    "SurfacePattern.MeadowTerrain procedural RGB texture loads cleanly");
                Check(LegoMeshAndMaterialFactory.GetPatternTexture(SurfacePattern.AlpineRock) != null,
                    "SurfacePattern.AlpineRock procedural RGB texture loads cleanly");
                Check(LegoMeshAndMaterialFactory.GetPatternTexture(SurfacePattern.TreeBark) != null,
                    "SurfacePattern.TreeBark procedural RGB texture loads cleanly");

                Check(LegoVehicleLoadout.CreateMinimalStreetBase(LegoPalette.BrightOrange, LegoPalette.SilverMetallic).Count >= 18,
                    "default v6 sports car silhouette has >= 18 bricks");
                Check(manager.PlayerKart.Visuals.Loadout.GetBricksForForm(TerrainType.Street).Count >= 18,
                    "active player car uses updated >= 18-brick loadout");

                float largeWheelRadius = LegoMeshAndMaterialFactory.GetWheelRadius(3, 3) * LegoMeshAndMaterialFactory.GetWheelScaleForTier(WheelSizeTier.Large);
                Check(largeWheelRadius < 0.55f, $"large wheel radius ({largeWheelRadius:F2}m) is proportional (< 0.55m)");

                Shader lowPolyShader = Resources.Load<Shader>("Shaders/MotorverseLowPolyLit");
                Shader postProcessShader = Resources.Load<Shader>("Shaders/MotorversePostProcess");
                Check(lowPolyShader != null, "MotorverseLowPolyLit shader loads cleanly from Resources");
                Check(postProcessShader != null, "MotorversePostProcess shader loads cleanly from Resources");
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    Check(lowPolyShader.isSupported && postProcessShader.isSupported, "stylized low-poly & post-process shaders compile cleanly on active GPU");
                }

                Debug.Log("GAMEPLAY_SMOKE_TESTS_PASSED: ESC/menu transitions, camera aim, no NPCs, circle bounds, slot consent/cancel/start/exit.");
                finished = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
                finished = true;
            }
            if (!finished) return;

            _smokePhase = 0;
            SessionState.SetBool(Pending, false);
            if (_hadSavedLoadout)
                PlayerPrefs.SetString(LoadoutSaveKey, _savedLoadoutJson);
            else
                PlayerPrefs.DeleteKey(LoadoutSaveKey);
            PlayerPrefs.Save();

            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            else EditorApplication.isPlaying = false;
        }
    }
}
#endif
