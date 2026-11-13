using System;
using UnityEngine;

namespace Lego2KDrive
{
    [Serializable]
    public class RaceSlot
    {
        public string playerId;
        public string playerName;
        public int slotIndex;
    }

    [DefaultExecutionOrder(-1000)]
    public class LegoRaceLobby : MonoBehaviour
    {
        public const int Capacity = 4;
        public const float EntryRadius = 2.75f;
        public static LegoRaceLobby Instance { get; private set; }
        public bool IsPanelOpen { get; private set; }
        public bool IsRaceActive { get; private set; }
        public int ActiveRaceId { get; private set; }
        public RaceSlot[] Slots { get; private set; } = new RaceSlot[0];
        public RaceSlot[] Participants { get; private set; } = new RaceSlot[0];
        public string Status { get; private set; } = "Vao vong tron va xac nhan de giu mot slot.";
        private bool _wasInside;
        private bool _wasConnected;
        private bool _requestPending;
        private bool _consentGiven;
        private float _requestTime;
        private string MyId => Online ? LegoMultiplayerRoomManager.Instance.MyPlayerId : "LOCAL";
        private bool Online => LegoMultiplayerRoomManager.Instance != null && LegoMultiplayerRoomManager.Instance.IsConnected;
        public int MySlot => Array.FindIndex(Slots, slot => slot.playerId == MyId);
        public bool IsLocalParticipant => Array.Exists(Participants, slot => slot.playerId == MyId);

        private void Awake() { Instance = this; }

        private void Update()
        {
            if (_wasConnected != Online)
            {
                if (!LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot)
                    LegoRaceManagerAndHUD.Instance.EnterOpenWorldOnFootMode(false);
                ResetLobby();
                _wasConnected = Online;
            }
            bool inside = LegoRaceManagerAndHUD.Instance != null && LegoRaceManagerAndHUD.Instance.IsInsideRaceZone;
            if (!inside && _wasInside)
            {
                if (_consentGiven && !IsRaceActive) Leave();
                ClosePanel();
                if (LegoGameInput.Instance != null) LegoGameInput.Instance.RefreshState();
            }
            if (inside && !_wasInside && !LegoGameInput.HasModal) OpenPanel();
            _wasInside = inside;
            if (_requestPending && Time.unscaledTime - _requestTime > 6f)
            {
                _requestPending = false;
                Status = "Server chua phan hoi. Kiem tra server da cap nhat roi thu lai.";
            }
            if (IsPanelOpen && Input.GetKeyDown(KeyCode.Return))
            {
                if (MySlot < 0) Confirm();
                else StartRace();
            }
        }

        public void OpenPanel()
        {
            if (!LegoRaceManagerAndHUD.Instance.IsInsideRaceZone) return;
            LegoGameInput.Instance.ClosePanels();
            IsPanelOpen = true;
            LegoGameInput.Instance.RefreshState();
        }

        public void ClosePanel() { IsPanelOpen = false; }

        public void Confirm()
        {
            if (_requestPending || IsRaceActive || MySlot >= 0 || !LegoRaceManagerAndHUD.Instance.IsInsideRaceZone) return;
            _consentGiven = true;
            if (Online) SendRequest("race_join");
            else
            {
                Slots = new[] { new RaceSlot { playerId = "LOCAL", playerName = "BAN", slotIndex = 0 } };
                Status = "Da xac nhan. Bam BAT DAU DUA khi san sang.";
            }
        }

        public void StartRace()
        {
            if (_requestPending || IsRaceActive || MySlot < 0 || !LegoRaceManagerAndHUD.Instance.IsInsideRaceZone) return;
            if (Online) SendRequest("race_start_request");
            else ReceiveStart(Slots, ActiveRaceId + 1);
        }

        private void SendRequest(string type)
        {
            _requestPending = true;
            _requestTime = Time.unscaledTime;
            Status = "Dang cho server xac nhan...";
            LegoMultiplayerRoomManager.Instance.SendRaceRequest(type);
        }

        public void Leave()
        {
            // Revoke consent immediately, even when a server acknowledgement is in flight.
            _consentGiven = false;
            if (Online) LegoMultiplayerRoomManager.Instance.SendRaceRequest("race_leave");
            else ResetLobby();
            _requestPending = false;
        }

