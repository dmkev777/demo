using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(LegoVehicleVisualAndDamage))]
    public class ArcadeKartController : MonoBehaviour
    {
        [Header("Racer Identity")]
        public string racerName = "Player";
        public bool isPlayer = true;
        public Color primaryColor = LegoPalette.BrightRed;
        public Color accentColor = LegoPalette.BrightYellow;

        [Header("Arcade Driving Specs")]
        public float baseTopSpeed = 34f;
        public float boostTopSpeed = 48f;
        public float acceleration = 28f;
        public float reverseSpeed = 16f;
        public float turnSpeed = 95f;
        public float driftTurnMultiplier = 1.38f;
        public float jumpVelocity = 10.5f;
        public float gravityExtra = 22f;

        [Header("Live Telemetry")]
        public TerrainType CurrentTerrain { get; private set; } = TerrainType.Street;
        public float ForwardSpeed { get; private set; }
        public float SpeedKmh => Mathf.Abs(ForwardSpeed) * 3.6f;
        public float BoostMeter { get; private set; } = 60f; // 0..100
        public bool IsGrounded { get; private set; }
        public bool IsDrifting { get; private set; }
        public bool IsBoosting { get; private set; }
        public bool HasShield => _shieldTimer > 0f;
        public PowerUpType HeldPowerUp { get; private set; } = PowerUpType.None;

        [Header("Race Progress")]
        public int CurrentLap { get; private set; } = 1;
        public int CurrentWaypointIndex { get; private set; } = 0;
        public int WaypointsPassedTotal { get; private set; } = 0;
        public float TotalRaceProgressScore { get; private set; } = 0f;
        public bool HasFinishedRace { get; private set; }

        public LegoVehicleVisualAndDamage Visuals { get; private set; }
        public Rigidbody Rb { get; private set; }

        // Input state (populated by Player keyboard or AI controller)
        private float _throttleInput;
        private float _steerInput;
        private bool _driftHeld;
        private bool _jumpPressed;
        private bool _boostHeld;
        private bool _useItemPressed;
        private Vector3 _groundNormal = Vector3.up;

        // Internal state
        private float _driftDirection;
        private float _driftChargeTime;
        private float _miniTurboTimer;
        private float _shieldTimer;
        private float _spinOutTimer;
        private float _collisionCooldown;
        private TerrainType _lastDetectedSurface = TerrainType.Street;
        private List<Vector3> _trackWaypoints;
        private Vector3 _raceStartAnchorPos;
        private Quaternion _raceStartAnchorRot = Quaternion.identity;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Rb.mass = 120f;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Freeze ALL physics rotation axes (X, Y, Z) so PhysX never spins the kart on its own!
            Rb.constraints = RigidbodyConstraints.FreezeRotation;

            BoxCollider col = GetComponent<BoxCollider>();
            // Keep BoxCollider raised off the ground so its corners never snag on road seams or steps
            col.size = new Vector3(2.0f, 0.65f, 3.1f);
            col.center = new Vector3(0f, 0.58f, 0f);

            // Frictionless physics material so arcade kart doesn't snag on walls or ground seams
#if UNITY_6000_0_OR_NEWER
            PhysicsMaterial zeroFriction = new PhysicsMaterial("ArcadeKartSlip")
            {
                dynamicFriction = 0.0f,
                staticFriction = 0.0f,
                bounciness = 0.0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
#else
            PhysicMaterial zeroFriction = new PhysicMaterial("ArcadeKartSlip")
            {
                dynamicFriction = 0.0f,
                staticFriction = 0.0f,
                bounciness = 0.0f,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
#endif
            col.material = zeroFriction;

            // Single centered smooth sphere collider at the bottom (bottom at y = -0.35f, zero rotational lever arm)
            SphereCollider centerWheelSphere = gameObject.AddComponent<SphereCollider>();
            centerWheelSphere.radius = 0.52f;
            centerWheelSphere.center = new Vector3(0f, 0.17f, 0f);
            centerWheelSphere.material = zeroFriction;

            Visuals = GetComponent<LegoVehicleVisualAndDamage>();
            Visuals.Initialize(primaryColor, accentColor, isPlayer);
        }

        public void SetTrackWaypoints(List<Vector3> waypoints, int startingWaypointIdx = 0)
        {
            _trackWaypoints = waypoints;
            CurrentWaypointIndex = startingWaypointIdx;
            WaypointsPassedTotal = 0;
            CurrentLap = 1;
            HasFinishedRace = false;
        }

        public void TeleportPoseImmediate(Vector3 targetPos, Quaternion targetRot)
        {
            transform.SetPositionAndRotation(targetPos, targetRot);
            if (Rb != null)
            {
                RigidbodyInterpolation prevInterp = Rb.interpolation;
                Rb.interpolation = RigidbodyInterpolation.None;
                Rb.position = targetPos;
                Rb.rotation = targetRot;
                LegoMeshAndMaterialFactory.SetVelocity(Rb, Vector3.zero);
                LegoMeshAndMaterialFactory.SetAngularVelocity(Rb, Vector3.zero);
                Physics.SyncTransforms();
                Rb.interpolation = prevInterp;
            }
            else
            {
                Physics.SyncTransforms();
            }
        }

        public void ResetForRaceStart(Vector3 startPos, Quaternion startRot)
        {
            _raceStartAnchorPos = startPos;
            _raceStartAnchorRot = startRot;
            TeleportPoseImmediate(startPos, startRot);

            BoostMeter = 65f;
            HeldPowerUp = PowerUpType.None;
            CurrentLap = 1;
            CurrentWaypointIndex = 1;
            WaypointsPassedTotal = 0;
            HasFinishedRace = false;
            TotalRaceProgressScore = 0f;
            _throttleInput = _steerInput = 0f;
            _driftHeld = _boostHeld = _jumpPressed = _useItemPressed = false;
            _shieldTimer = 0f;
            _spinOutTimer = 0f;
            _miniTurboTimer = 0f;
            Visuals.FullRepairInstant();
            Visuals.SwitchToForm(TerrainType.Street, false);
        }

        public void SetAIInputs(float throttle, float steer, bool drift, bool boost, bool fireItem)
        {
            if (isPlayer) return;
            _throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            _steerInput = Mathf.Clamp(steer, -1f, 1f);
            _driftHeld = drift;
            _boostHeld = boost;
            if (fireItem) _useItemPressed = true;
        }

        private void Update()
        {
            if (_collisionCooldown > 0f)
                _collisionCooldown -= Time.deltaTime;
            if (_shieldTimer > 0f)
                _shieldTimer -= Time.deltaTime;
            if (_miniTurboTimer > 0f)
                _miniTurboTimer -= Time.deltaTime;
            if (_spinOutTimer > 0f)
                _spinOutTimer -= Time.deltaTime;

            bool raceAllowsDriving = LegoRaceManagerAndHUD.Instance == null || LegoRaceManagerAndHUD.Instance.CanRacersDrive;

            if (isPlayer && raceAllowsDriving && LegoGameInput.GameplayAllowed)
            {
                ReadPlayerInput();
            }
            else if (!raceAllowsDriving || isPlayer)
            {
                _throttleInput = 0f;
                _steerInput = 0f;
                _driftHeld = false;
                _boostHeld = false;
                _jumpPressed = false;
                _useItemPressed = false;
            }

            if (_useItemPressed)
            {
                _useItemPressed = false;
                TryUseHeldPowerUp();
            }

            if (raceAllowsDriving && !HasFinishedRace) UpdateWaypointProgress();
            Visuals.UpdateVisualAnimations(ForwardSpeed, _steerInput, IsDrifting ? _driftDirection : 0f, IsBoosting, HasShield);
        }

        private void ReadPlayerInput()
        {
            float v = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            _throttleInput = v;

            float h = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            _steerInput = h;

            _driftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.Space))
                _jumpPressed = true;

            _boostHeld = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return))
                _useItemPressed = true;
        }

        private void FixedUpdate()
        {
            // Keep vehicle completely stationary and grounded inside the Garage workshop
            if (isPlayer && LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
            {
                if (LegoRaceManagerAndHUD.Instance != null)
                {
                    Vector3 gPos = LegoRaceManagerAndHUD.Instance.GarageInteriorCarPos;
                    Quaternion gRot = LegoRaceManagerAndHUD.Instance.GarageInteriorCarRot;
                    Rb.position = gPos;
                    Rb.rotation = gRot;
                    transform.SetPositionAndRotation(gPos, gRot);
                }
                LegoMeshAndMaterialFactory.SetVelocity(Rb, Vector3.zero);
                LegoMeshAndMaterialFactory.SetAngularVelocity(Rb, Vector3.zero);
                return;
            }

            // Pin player kart to the race starting grid pose during the 3-2-1 countdown
            if (isPlayer && LegoRaceManagerAndHUD.Instance != null &&
                !LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot &&
                !LegoRaceManagerAndHUD.Instance.CanRacersDrive &&
                (LegoGarageBuilder.Instance == null || (!LegoGarageBuilder.Instance.IsGarageOpen && !LegoGarageBuilder.Instance.IsInTestDrive)))
            {
                if (_raceStartAnchorRot != default(Quaternion))
                {
                    Rb.position = _raceStartAnchorPos;
                    Rb.rotation = _raceStartAnchorRot;
                    transform.SetPositionAndRotation(_raceStartAnchorPos, _raceStartAnchorRot);
                }
                LegoMeshAndMaterialFactory.SetVelocity(Rb, Vector3.zero);
                LegoMeshAndMaterialFactory.SetAngularVelocity(Rb, Vector3.zero);
                ForwardSpeed = 0f;
                return;
            }

            // Force zero angular velocity every physics step so the kart never spins on its own
            LegoMeshAndMaterialFactory.SetAngularVelocity(Rb, Vector3.zero);

            if (Visuals.IsStunned)
            {
                Vector3 vel = LegoMeshAndMaterialFactory.GetVelocity(Rb);
                LegoMeshAndMaterialFactory.SetVelocity(Rb, new Vector3(vel.x * 0.88f, vel.y - gravityExtra * Time.fixedDeltaTime, vel.z * 0.88f));
                Vector3 flatFwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                ForwardSpeed = Vector3.Dot(vel, flatFwd);
                return;
            }

            DetectSurfaceAndAutoTransform();

            Vector3 currentVel = LegoMeshAndMaterialFactory.GetVelocity(Rb);
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            ForwardSpeed = Vector3.Dot(currentVel, flatForward);

            VehicleAeroStats aeroStats = (Visuals != null && Visuals.Loadout != null)
                ? Visuals.Loadout.CalculateVehicleStats()
                : new VehicleAeroStats { topSpeedMultiplier = 1f, accelMultiplier = 1f, gripMultiplier = 1f, offRoadMultiplier = 1f };
            if (aeroStats.brickCount <= 0)
            {
                aeroStats.topSpeedMultiplier = 1f;
                aeroStats.accelMultiplier = 1f;
                aeroStats.gripMultiplier = 1f;
                aeroStats.offRoadMultiplier = 1f;
            }

            // 1. Handle Jump
            if (_jumpPressed && IsGrounded)
            {
                _jumpPressed = false;
                currentVel.y = jumpVelocity;
                IsGrounded = false;
                if (isPlayer && LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayJump();
            }
            _jumpPressed = false;

            // 2. Handle Drift & Mini-Turbo Charge
            if (_driftHeld && IsGrounded && ForwardSpeed > 10f && Mathf.Abs(_steerInput) > 0.15f)
            {
                if (!IsDrifting)
                {
                    IsDrifting = true;
                    _driftDirection = Mathf.Sign(_steerInput);
                    _driftChargeTime = 0f;
                }
                _driftChargeTime += Time.fixedDeltaTime;
                BoostMeter = Mathf.Min(100f, BoostMeter + 14f * Time.fixedDeltaTime);
            }
            else
            {
                if (IsDrifting)
                {
                    if (_driftChargeTime > 0.65f)
                    {
                        _miniTurboTimer = 1.1f;
                        if (isPlayer && LegoProceduralAudio.Instance != null)
                            LegoProceduralAudio.Instance.PlayBoost();
                    }
                    IsDrifting = false;
                    _driftDirection = 0f;
                    _driftChargeTime = 0f;
                }
            }

            // 3. Handle Boost
            bool wantsBoost = (_boostHeld && BoostMeter > 2f) || (_miniTurboTimer > 0f);
            if (wantsBoost && _throttleInput >= 0f)
            {
                if (!IsBoosting && isPlayer && _boostHeld && LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayBoost();

                IsBoosting = true;
                if (_miniTurboTimer <= 0f)
                    BoostMeter = Mathf.Max(0f, BoostMeter - 24f * Time.fixedDeltaTime);
            }
            else
            {
                IsBoosting = false;
            }

            // 4. Calculate Target Speed & Aerodynamic / Wheel-Tier Handling
            float terrainSpeedMult = 1.0f;
            if (CurrentTerrain == TerrainType.OffRoad)
                terrainSpeedMult = Mathf.Clamp(0.86f * aeroStats.offRoadMultiplier, 0.72f, 1.08f);

            float maxSpd = (IsBoosting ? boostTopSpeed : baseTopSpeed) * terrainSpeedMult * aeroStats.topSpeedMultiplier;
            float targetForwardSpeed = 0f;
            if (_throttleInput > 0.01f)
                targetForwardSpeed = _throttleInput * maxSpd;
            else if (_throttleInput < -0.01f)
                targetForwardSpeed = _throttleInput * reverseSpeed;
            else if (IsBoosting)
                targetForwardSpeed = maxSpd;

            float accelRate = (IsBoosting ? acceleration * 1.9f : acceleration) * aeroStats.accelMultiplier;
            float newForwardSpeed = Mathf.MoveTowards(ForwardSpeed, targetForwardSpeed, accelRate * Time.fixedDeltaTime);

            // 5. Steering Rotation (Only rotate when player steers AND is moving or pressing throttle)
            float effectiveSteer = _steerInput;
            if (IsDrifting)
            {
                effectiveSteer = (_steerInput * 0.65f + _driftDirection * 0.65f) * driftTurnMultiplier;
            }

            if (Mathf.Abs(effectiveSteer) > 0.01f && (Mathf.Abs(ForwardSpeed) > 0.4f || Mathf.Abs(_throttleInput) > 0.01f || IsBoosting))
            {
                float dirSign = (_throttleInput < -0.01f && ForwardSpeed < 0.5f) ? -1f : 1f;
                float yawDelta = effectiveSteer * turnSpeed * dirSign * Time.fixedDeltaTime;
                transform.Rotate(Vector3.up, yawDelta, Space.World);
            }

            // Smoothly align pitch/roll to terrain slope normal
            Vector3 targetUp = IsGrounded ? _groundNormal : Vector3.up;
            Quaternion slopeAlign = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeAlign, 10f * Time.fixedDeltaTime);

            // 6. Apply Arcade Velocity (modulated by aerodynamics grip multiplier)
            flatForward = Vector3.ProjectOnPlane(transform.forward, _groundNormal).normalized;
            if (flatForward.sqrMagnitude < 0.01f)
                flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            Vector3 lateralVel = Vector3.ProjectOnPlane(currentVel, flatForward);
            lateralVel.y = 0f;

            float gripFactor = (IsDrifting ? 4.5f : 11.5f) * aeroStats.gripMultiplier;
            Vector3 dampedLateral = Vector3.Lerp(lateralVel, Vector3.zero, gripFactor * Time.fixedDeltaTime);

            float newY = currentVel.y;
            if (!IsGrounded)
            {
                newY -= gravityExtra * Time.fixedDeltaTime;
            }

            Vector3 finalVel = flatForward * newForwardSpeed + dampedLateral;
            if (!IsGrounded || _jumpPressed || currentVel.y > 0.5f)
            {
                finalVel.y = newY;
            }

            // Clamp vehicle inside Test Drive Arena bounds when test-driving from Garage
            if (isPlayer && LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsInTestDrive && LegoRaceManagerAndHUD.Instance != null)
            {
                Bounds tb = LegoRaceManagerAndHUD.Instance.TestArenaBounds;
                if (tb.size.x > 2f && tb.size.z > 2f)
                {
                    float pad = 1.6f;
                    float minX = tb.min.x + pad;
                    float maxX = tb.max.x - pad;
                    float minZ = tb.min.z + pad;
                    float maxZ = tb.max.z - pad;
                    Vector3 pos = transform.position;
                    bool clamped = false;
                    if (pos.x < minX) { pos.x = minX; if (finalVel.x < 0f) finalVel.x = 0f; clamped = true; }
                    else if (pos.x > maxX) { pos.x = maxX; if (finalVel.x > 0f) finalVel.x = 0f; clamped = true; }
                    if (pos.z < minZ) { pos.z = minZ; if (finalVel.z < 0f) finalVel.z = 0f; clamped = true; }
                    else if (pos.z > maxZ) { pos.z = maxZ; if (finalVel.z > 0f) finalVel.z = 0f; clamped = true; }
                    if (clamped) transform.position = pos;
                }
            }

            LegoMeshAndMaterialFactory.SetVelocity(Rb, finalVel);
        }

        private void DetectSurfaceAndAutoTransform()
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 1.1f;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 2.3f, ~0, QueryTriggerInteraction.Ignore);

            bool foundGround = false;
            float closestDist = float.MaxValue;
            RaycastHit bestHit = default;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].collider.transform.IsChildOf(transform))
                    continue;
                if (hits[i].distance < closestDist)
                {
                    closestDist = hits[i].distance;
                    bestHit = hits[i];
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                IsGrounded = true;
                _groundNormal = bestHit.normal.y > 0.55f ? bestHit.normal : Vector3.up;
                TerrainSurfaceTag tag = bestHit.collider.GetComponent<TerrainSurfaceTag>();
                if (tag == null && bestHit.collider.transform.parent != null)
                    tag = bestHit.collider.transform.parent.GetComponent<TerrainSurfaceTag>();

                if (tag != null)
                {
                    TerrainType mapped = tag.terrainType == TerrainType.Water ? TerrainType.OffRoad : tag.terrainType;
                    CurrentTerrain = mapped;
                    if (CurrentTerrain != _lastDetectedSurface)
                    {
                        _lastDetectedSurface = CurrentTerrain;
                        Visuals.SwitchToForm(CurrentTerrain, false);
                    }
                }
            }
            else
            {
                IsGrounded = false;
                _groundNormal = Vector3.up;
            }
        }

        private void UpdateWaypointProgress()
        {
            if (_trackWaypoints == null || _trackWaypoints.Count == 0) return;

            Vector3 targetWp = _trackWaypoints[CurrentWaypointIndex];
            float distSq = Vector3.SqrMagnitude(new Vector3(transform.position.x - targetWp.x, 0f, transform.position.z - targetWp.z));

            // Advance waypoint when within 26 units
            if (distSq < 26f * 26f)
            {
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % _trackWaypoints.Count;
                WaypointsPassedTotal++;

                // Crossing finish line (waypoint 0 -> 1 after completing full loop)
                if (CurrentWaypointIndex == 1 && WaypointsPassedTotal >= _trackWaypoints.Count - 1)
                {
                    CurrentLap++;
                    if (LegoRaceManagerAndHUD.Instance != null)
                    {
                        HasFinishedRace = CurrentLap > LegoRaceManagerAndHUD.Instance.totalLaps;
                        LegoRaceManagerAndHUD.Instance.OnRacerCompletedLap(this);
                    }
                }
            }

            float distToNext = Vector3.Distance(transform.position, _trackWaypoints[CurrentWaypointIndex]);
            TotalRaceProgressScore = WaypointsPassedTotal * 1000f - distToNext;
        }

        public void GrantPowerUp(PowerUpType type)
        {
            if (HeldPowerUp == PowerUpType.None)
            {
                HeldPowerUp = type;
                if (isPlayer && LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayPowerUp();
            }
        }

        public void AddBoost(float amount)
        {
            BoostMeter = Mathf.Min(100f, BoostMeter + amount);
            _miniTurboTimer = Mathf.Max(_miniTurboTimer, 0.75f);
            if (isPlayer && LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBoost();
        }

        public void TryUseHeldPowerUp()
        {
            if (HeldPowerUp == PowerUpType.None || Visuals.IsWrecked) return;

            PowerUpType used = HeldPowerUp;
            HeldPowerUp = PowerUpType.None;

            switch (used)
            {
                case PowerUpType.HomingBrickMissile:
                    LegoPowerUpProjectiles.SpawnHomingMissile(this);
                    break;
                case PowerUpType.EnergyBrickShield:
                    _shieldTimer = 5.5f;
                    if (isPlayer && LegoProceduralAudio.Instance != null)
                        LegoProceduralAudio.Instance.PlayPowerUp();
                    break;
                case PowerUpType.SpiderWebMine:
                    LegoPowerUpProjectiles.SpawnHazardMine(this);
                    break;
                case PowerUpType.HyperWarpBoost:
                    BoostMeter = 100f;
                    _miniTurboTimer = 2.6f;
                    _shieldTimer = 2.6f;
                    if (isPlayer && LegoProceduralAudio.Instance != null)
                        LegoProceduralAudio.Instance.PlayBoost();
                    break;
            }
        }

        public void ApplyWeaponImpact(int brickDamage, Vector3 hitForce, bool spinOut)
        {
            if (HasShield || Visuals.IsStunned) return;

            Visuals.TakeBrickDamage(brickDamage, hitForce);
        }

        public void OnVehicleWrecked()
        {
            IsBoosting = false;
            IsDrifting = false;
            _driftDirection = 0f;
            _driftChargeTime = 0f;
            _miniTurboTimer = 0f;
        }

        public void OnVehicleRespawnedFromWreck()
        {
            LegoMeshAndMaterialFactory.SetVelocity(Rb, Vector3.zero);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_collisionCooldown > 0f || Visuals.IsStunned) return;

            // 1. Ignore collisions with drivable road / ground / water surfaces (TerrainSurfaceTag)
            if (collision.collider.GetComponent<TerrainSurfaceTag>() != null)
                return;

            // 2. Check contact normal: ignore floor, slope, or road-seam contacts (where normal points upward)
            Vector3 contactNormal = Vector3.up;
            if (collision.contactCount > 0)
            {
                contactNormal = collision.GetContact(0).normal;
            }

            ArcadeKartController otherKart = collision.collider.GetComponent<ArcadeKartController>();

            if (otherKart != null)
            {
                float relativeSpeed = collision.relativeVelocity.magnitude;
                _collisionCooldown = 0.45f;
                // Ramming rival while boosting or shielded deals heavy HP damage!
                if ((IsBoosting || HasShield) && !otherKart.HasShield)
                {
                    otherKart.Visuals.TakeBrickDamage(4, transform.forward * 14f);
                }
                else if (relativeSpeed > 15f && !HasShield)
                {
                    Visuals.TakeBrickDamage(2, collision.relativeVelocity);
                }
                return;
            }

            // Only count horizontal impacts (walls, buildings, obstacles) — never ground/ramps (normal.y > 0.35f)
            if (Mathf.Abs(contactNormal.y) > 0.35f)
                return;

            // Measure actual impact speed perpendicular INTO the wall (so lightly grazing a barrier doesn't hurt)
            float normalImpactSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contactNormal));
            if (normalImpactSpeed > 14f && !HasShield)
            {
                _collisionCooldown = 0.5f;
                int dmg = normalImpactSpeed > 24f ? 2 : 1;
                Visuals.TakeBrickDamage(dmg, collision.relativeVelocity);
            }
        }
    }
}
