using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Lego2KDrive
{
    [Serializable]
    public class MultiplayerPacket
    {
        public string type;          // "join", "state", "loadout", "start_race", "voxel_place", "voxel_break", "leave"
        public string room;          // Room code (e.g. "PHONG1")
        public string playerId;      // Unique ID for each connected client
        public string playerName;    // Display name overhead
        public bool isOnFoot;        // True = walking character, False = driving custom vehicle
        public int vehicleForm;      // 0 = Street, 1 = OffRoad, 2 = Water
        public Vector3 pos;          // Character pos (if isOnFoot) or Vehicle pos (if !isOnFoot)
        public float yaw;            // Character/Vehicle yaw angle
        public float speed;          // Movement or driving speed
        public float steer;          // Steering input (-1..1)
        public bool isBoosting;
        public bool hasShield;
        public bool isStunned;
        public float hp;
        public int blockIndex;       // For voxel_place
        public string loadoutJson;   // Serialized custom LEGO car blueprint JSON
    }

    /// <summary>
    /// Represents a friend (Remote Player) inside the same Multiplayer Room.
    /// Holds both their 3D Blocky Character AND their Custom-Built LEGO Vehicle!
    /// </summary>
    public class RemotePlayerAvatar : MonoBehaviour
    {
        public string PlayerId { get; private set; }
        public string PlayerName { get; set; } = "Friend";
        public bool IsOnFoot { get; private set; } = true;
        public float CurrentHP { get; private set; } = 100f;
        public bool IsStunned { get; private set; }
        public float LastSeenTime { get; set; }

        public GameObject CharacterRoot { get; private set; }
        public GameObject VehicleRoot { get; private set; }
        public LegoVehicleVisualAndDamage VehicleVisuals { get; private set; }

        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private float _walkAnimTime;

        private Vector3 _targetPos;
        private float _targetYaw;
        private float _targetSpeed;
        private float _targetSteer;
        private bool _isBoosting;
        private bool _hasShield;
        private TerrainType _targetForm = TerrainType.Street;
        private Vector3 _parkedCarPos;
        private Quaternion _parkedCarRot;
        private string _lastAppliedLoadoutJson = "";

        public void Initialize(string playerId, string playerName, Color shirtColor, Vector3 parkedPos, Quaternion parkedRot)
        {
            PlayerId = playerId;
            PlayerName = string.IsNullOrEmpty(playerName) ? "Ban Be" : playerName;
            LastSeenTime = Time.unscaledTime;
            _parkedCarPos = parkedPos;
            _parkedCarRot = parkedRot;

            BuildRemoteCharacterVisual(shirtColor);
            BuildRemoteVehicleVisual(shirtColor);

            _targetPos = parkedPos + Vector3.right * 3f;
            CharacterRoot.transform.position = _targetPos;
            VehicleRoot.transform.SetPositionAndRotation(_parkedCarPos, _parkedCarRot);
        }

        private void BuildRemoteCharacterVisual(Color shirtColor)
        {
            CharacterRoot = new GameObject($"RemoteCharacter_{PlayerId}");
            CharacterRoot.transform.SetParent(transform, false);

            GameObject torso = CreatePart("Torso", CharacterRoot.transform, new Vector3(0f, 0.72f, 0f),
                new Vector3Int(2, 3, 1), shirtColor);
            torso.transform.localScale = new Vector3(1.1f, 1.0f, 1.1f);

            GameObject head = CreatePart("Head", CharacterRoot.transform, new Vector3(0f, 1.56f, 0f),
                new Vector3Int(2, 3, 2), new Color(0.96f, 0.78f, 0.58f));
            CreatePart("Hair", head.transform, new Vector3(0f, 0.75f, 0f),
                new Vector3Int(2, 1, 2), new Color(0.20f, 0.14f, 0.08f));

            _leftArm = CreateLimb("LArm", CharacterRoot.transform, new Vector3(-0.62f, 1.50f, 0f),
                new Vector3(0f, -0.75f, 0f), new Vector3Int(1, 3, 1), new Color(0.96f, 0.78f, 0.58f));
            _rightArm = CreateLimb("RArm", CharacterRoot.transform, new Vector3(0.62f, 1.50f, 0f),
                new Vector3(0f, -0.75f, 0f), new Vector3Int(1, 3, 1), new Color(0.96f, 0.78f, 0.58f));
            _leftLeg = CreateLimb("LLeg", CharacterRoot.transform, new Vector3(-0.22f, 0.72f, 0f),
                new Vector3(0f, -0.72f, 0f), new Vector3Int(1, 3, 1), LegoPalette.BrightBlue);
            _rightLeg = CreateLimb("RLeg", CharacterRoot.transform, new Vector3(0.22f, 0.72f, 0f),
                new Vector3(0f, -0.72f, 0f), new Vector3Int(1, 3, 1), LegoPalette.BrightBlue);
        }

        private void BuildRemoteVehicleVisual(Color primaryColor)
        {
            VehicleRoot = new GameObject($"RemoteVehicle_{PlayerId}");
            VehicleRoot.transform.SetParent(transform, false);
            VehicleVisuals = VehicleRoot.AddComponent<LegoVehicleVisualAndDamage>();
            VehicleVisuals.Initialize(primaryColor, LegoPalette.BrightYellow, false);
        }

        private static Transform CreateLimb(string name, Transform parent, Vector3 pivotPos, Vector3 offset, Vector3Int size, Color col)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPos;
            CreatePart(name + "_Mesh", pivot.transform, offset, size, col);
            return pivot.transform;
        }

        private static GameObject CreatePart(string name, Transform parent, Vector3 localPos, Vector3Int size, Color col)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(size.x, size.y, size.z, BrickShape.FlatTile);
            obj.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(col, false, 0.45f);
            return obj;
        }

        public void ApplyCustomVehicleLoadoutJson(string json)
        {
            if (string.IsNullOrEmpty(json) || json == _lastAppliedLoadoutJson || VehicleVisuals == null)
                return;

            if (VehicleVisuals.Loadout.ImportFromJson(json))
            {
                _lastAppliedLoadoutJson = json;
                VehicleVisuals.RebuildAllForms();
                VehicleVisuals.SwitchToForm(_targetForm, true);
            }
        }

        public void OnReceiveNetworkState(MultiplayerPacket pkt)
        {
            LastSeenTime = Time.unscaledTime;
            if (!string.IsNullOrEmpty(pkt.playerName))
                PlayerName = pkt.playerName;

            IsOnFoot = pkt.isOnFoot;
            _targetPos = pkt.pos;
            _targetYaw = pkt.yaw;
            _targetSpeed = pkt.speed;
            _targetSteer = pkt.steer;
            _isBoosting = pkt.isBoosting;
            _hasShield = pkt.hasShield;
            IsStunned = pkt.isStunned;
            CurrentHP = pkt.hp > 0f ? pkt.hp : 100f;
            _targetForm = (TerrainType)Mathf.Clamp(pkt.vehicleForm, 0, 2);

            if (!string.IsNullOrEmpty(pkt.loadoutJson))
            {
                ApplyCustomVehicleLoadoutJson(pkt.loadoutJson);
            }
        }

        public Vector3 GetHeadWorldPosition()
        {
            if (IsOnFoot && CharacterRoot != null)
                return CharacterRoot.transform.position + Vector3.up * 2.55f;
            if (VehicleRoot != null)
                return VehicleRoot.transform.position + Vector3.up * 2.35f;
            return transform.position + Vector3.up * 2.4f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (IsOnFoot)
            {
                if (CharacterRoot != null)
                {
                    if (!CharacterRoot.activeSelf)
                    {
                        CharacterRoot.SetActive(true);
                        CharacterRoot.transform.position = _targetPos;
                    }
                    CharacterRoot.transform.position = Vector3.Lerp(CharacterRoot.transform.position, _targetPos, dt * 14f);
                    Quaternion targetRot = Quaternion.Euler(0f, _targetYaw, 0f);
                    CharacterRoot.transform.rotation = Quaternion.Slerp(CharacterRoot.transform.rotation, targetRot, dt * 14f);

                    if (_targetSpeed > 0.4f)
                    {
                        _walkAnimTime += dt * (_targetSpeed * 1.15f);
                        float swing = Mathf.Sin(_walkAnimTime) * 42f;
                        if (_leftLeg) _leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
                        if (_rightLeg) _rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
                        if (_leftArm) _leftArm.localRotation = Quaternion.Euler(-swing * 0.85f, 0f, 0f);
                        if (_rightArm) _rightArm.localRotation = Quaternion.Euler(swing * 0.85f, 0f, 0f);
                    }
                    else
                    {
                        if (_leftLeg) _leftLeg.localRotation = Quaternion.Slerp(_leftLeg.localRotation, Quaternion.identity, dt * 10f);
                        if (_rightLeg) _rightLeg.localRotation = Quaternion.Slerp(_rightLeg.localRotation, Quaternion.identity, dt * 10f);
                        if (_leftArm) _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, Quaternion.identity, dt * 10f);
                        if (_rightArm) _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, Quaternion.identity, dt * 10f);
                    }
                }

                // Keep friend's custom vehicle parked at the starting grid when they are walking around on foot!
                if (VehicleRoot != null)
                {
                    VehicleRoot.transform.position = Vector3.Lerp(VehicleRoot.transform.position, _parkedCarPos, dt * 8f);
                    VehicleRoot.transform.rotation = Quaternion.Slerp(VehicleRoot.transform.rotation, _parkedCarRot, dt * 8f);
                    VehicleVisuals.UpdateVisualAnimations(0f, 0f, 0f, false, false);
                }
            }
            else
            {
                if (CharacterRoot != null && CharacterRoot.activeSelf)
                    CharacterRoot.SetActive(false);

                if (VehicleRoot != null)
                {
                    if (Vector3.Distance(VehicleRoot.transform.position, _targetPos) > 25f)
                        VehicleRoot.transform.position = _targetPos;
                    else
                        VehicleRoot.transform.position = Vector3.Lerp(VehicleRoot.transform.position, _targetPos, dt * 15f);

                    Quaternion targetRot = Quaternion.Euler(0f, _targetYaw, 0f);
                    VehicleRoot.transform.rotation = Quaternion.Slerp(VehicleRoot.transform.rotation, targetRot, dt * 15f);

                    if (VehicleVisuals.CurrentForm != _targetForm)
                        VehicleVisuals.SwitchToForm(_targetForm, true);

                    VehicleVisuals.UpdateVisualAnimations(_targetSpeed, _targetSteer, 0f, _isBoosting, _hasShield);
                }
            }
        }
    }

    /// <summary>
    /// Cross-platform Multiplayer Room Manager supporting both Unity WebGL (browser WebSocket via .jslib)
    /// and Unity Editor / Standalone (.NET ClientWebSocket).
    /// Syncs Open-World character exploration, Overhead Name Tags, Custom Garage LEGO Cars, Block Building & Race Starts!
    /// </summary>
    public class LegoMultiplayerRoomManager : MonoBehaviour
    {
        public static LegoMultiplayerRoomManager Instance { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void LegoWS_Connect(string url);
        [DllImport("__Internal")] private static extern void LegoWS_Send(string msg);
        [DllImport("__Internal")] private static extern int LegoWS_GetStatus();
        [DllImport("__Internal")] private static extern int LegoWS_HasMessage();
        [DllImport("__Internal")] private static extern IntPtr LegoWS_PopMessage();
        [DllImport("__Internal")] private static extern void LegoWS_Close();
#endif

        public bool IsRoomPanelOpen { get; set; } = false;
        public string ServerUrl = "ws://localhost:8080";
        public string RoomCode = "PHONG1";
        public string MyPlayerName = "Player";
        public string MyPlayerId { get; private set; }
        public string StatusMessage { get; private set; } = "Chua ket noi (Offline)";

        private readonly Dictionary<string, RemotePlayerAvatar> _remotePlayers = new Dictionary<string, RemotePlayerAvatar>();
        private readonly ConcurrentQueue<string> _incomingQueue = new ConcurrentQueue<string>();

        private ClientWebSocket _editorSocket;
        private CancellationTokenSource _cts;
        private volatile int _editorSocketStatus = 0; // 0 = disconnected, 1 = connecting, 2 = connected, 3 = error
        private bool _sentInitialJoin = false;
        private float _syncTimer = 0f;
        private Texture2D _boxTex;

        public bool IsConnected
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return LegoWS_GetStatus() == 2;
#else
                return _editorSocketStatus == 2 && _editorSocket != null && _editorSocket.State == WebSocketState.Open;
#endif
            }
        }

        public int RemotePlayerCount => _remotePlayers.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            MyPlayerId = "P_" + UnityEngine.Random.Range(10000, 99999);
            MyPlayerName = PlayerPrefs.GetString("LegoMP_PlayerName", "TayDua_" + UnityEngine.Random.Range(10, 99));
            RoomCode = PlayerPrefs.GetString("LegoMP_RoomCode", "PHONG1");
            ServerUrl = PlayerPrefs.GetString("LegoMP_ServerUrl", "ws://localhost:8080");

            _boxTex = new Texture2D(1, 1);
            _boxTex.SetPixel(0, 0, Color.white);
            _boxTex.Apply();
        }

        private void OnDestroy()
        {
            DisconnectFromRoom();
        }

        public void ToggleRoomPanel()
        {
            IsRoomPanelOpen = !IsRoomPanelOpen;
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.55f);
        }

        public void ConnectToRoom()
        {
            DisconnectFromRoom();

            RoomCode = string.IsNullOrEmpty(RoomCode) ? "PHONG1" : RoomCode.Trim().ToUpperInvariant();
            MyPlayerName = string.IsNullOrEmpty(MyPlayerName) ? "TayDua" : MyPlayerName.Trim();
            ServerUrl = string.IsNullOrEmpty(ServerUrl) ? "ws://localhost:8080" : ServerUrl.Trim();

            PlayerPrefs.SetString("LegoMP_PlayerName", MyPlayerName);
            PlayerPrefs.SetString("LegoMP_RoomCode", RoomCode);
            PlayerPrefs.SetString("LegoMP_ServerUrl", ServerUrl);
            PlayerPrefs.Save();

            _sentInitialJoin = false;
            StatusMessage = $"Dang ket noi toi {ServerUrl} (Phong: {RoomCode})...";

#if UNITY_WEBGL && !UNITY_EDITOR
            LegoWS_Connect(ServerUrl);
#else
            _editorSocketStatus = 1;
            _cts = new CancellationTokenSource();
            _ = ConnectEditorWebSocketAsync(ServerUrl, _cts.Token);
#endif
        }

        public void DisconnectFromRoom()
        {
            if (IsConnected)
            {
                SendPacket(new MultiplayerPacket
                {
                    type = "leave",
                    room = RoomCode,
                    playerId = MyPlayerId,
                    playerName = MyPlayerName
                });
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            LegoWS_Close();
#else
            try
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }
                if (_editorSocket != null)
                {
                    _editorSocket.Dispose();
                    _editorSocket = null;
                }
            }
            catch { }
            _editorSocketStatus = 0;