        public void Finish()
        {
            if (Online) LegoMultiplayerRoomManager.Instance.SendRaceRequest("race_finish");
            else IsRaceActive = false;
        }

        public void ReceiveLobby(RaceSlot[] slots, bool active)
        {
            Slots = slots ?? new RaceSlot[0];
            IsRaceActive = active;
            _requestPending = false;
            Status = active ? "Cuoc dua dang dien ra. Cho luot tiep theo." :
                MySlot >= 0 ? "Da xac nhan. Doi ban be roi bam BAT DAU DUA." : "Xac nhan de giu slot. Roi vong tron se huy slot.";
        }

        public void ReceiveError(string message)
        {
            _requestPending = false;
            Status = message;
        }

        public void ReceiveStart(RaceSlot[] slots, int raceId)
        {
            if (slots == null || raceId <= ActiveRaceId) return;
            // Consent must already have been acknowledged in the lobby.
            bool confirmed = _consentGiven && MySlot >= 0 && LegoRaceManagerAndHUD.Instance.IsInsideRaceZone;
            Participants = slots;
            ActiveRaceId = raceId;
            IsRaceActive = true;
            _requestPending = false;
            if (!IsLocalParticipant) return;
            if (!confirmed) { Leave(); return; }
            RaceSlot mine = Array.Find(slots, slot => slot.playerId == MyId);
            if (mine.slotIndex < 0 || mine.slotIndex >= Capacity) return;
            LegoGameInput.Instance.ClosePanels();
            LegoRaceManagerAndHUD.Instance.StartConfirmedRace(mine.slotIndex);
        }

        public bool OwnsGridSlot(int slotIndex)
        {
            return _consentGiven && IsRaceActive && Array.Exists(Participants, slot => slot.playerId == MyId && slot.slotIndex == slotIndex);
        }

        private void ResetLobby()
        {
            Slots = new RaceSlot[0];
            Participants = new RaceSlot[0];
            IsRaceActive = false;
            ActiveRaceId = 0;
            _requestPending = false;
            _consentGiven = false;
            Status = "Vao vong tron va xac nhan de giu mot slot.";
        }

        public void DrawPanel()
        {
            float width = Mathf.Min(480f, Screen.width - 24f);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2f, Mathf.Max(12f, (Screen.height - 400f) / 2f), width, 400f), GUI.skin.box);
            GUILayout.Label($"DANG KY DUA XE ({Slots.Length}/{Capacity} SLOT)", new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter });
            for (int i = 0; i < Capacity; i++)
            {
                RaceSlot occupant = Array.Find(Slots, slot => slot.slotIndex == i);
                GUILayout.Label($"SLOT {i + 1}: " + (occupant == null ? "TRONG" : occupant.playerName + " - DA XAC NHAN"), GUILayout.Height(30));
            }
            GUILayout.Label(Status, new GUIStyle(GUI.skin.label) { wordWrap = true }, GUILayout.Height(46));
            GUI.enabled = !_requestPending && !IsRaceActive && MySlot < 0 && Slots.Length < Capacity;
            if (GUILayout.Button("XAC NHAN THAM GIA [ENTER]", GUILayout.Height(36))) Confirm();
            GUI.enabled = !_requestPending && !IsRaceActive && MySlot >= 0;
            if (GUILayout.Button("BAT DAU DUA - CHI NGUOI DA XAC NHAN", GUILayout.Height(36))) StartRace();
            if (GUILayout.Button("HUY SLOT", GUILayout.Height(30))) Leave();
            GUI.enabled = true;
            if (GUILayout.Button("DONG [ESC]", GUILayout.Height(30)))
            {
                ClosePanel();
                LegoGameInput.Instance.RefreshState();
            }
            GUILayout.EndArea();
        }
    }

    public class LegoRaceSlotMarker : MonoBehaviour
    {
        public int SlotIndex;
        public TextMesh Label;

        private void LateUpdate()
        {
            if (Label == null || LegoRaceLobby.Instance == null) return;
            RaceSlot occupant = Array.Find(LegoRaceLobby.Instance.Slots, slot => slot.slotIndex == SlotIndex);
            Label.text = $"SLOT {SlotIndex + 1}\n" + (occupant == null ? "TRONG" : occupant.playerName);
            Label.color = occupant == null ? LegoPalette.CyanGlow : LegoPalette.BrightGreen;
            if (Camera.main != null) Label.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
