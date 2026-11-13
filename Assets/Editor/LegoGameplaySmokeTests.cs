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
        static LegoGameplaySmokeTests() { EditorApplication.update += Tick; }

        [MenuItem("LEGO 2K Drive/Validate ESC, Camera and Race Slots", false, 30)]
        public static void Run()
        {
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Gameplay smoke check failed: " + message);
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var manager = LegoRaceManagerAndHUD.Instance;
            if (manager == null || manager.PlayerCharacter == null) return;
            SessionState.SetBool(Pending, false);
            int exitCode = 0;
            try
            {
                var input = LegoGameInput.Instance;
                var lobby = LegoRaceLobby.Instance;
                Check(manager.AllRacers.Count == 1, "NPC racers removed");
                Check(UnityEngine.Object.FindObjectsByType<LegoAIRacer>(FindObjectsSortMode.None).Length == 0, "no AI controllers");
                Check(UnityEngine.Object.FindObjectsByType<LegoRaceSlotMarker>(FindObjectsSortMode.None).Length == 4, "four visible grid slots");
                Check(manager.IsPlayerOnFoot && !manager.PlayerKart.gameObject.activeSelf, "empty grid on spawn");
                manager.StartConfirmedRace(0);
                Check(manager.IsPlayerOnFoot, "unconfirmed direct start rejected");
                lobby.Confirm();
                Check(lobby.MySlot < 0, "cannot reserve from village");

                input.OpenPauseMenu();
                Check(input.IsPauseMenuOpen && Time.timeScale == 0 && Cursor.visible && Cursor.lockState == CursorLockMode.None, "ESC menu pauses and releases cursor");
                Check(!LegoGameInput.GameplayAllowed, "menu blocks gameplay input");
                input.ToggleInventory();
                Check(!input.IsPauseMenuOpen && MinecraftWorldBuilderAndInventory.Instance.IsInventoryOpen, "inventory replaces pause menu");
                input.ClosePanels();
                Check(!LegoGameInput.HasModal && Time.timeScale == 1, "closing menu restores simulation");
                input.ToggleGarage();
                Check(LegoGarageBuilder.Instance.IsGarageOpen && manager.PlayerKart.gameObject.activeSelf, "garage shows personal car");
                input.ClosePanels();
                Check(!manager.PlayerKart.gameObject.activeSelf && Time.timeScale == 1, "closing garage hides unregistered car");
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
                Debug.Log("GAMEPLAY_SMOKE_TESTS_PASSED: ESC/menu transitions, camera aim, no NPCs, circle bounds, slot consent/cancel/start/exit.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 1;
            }
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            else EditorApplication.isPlaying = false;
        }
    }
}
#endif