#endif
            ClearAllRemotePlayers();
            _sentInitialJoin = false;
            StatusMessage = "Da ngat ket noi (Offline)";
        }

        private async Task ConnectEditorWebSocketAsync(string url, CancellationToken token)
        {
            try
            {
                _editorSocket = new ClientWebSocket();
                await _editorSocket.ConnectAsync(new Uri(url), token);
                _editorSocketStatus = 2;

                byte[] buffer = new byte[65536];
                while (_editorSocket != null && _editorSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _editorSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                _editorSocketStatus = 0;
                                return;
                            }
                            ms.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string json = Encoding.UTF8.GetString(ms.ToArray());
                            _incomingQueue.Enqueue(json);
                        }
                    }
                }
                _editorSocketStatus = 0;
            }
            catch (Exception)
            {
                _editorSocketStatus = 3;
            }
        }

        private void SendPacket(MultiplayerPacket pkt)
        {
            if (!IsConnected) return;
            string json = JsonUtility.ToJson(pkt);

#if UNITY_WEBGL && !UNITY_EDITOR
            LegoWS_Send(json);
#else
            if (_editorSocket != null && _editorSocket.State == WebSocketState.Open)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                _ = _editorSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
#endif
        }

        public void BroadcastMyCustomCarLoadout()
        {
            if (!IsConnected) return;
            LegoRaceManagerAndHUD mgr = LegoRaceManagerAndHUD.Instance;
            if (mgr == null || mgr.PlayerKart == null || mgr.PlayerKart.Visuals == null) return;

            SendPacket(new MultiplayerPacket
            {
                type = "loadout",
                room = RoomCode,
                playerId = MyPlayerId,
                playerName = MyPlayerName,
                loadoutJson = mgr.PlayerKart.Visuals.Loadout.ExportToJson()
            });
        }

        public void BroadcastStartRaceToRoom()
        {
            if (LegoRaceManagerAndHUD.Instance != null)
            {
                LegoRaceManagerAndHUD.Instance.EnterVehicleAndStartRace();
            }

            if (IsConnected)
            {
                SendPacket(new MultiplayerPacket
                {
                    type = "start_race",
                    room = RoomCode,
                    playerId = MyPlayerId,
                    playerName = MyPlayerName
                });
            }
        }

        public void BroadcastVoxelBlockPlaced(Vector3 worldPos, float yaw, int catalogIndex)
        {
            if (!IsConnected) return;
            SendPacket(new MultiplayerPacket
            {
                type = "voxel_place",
                room = RoomCode,
                playerId = MyPlayerId,
                pos = worldPos,
                yaw = yaw,
                blockIndex = catalogIndex
            });
        }

        public void BroadcastVoxelBlockBroken(Vector3 worldPos)
        {
            if (!IsConnected) return;
            SendPacket(new MultiplayerPacket
            {
                type = "voxel_break",
                room = RoomCode,
                playerId = MyPlayerId,
                pos = worldPos
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                ToggleRoomPanel();
            }

            // 1. Poll WebGL JavaScript WebSocket queue if running in Browser
#if UNITY_WEBGL && !UNITY_EDITOR
            int wsState = LegoWS_GetStatus();
            if (wsState == 3)
                StatusMessage = "Loi ket noi Server! Hay kiem tra Server URL hoac chay node server.js";
            while (LegoWS_HasMessage() == 1)
            {
                IntPtr ptr = LegoWS_PopMessage();
                if (ptr != IntPtr.Zero)
                {
                    string msg = Marshal.PtrToStringUTF8(ptr);
                    _incomingQueue.Enqueue(msg);
                }
            }
#else
            if (_editorSocketStatus == 3)
                StatusMessage = "Khong ket noi duoc Server! Hay chay: node MultiplayerServer/server.js";
#endif

            // 2. When newly connected, send initial "join" packet with our custom LEGO car JSON!
            if (IsConnected && !_sentInitialJoin)
            {
                _sentInitialJoin = true;
                StatusMessage = $"ONLINE — Phong: {RoomCode}";
                SendInitialJoinPacket();
            }

            // 3. Process all received packets on Unity main thread
            while (_incomingQueue.TryDequeue(out string rawJson))
            {
                ProcessIncomingJson(rawJson);
            }

            // 4. Broadcast our live position, character/vehicle state at 15 Hz
            if (IsConnected)
            {
                _syncTimer += Time.unscaledDeltaTime;
                if (_syncTimer >= 0.065f)
                {
                    _syncTimer = 0f;
                    SendMyLiveStatePacket();
                }

                // Clean up timed-out remote players (no packet for > 12s)
                List<string> staleIds = null;
                foreach (var kv in _remotePlayers)
                {
                    if (Time.unscaledTime - kv.Value.LastSeenTime > 12f)
                    {
                        if (staleIds == null) staleIds = new List<string>();
                        staleIds.Add(kv.Key);
                    }
                }
                if (staleIds != null)
                {
                    foreach (string id in staleIds)
                        RemoveRemotePlayer(id);
                }
            }
        }

        private void SendInitialJoinPacket()
        {
            LegoRaceManagerAndHUD mgr = LegoRaceManagerAndHUD.Instance;
            if (mgr == null) return;

            string carJson = (mgr.PlayerKart != null && mgr.PlayerKart.Visuals != null)
                ? mgr.PlayerKart.Visuals.Loadout.ExportToJson()
                : "";

            Vector3 pos = mgr.IsPlayerOnFoot && mgr.PlayerCharacter != null
                ? mgr.PlayerCharacter.transform.position
                : (mgr.PlayerKart != null ? mgr.PlayerKart.transform.position : Vector3.zero);

            SendPacket(new MultiplayerPacket
            {
                type = "join",
                room = RoomCode,
                playerId = MyPlayerId,
                playerName = MyPlayerName,
                isOnFoot = mgr.IsPlayerOnFoot,
                pos = pos,
                loadoutJson = carJson
            });
        }

        private void SendMyLiveStatePacket()
        {
            LegoRaceManagerAndHUD mgr = LegoRaceManagerAndHUD.Instance;
            if (mgr == null) return;

            bool onFoot = mgr.IsPlayerOnFoot;
            Vector3 pos = Vector3.zero;
            float yaw = 0f;
            float spd = 0f;
            float steer = 0f;
            int form = 0;
            bool boosting = false;
            bool shield = false;
            bool stunned = false;
            float hp = 100f;

            if (onFoot && mgr.PlayerCharacter != null)
            {
                pos = mgr.PlayerCharacter.transform.position;
                yaw = mgr.PlayerCharacter.transform.eulerAngles.y;
                Rigidbody rb = mgr.PlayerCharacter.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 v = LegoMeshAndMaterialFactory.GetVelocity(rb);
                    spd = new Vector2(v.x, v.z).magnitude;
                }
            }
            else if (mgr.PlayerKart != null)
            {
                pos = mgr.PlayerKart.transform.position;
                yaw = mgr.PlayerKart.transform.eulerAngles.y;
                spd = mgr.PlayerKart.ForwardSpeed;
                form = (int)mgr.PlayerKart.Visuals.CurrentForm;
                boosting = mgr.PlayerKart.IsBoosting;
                shield = mgr.PlayerKart.HasShield;
                stunned = mgr.PlayerKart.Visuals.IsStunned;
                hp = mgr.PlayerKart.Visuals.CurrentHP;
            }

            SendPacket(new MultiplayerPacket
            {
                type = "state",
                room = RoomCode,
                playerId = MyPlayerId,
                playerName = MyPlayerName,
                isOnFoot = onFoot,
                vehicleForm = form,
                pos = pos,
                yaw = yaw,
                speed = spd,
                steer = steer,
                isBoosting = boosting,
                hasShield = shield,
                isStunned = stunned,
                hp = hp
            });
        }

        private void ProcessIncomingJson(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return;

            MultiplayerPacket pkt;
            try
            {
                pkt = JsonUtility.FromJson<MultiplayerPacket>(rawJson);
            }
            catch
            {
                return;
            }

            if (pkt == null || string.IsNullOrEmpty(pkt.playerId) || pkt.playerId == MyPlayerId)
                return;
            if (!string.IsNullOrEmpty(pkt.room) && !string.Equals(pkt.room, RoomCode, StringComparison.OrdinalIgnoreCase))
                return;

            if (pkt.type == "leave")
            {
                RemoveRemotePlayer(pkt.playerId);
                return;
            }

            if (pkt.type == "voxel_place")
            {
                if (MinecraftWorldBuilderAndInventory.Instance != null)
                    MinecraftWorldBuilderAndInventory.Instance.SpawnNetworkVoxelBlock(pkt.pos, pkt.yaw, pkt.blockIndex);
                return;
            }

            if (pkt.type == "voxel_break")
            {
                if (MinecraftWorldBuilderAndInventory.Instance != null)
                    MinecraftWorldBuilderAndInventory.Instance.BreakNetworkVoxelBlockAt(pkt.pos);
                return;
            }

            if (pkt.type == "start_race")
            {
                if (LegoRaceManagerAndHUD.Instance != null)
                    LegoRaceManagerAndHUD.Instance.EnterVehicleAndStartRace();
                return;
            }

            RemotePlayerAvatar avatar = GetOrCreateRemotePlayer(pkt.playerId, pkt.playerName);

            if (pkt.type == "join")
            {
                avatar.OnReceiveNetworkState(pkt);
                // Reply with our custom car loadout so the newly joined friend immediately sees our custom LEGO vehicle!
                BroadcastMyCustomCarLoadout();
            }
            else if (pkt.type == "loadout")
            {
                avatar.ApplyCustomVehicleLoadoutJson(pkt.loadoutJson);
            }
            else if (pkt.type == "state")
            {
                avatar.OnReceiveNetworkState(pkt);
            }
        }

        private RemotePlayerAvatar GetOrCreateRemotePlayer(string playerId, string playerName)
        {
            if (_remotePlayers.TryGetValue(playerId, out RemotePlayerAvatar existing) && existing != null)
                return existing;

            GameObject obj = new GameObject($"RemoteFriend_{playerId}");
            obj.transform.SetParent(transform, false);
            RemotePlayerAvatar avatar = obj.AddComponent<RemotePlayerAvatar>();

            Color[] friendColors =
            {
                LegoPalette.BrightGreen,
                LegoPalette.BrightBlue,
                LegoPalette.BrightOrange,
                LegoPalette.PurpleRival,
                LegoPalette.CyanGlow
            };
            int colorIdx = Mathf.Abs(playerId.GetHashCode()) % friendColors.Length;

            // Park friend's car near the starting grid
            Vector3 parkedPos = new Vector3(0f, 0.8f, 155f) + Vector3.right * ((_remotePlayers.Count + 1) * 4.2f);
            Quaternion parkedRot = Quaternion.identity;
            if (LegoRaceManagerAndHUD.Instance != null && LegoRaceManagerAndHUD.Instance.PlayerKart != null)
            {
                Transform pk = LegoRaceManagerAndHUD.Instance.PlayerKart.transform;
                parkedPos = pk.position + pk.right * ((_remotePlayers.Count + 1) * 4.5f);
                parkedRot = pk.rotation;
            }

            avatar.Initialize(playerId, playerName, friendColors[colorIdx], parkedPos, parkedRot);
            _remotePlayers[playerId] = avatar;
            return avatar;
        }

        private void RemoveRemotePlayer(string playerId)
        {
            if (_remotePlayers.TryGetValue(playerId, out RemotePlayerAvatar avatar))
            {
                if (avatar != null)
                    Destroy(avatar.gameObject);
                _remotePlayers.Remove(playerId);
            }
        }

        private void ClearAllRemotePlayers()
        {
            foreach (var kv in _remotePlayers)
            {
                if (kv.Value != null)
                    Destroy(kv.Value.gameObject);
            }
            _remotePlayers.Clear();
        }

        /// <summary>
        /// Draws 3D Overhead Name Tags above every friend in the room + the Multiplayer Room UI Modal ([P]).
        /// </summary>
        public void DrawMultiplayerGUI()
        {
            DrawRemotePlayerOverheadNameTags();

            if (IsRoomPanelOpen)
            {
                DrawMultiplayerRoomModal();
            }
        }

        private void DrawRemotePlayerOverheadNameTags()
        {
            if (_remotePlayers.Count == 0) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            GUIStyle tagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            foreach (var kv in _remotePlayers)
            {
                RemotePlayerAvatar friend = kv.Value;
                if (friend == null) continue;

                Vector3 worldHead = friend.GetHeadWorldPosition();
                Vector3 screenPos = cam.WorldToScreenPoint(worldHead);
                if (screenPos.z <= 0f) continue;

                float guiX = screenPos.x;
                float guiY = Screen.height - screenPos.y;
                float dist = Vector3.Distance(cam.transform.position, worldHead);

                string modeStr = friend.IsOnFoot ? "Di Bo" : (friend.IsStunned ? "CHOANG!" : $"Lai Xe ({friend.CurrentHP:F0} HP)");
                string label = $"{friend.PlayerName} [{modeStr}] ({dist:F0}m)";

                Rect boxRect = new Rect(guiX - 105f, guiY - 28f, 210f, 24f);
                DrawRect(boxRect, new Color(0.06f, 0.09f, 0.14f, 0.82f));
                tagStyle.normal.textColor = friend.IsStunned ? LegoPalette.BrightRed : LegoPalette.BrightYellow;
                GUI.Label(boxRect, label, tagStyle);
            }
        }

        private void DrawMultiplayerRoomModal()
        {
            float winW = 500f;
            float winH = 365f;
            Rect win = new Rect((Screen.width - winW) * 0.5f, (Screen.height - winH) * 0.5f, winW, winH);

            DrawRect(win, new Color(0.07f, 0.09f, 0.14f, 0.96f));
            DrawRect(new Rect(win.x, win.y, winW, 4f), IsConnected ? LegoPalette.BrightGreen : LegoPalette.CyanGlow);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = LegoPalette.BrightYellow;
            GUI.Label(new Rect(win.x, win.y + 10f, winW, 28f), "PHONG CHOI CHUNG MULTIPLAYER (WEBGL / PC)", titleStyle);

            GUIStyle lblStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            lblStyle.normal.textColor = Color.white;

            GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            float y = win.y + 50f;
            GUI.Label(new Rect(win.x + 24f, y, 160f, 26f), "Ten Hien Thi Cua Ban:", lblStyle);
            MyPlayerName = GUI.TextField(new Rect(win.x + 195f, y, 275f, 26f), MyPlayerName, 20, fieldStyle);

            y += 38f;
            GUI.Label(new Rect(win.x + 24f, y, 160f, 26f), "Ma Phong (Room Code):", lblStyle);
            RoomCode = GUI.TextField(new Rect(win.x + 195f, y, 275f, 26f), RoomCode, 16, fieldStyle);

            y += 38f;
            GUI.Label(new Rect(win.x + 24f, y, 160f, 26f), "WebSocket Server URL:", lblStyle);
            ServerUrl = GUI.TextField(new Rect(win.x + 195f, y, 275f, 26f), ServerUrl, 128, fieldStyle);

            y += 38f;
            lblStyle.normal.textColor = IsConnected ? LegoPalette.BrightGreen : LegoPalette.BrightOrange;
            GUI.Label(new Rect(win.x + 24f, y, winW - 48f, 24f),
                $"Trang Thai: {StatusMessage}  |  Ban be trong phong: {_remotePlayers.Count}", lblStyle);

            y += 34f;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            Color prevBg = GUI.backgroundColor;
            if (!IsConnected)
            {
                GUI.backgroundColor = LegoPalette.BrightGreen;
                if (GUI.Button(new Rect(win.x + 24f, y, winW - 48f, 36f), "KET NOI VAO PHONG CHOI (JOIN ROOM)", btnStyle))
                {
                    ConnectToRoom();
                }
            }
            else
            {
                GUI.backgroundColor = LegoPalette.CyanGlow;
                if (GUI.Button(new Rect(win.x + 24f, y, (winW - 58f) * 0.58f, 36f), "GOI CA PHONG CUNG DUA XE!", btnStyle))
                {
                    BroadcastStartRaceToRoom();
                    IsRoomPanelOpen = false;
                }

                GUI.backgroundColor = LegoPalette.BrightRed;
                if (GUI.Button(new Rect(win.x + 34f + (winW - 58f) * 0.58f, y, (winW - 58f) * 0.42f, 36f), "ROI PHONG (LEAVE)", btnStyle))
                {
                    DisconnectFromRoom();
                }
            }
            GUI.backgroundColor = prevBg;

            y += 46f;
            lblStyle.fontSize = 11;
            lblStyle.normal.textColor = new Color(0.78f, 0.86f, 0.96f);
            GUI.Label(new Rect(win.x + 24f, y, winW - 48f, 40f),
                "Huong dan: Chay 'node MultiplayerServer/server.js' de mo Server. Cac ban nhap cung Ma Phong se thay nhau di dao, thay xe tu do & cung xay nha / dua xe!", lblStyle);

            if (GUI.Button(new Rect(win.x + (winW - 160f) * 0.5f, win.y + winH - 38f, 160f, 28f), "DONG BANG [P]", btnStyle))
            {
                IsRoomPanelOpen = false;
            }
        }

        private void DrawRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            if (_boxTex != null)
                GUI.DrawTexture(r, _boxTex);
            GUI.color = prev;
        }
    }
}
