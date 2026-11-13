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

        // Internal state
        private float _driftDirection;
        private float _driftChargeTime;
        private float _miniTurboTimer;
        private float _shieldTimer;
        private float _spinOutTimer;
        private float _collisionCooldown;
        private TerrainType _lastDetectedSurface = TerrainType.Street;
        private List<Vector3> _trackWaypoints;

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

        public void ResetForRaceStart(Vector3 startPos, Quaternion startRot)
        {
            LegoMeshAndMaterialFactory.SetVelocity(Rb, Vector3.zero);
            LegoMeshAndMaterialFactory.SetAngularVelocity(Rb, Vector3.zero);
            transform.SetPositionAndRotation(startPos, startRot);

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

            // Manual form preview keys (1 = Street, 2 = Off-Road, 3 = Boat)
            if (Input.GetKeyDown(KeyCode.Alpha1))
                Visuals.SwitchToForm(TerrainType.Street, true);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                Visuals.SwitchToForm(TerrainType.OffRoad, true);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                Visuals.SwitchToForm(TerrainType.Water, true);
        }

        private void FixedUpdate()
        {
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

            // 4. Calculate Target Speed & Terrain Handling
            float terrainSpeedMult = 1.0f;
            if (CurrentTerrain == TerrainType.OffRoad) terrainSpeedMult = 0.96f;
            else if (CurrentTerrain == TerrainType.Water) terrainSpeedMult = 0.98f;

            float maxSpd = (IsBoosting ? boostTopSpeed : baseTopSpeed) * terrainSpeedMult;
            float targetForwardSpeed = 0f;
            if (_throttleInput > 0.01f)
                targetForwardSpeed = _throttleInput * maxSpd;
            else if (_throttleInput < -0.01f)
                targetForwardSpeed = _throttleInput * reverseSpeed;
            else if (IsBoosting)
                targetForwardSpeed = maxSpd;

            float accelRate = IsBoosting ? acceleration * 1.9f : acceleration;
            float newForwardSpeed = Mathf.MoveTowards(ForwardSpeed, targetForwardSpeed, accelRate * Time.fixedDeltaTime);

            // 5. Steering Rotation (Only rotate when player/AI actually steers AND is moving or pressing throttle)
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

            // 6. Apply Arcade Velocity (with slight lateral slide when drifting or on water)
            flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 lateralVel = Vector3.ProjectOnPlane(currentVel, flatForward);
            lateralVel.y = 0f;

            float gripFactor = IsDrifting ? 4.5f : (CurrentTerrain == TerrainType.Water ? 6.0f : 11.5f);
            Vector3 dampedLateral = Vector3.Lerp(lateralVel, Vector3.zero, gripFactor * Time.fixedDeltaTime);

            float newY = currentVel.y;
            if (!IsGrounded)
            {
                newY -= gravityExtra * Time.fixedDeltaTime;
            }

            Vector3 finalVel = flatForward * newForwardSpeed + dampedLateral;
            finalVel.y = newY;
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
                TerrainSurfaceTag tag = bestHit.collider.GetComponent<TerrainSurfaceTag>();
                if (tag == null && bestHit.collider.transform.parent != null)
                    tag = bestHit.collider.transform.parent.GetComponent<TerrainSurfaceTag>();

                if (tag != null)
                {
                    CurrentTerrain = tag.terrainType;
                    // Automatically transform vehicle when entering a new terrain type!
                    if (CurrentTerrain != _lastDetectedSurface)
                    {
                        _lastDetectedSurface = CurrentTerrain;
                        Visuals.SwitchToForm(CurrentTerrain, true);
                    }
                    else if (Visuals.CurrentForm != CurrentTerrain && !isPlayer)
                    {
                        Visuals.SwitchToForm(CurrentTerrain, true);
                    }
                }
            }
            else
            {
                IsGrounded = false;
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
