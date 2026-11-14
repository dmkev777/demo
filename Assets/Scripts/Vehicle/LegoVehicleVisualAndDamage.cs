using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    public class LiveBrickInstance
    {
        public LegoBrickSpec spec;
        public GameObject brickObj;          // Outer pivot (handles steering yaw for wheels)
        public Transform wheelSpinTransform; // Inner spinning tire+rim (handles rolling pitch for wheels)
        public Vector3 baseLocalPos;
        public bool isDetached;
        public bool isSteerableFrontWheel;
        public bool isSpinningWheel;
    }

    /// <summary>
    /// Handles building the 3 high-detail LEGO vehicle forms, true centered axle wheel rotation,
    /// terrain shapeshifting animation, brick trail VFX, vehicle HP bar, and 5-second stun when HP reaches 0!
    /// (No flying debris bricks are spawned so racing stays clean and responsive.)
    /// </summary>
    public class LegoVehicleVisualAndDamage : MonoBehaviour
    {
        public const float GROUND_CONTACT_OFFSET_Y = -0.35f; // Matches exact bottom of BoxCollider (0.2 - 0.55)
        public const float MAX_HP = 100f;
        public const float STUN_DURATION = 5.0f;

        public static bool DestructionModeEnabled = true;

        public LegoVehicleLoadout Loadout { get; private set; }
        public TerrainType CurrentForm { get; private set; } = TerrainType.Street;

        public float MaxHP => MAX_HP;
        public float CurrentHP { get; private set; } = MAX_HP;
        public float HealthNormalized => Mathf.Clamp01(CurrentHP / MAX_HP);
        public bool IsStunned => _stunTimer > 0f;
        public bool IsWrecked => IsStunned;
        public float StunTimeRemaining => Mathf.Max(0f, _stunTimer);

        public int TotalDetachableBricks { get; private set; }
        public int AttachedBricksCount { get; private set; }

        private Transform _visualRoot;
        private readonly Dictionary<TerrainType, GameObject> _formRoots = new Dictionary<TerrainType, GameObject>();
        private readonly Dictionary<TerrainType, List<LiveBrickInstance>> _formBricks = new Dictionary<TerrainType, List<LiveBrickInstance>>();

        private float _stunTimer;
        private float _hitShakeTimer;
        private float _transformAnimTimer;
        private float _wheelSpinAngle;
        private float _vfxTrailTimer;
        private GameObject _shieldDomeVisual;
        private GameObject _stunHaloVisual;
        private GameObject[] _boostFlames = new GameObject[2];
        private GameObject _driverCockpitVisual;

        public void Initialize(Color primaryColor, Color accentColor, bool isPlayer = true)
        {
            Loadout = new LegoVehicleLoadout(primaryColor, accentColor, isPlayer);

            if (_visualRoot == null)
            {
                GameObject rootObj = new GameObject("LegoVisualRoot");
                rootObj.transform.SetParent(transform, false);
                rootObj.transform.localPosition = new Vector3(0f, GROUND_CONTACT_OFFSET_Y, 0f);
                _visualRoot = rootObj.transform;
            }

            RebuildAllForms();
            CreateVfxAttachments();
            SwitchToForm(TerrainType.Street, false);
        }

        public void RebuildAllForms()
        {
            foreach (var kv in _formRoots)
            {
                if (kv.Value != null)
                {
                    kv.Value.SetActive(false);
                    Destroy(kv.Value);
                }
            }
            _formRoots.Clear();
            _formBricks.Clear();

            // Single unified customizable car (boat & 3-form shapeshifting removed)
            CurrentForm = TerrainType.Street;
            BuildFormHierarchy(TerrainType.Street);

            if (_formRoots.TryGetValue(TerrainType.Street, out GameObject carRoot) && carRoot != null)
            {
                carRoot.SetActive(true);
            }

            ApplyDamageStateToCurrentForm();
        }

        private void BuildFormHierarchy(TerrainType form)
        {
            GameObject formRoot = new GameObject($"Form_{form}");
            formRoot.transform.SetParent(_visualRoot, false);
            _formRoots[form] = formRoot;

            List<LiveBrickInstance> liveList = new List<LiveBrickInstance>();
            List<LegoBrickSpec> specs = Loadout.GetBricksForForm(form);
            float wheelScale = LegoMeshAndMaterialFactory.GetWheelScaleForTier(Loadout.wheelSizeTier);
            float referenceScaledRadius = 0.378f * wheelScale;
            float stanceLift = Mathf.Max(0f, (referenceScaledRadius - 0.36f) * 0.85f);

            foreach (LegoBrickSpec spec in specs)
            {
                if (spec.shape == BrickShape.Wheel)
                {
                    // Build 2-node Wheel Hierarchy: SteerPivot -> SpinningTire + TechnicAlloyRim
                    float baseRadius = LegoMeshAndMaterialFactory.GetWheelRadius(spec.size.y, spec.size.z);
                    float scaledRadius = baseRadius * wheelScale;
                    Vector3 axleLocalPos = new Vector3(
                        spec.gridPos.x * LegoMeshAndMaterialFactory.STUD_UNIT * Mathf.Max(0.95f, wheelScale * 0.96f),
                        scaledRadius,
                        spec.gridPos.z * LegoMeshAndMaterialFactory.STUD_UNIT
                    );

                    GameObject steerPivot = new GameObject($"WheelPivot_{spec.gridPos}");
                    steerPivot.transform.SetParent(formRoot.transform, false);
                    steerPivot.transform.localPosition = axleLocalPos;

                    GameObject tireObj = new GameObject("TireAndRim");
                    tireObj.transform.SetParent(steerPivot.transform, false);
                    tireObj.transform.localPosition = Vector3.zero;
                    tireObj.transform.localScale = Vector3.one * wheelScale;

                    MeshFilter tmf = tireObj.AddComponent<MeshFilter>();
                    tmf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(spec.size.x, spec.size.y, spec.size.z, BrickShape.Wheel);
                    MeshRenderer tmr = tireObj.AddComponent<MeshRenderer>();
                    tmr.sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(spec.color, false, 0.22f, 0.0f);

                    GameObject rimObj = new GameObject("TechnicAlloyRim");
                    rimObj.transform.SetParent(tireObj.transform, false);
                    rimObj.transform.localPosition = Vector3.zero;
                    MeshFilter rmf = rimObj.AddComponent<MeshFilter>();
                    rmf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(spec.size.x, spec.size.y, spec.size.z, BrickShape.CylinderPart);
                    MeshRenderer rmr = rimObj.AddComponent<MeshRenderer>();
                    rmr.sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.SilverMetallic, false, 0.68f, 0.45f);

                    liveList.Add(new LiveBrickInstance
                    {
                        spec = spec,
                        brickObj = steerPivot,
                        wheelSpinTransform = tireObj.transform,
                        baseLocalPos = axleLocalPos,
                        isDetached = false,
                        isSpinningWheel = true,
                        isSteerableFrontWheel = (spec.gridPos.z > 0)
                    });
                }
                else
                {
                    GameObject bObj = new GameObject($"Brick_{spec.shape}_{spec.gridPos}");
                    bObj.transform.SetParent(formRoot.transform, false);

                    float baseY = spec.gridPos.y * LegoMeshAndMaterialFactory.PLATE_UNIT;
                    float adjustedY = Mathf.Max(0.18f, baseY + stanceLift);
                    Vector3 localPos = new Vector3(
                        spec.gridPos.x * LegoMeshAndMaterialFactory.STUD_UNIT,
                        adjustedY,
                        spec.gridPos.z * LegoMeshAndMaterialFactory.STUD_UNIT
                    );
                    bObj.transform.localPosition = localPos;

                    MeshFilter mf = bObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(spec.size.x, spec.size.y, spec.size.z, spec.shape);

                    MeshRenderer mr = bObj.AddComponent<MeshRenderer>();
                    float smoothness = (spec.shape == BrickShape.Windshield) ? 0.82f : (spec.isCoreChassis ? 0.32f : 0.58f);
                    float metallic = (spec.shape == BrickShape.V8Engine) ? 0.45f : 0.08f;
                    mr.sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(spec.color, spec.isEmissive, smoothness, metallic);

                    liveList.Add(new LiveBrickInstance
                    {
                        spec = spec,
                        brickObj = bObj,
                        wheelSpinTransform = null,
                        baseLocalPos = localPos,
                        isDetached = false,
                        isSpinningWheel = false,
                        isSteerableFrontWheel = false
                    });
                }
            }

            // Soft under-chassis contact shadow grounding the vehicle onto the workshop floor and track
            GameObject shadowQuad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shadowQuad.name = "UnderbodyShadow";
            Destroy(shadowQuad.GetComponent<Collider>());
            shadowQuad.transform.SetParent(formRoot.transform, false);
            shadowQuad.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            shadowQuad.transform.localScale = new Vector3(1.95f, 0.01f, 3.15f);
            MeshRenderer shadowMr = shadowQuad.GetComponent<MeshRenderer>();
            shadowMr.sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(new Color(0.05f, 0.06f, 0.08f), false, 0.08f, 0.0f);
            shadowMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _driverCockpitVisual = BuildSeatedLowPolyDriver(formRoot.transform, Mathf.Max(0.56f, 0.68f + stanceLift));
            _formBricks[form] = liveList;
        }

        private GameObject BuildSeatedLowPolyDriver(Transform parent, float seatY)
        {
            GameObject driver = new GameObject("SeatedLowPolyDriver");
            driver.transform.SetParent(parent, false);
            driver.transform.localPosition = new Vector3(0f, seatY, -0.08f);

            Color suitCol = Loadout != null ? Loadout.primaryColor : LegoPalette.BrightBlue;
            Color skinCol = new Color(0.95f, 0.78f, 0.62f);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(torso.GetComponent<Collider>());
            torso.transform.SetParent(driver.transform, false);
            torso.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            torso.transform.localScale = new Vector3(0.46f, 0.42f, 0.28f);
            torso.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(suitCol, false, 0.22f);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(driver.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.64f, 0.02f);
            head.transform.localScale = new Vector3(0.32f, 0.30f, 0.32f);
            head.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(skinCol, false, 0.20f);

            GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(helmet.GetComponent<Collider>());
            helmet.transform.SetParent(driver.transform, false);
            helmet.transform.localPosition = new Vector3(0f, 0.76f, -0.01f);
            helmet.transform.localScale = new Vector3(0.36f, 0.16f, 0.38f);
            helmet.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.PureWhite, false, 0.28f);

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(visor.GetComponent<Collider>());
            visor.transform.SetParent(driver.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.67f, 0.16f);
            visor.transform.localScale = new Vector3(0.28f, 0.12f, 0.06f);
            visor.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.CarbonBlack, false, 0.65f);

            return driver;
        }

        private void CreateVfxAttachments()
        {
            _shieldDomeVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _shieldDomeVisual.name = "EnergyBrickShieldDome";
            Destroy(_shieldDomeVisual.GetComponent<Collider>());
            _shieldDomeVisual.transform.SetParent(_visualRoot, false);
            _shieldDomeVisual.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            _shieldDomeVisual.transform.localScale = new Vector3(3.4f, 2.2f, 4.6f);
            _shieldDomeVisual.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true, 0.9f);
            _shieldDomeVisual.SetActive(false);

            // Overhead spinning Stun Halo (4 glowing stars/blocks) shown during the 5-second Stun when HP hits 0
            _stunHaloVisual = new GameObject("StunDizzyHalo");
            _stunHaloVisual.transform.SetParent(_visualRoot, false);
            _stunHaloVisual.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                GameObject star = new GameObject($"StunStar_{i}");
                star.transform.SetParent(_stunHaloVisual.transform, false);
                star.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.95f, 0f, Mathf.Sin(angle) * 0.95f);
                star.transform.localRotation = Quaternion.Euler(25f, i * 90f + 45f, 25f);
                MeshFilter smf = star.AddComponent<MeshFilter>();
                smf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(1, 1, 1, BrickShape.FlatTile);
                MeshRenderer smr = star.AddComponent<MeshRenderer>();
                Color starCol = (i % 2 == 0) ? LegoPalette.BrightYellow : LegoPalette.BrightOrange;
                smr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(starCol, true);
            }
            _stunHaloVisual.SetActive(false);

            for (int i = 0; i < 2; i++)
            {
                GameObject flame = new GameObject($"BoostFlame_{i}");
                flame.transform.SetParent(_visualRoot, false);
                flame.transform.localPosition = new Vector3(i == 0 ? -0.36f : 0.36f, 0.55f, -2.35f);
                MeshFilter mf = flame.AddComponent<MeshFilter>();
                mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(1, 1, 3, BrickShape.SlopeFront);
                MeshRenderer mr = flame.AddComponent<MeshRenderer>();
                mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true);
                flame.SetActive(false);
                _boostFlames[i] = flame;
            }
        }

        public void SwitchToForm(TerrainType newForm, bool playTransformEffect = true)
        {
            if (newForm == TerrainType.Water) newForm = TerrainType.OffRoad;
            CurrentForm = newForm;

            // Single customizable car always stays active on TerrainType.Street root
            if (_formRoots.TryGetValue(TerrainType.Street, out GameObject carRoot) && carRoot != null)
                carRoot.SetActive(true);

            ApplyDamageStateToCurrentForm();
        }

        private void ApplyDamageStateToCurrentForm()
        {
            if (!_formBricks.TryGetValue(TerrainType.Street, out List<LiveBrickInstance> bricks))
                return;

            TotalDetachableBricks = bricks.Count;
            AttachedBricksCount = bricks.Count;

            foreach (var b in bricks)
            {
                b.isDetached = false;
                if (b.brickObj != null)
                    b.brickObj.SetActive(true);
            }
        }

        public void ResetPoseForGarage()
        {
            if (_visualRoot == null) return;
            _transformAnimTimer = 0f;
            _hitShakeTimer = 0f;
            _visualRoot.localScale = Vector3.one;
            _visualRoot.localPosition = new Vector3(0f, GROUND_CONTACT_OFFSET_Y, 0f);
            _visualRoot.localRotation = Quaternion.identity;
            if (_driverCockpitVisual != null && _driverCockpitVisual.activeSelf)
                _driverCockpitVisual.SetActive(false);
            if (_stunHaloVisual != null)
                _stunHaloVisual.SetActive(false);
        }

        public void UpdateVisualAnimations(float forwardSpeed, float steerInput, float driftTilt, bool isBoosting, bool hasShield)
        {
            if (_visualRoot == null) return;

            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
            {
                ResetPoseForGarage();
                return;
            }

            if (_driverCockpitVisual != null && !_driverCockpitVisual.activeSelf)
            {
                _driverCockpitVisual.SetActive(true);
            }

            // 0. Update 5-second Stun timer when HP reaches 0
            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                if (_stunTimer <= 0f)
                {
                    _stunTimer = 0f;
                    CurrentHP = MAX_HP;
                    _transformAnimTimer = 0.28f;
                    if (LegoProceduralAudio.Instance != null)
                        LegoProceduralAudio.Instance.PlayPowerUp();
                }
            }

            if (_hitShakeTimer > 0f)
            {
                _hitShakeTimer -= Time.deltaTime;
            }

            // 1. Shapeshifting elastic pop scale animation
            if (_transformAnimTimer > 0f)
            {
                _transformAnimTimer -= Time.deltaTime;
                float t = 1f - (_transformAnimTimer / 0.28f);
                float bounce = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.25f * (1f - t);
                _visualRoot.localScale = new Vector3(bounce, 2f - bounce, bounce);
            }
            else
            {
                _visualRoot.localScale = Vector3.Lerp(_visualRoot.localScale, Vector3.one, Time.deltaTime * 14f);
            }

            // 2. Ground flush offset + boat wave / off-road suspension / stun wobble motion
            float targetRoll = -steerInput * 4.0f - driftTilt * 9.5f;
            float wavePitch = 0f;
            float yOffset = GROUND_CONTACT_OFFSET_Y;

            if (IsStunned)
            {
                targetRoll = Mathf.Sin(Time.time * 16f) * 8.5f;
                wavePitch = Mathf.Cos(Time.time * 14f) * 5.0f;
            }
            else if (_hitShakeTimer > 0f)
            {
                targetRoll += Mathf.Sin(Time.time * 45f) * 7.0f;
            }
            else if (CurrentForm == TerrainType.OffRoad)
            {
                if (Mathf.Abs(forwardSpeed) > 2f)
                    yOffset = GROUND_CONTACT_OFFSET_Y + Mathf.Abs(Mathf.Sin(Time.time * 20f)) * 0.04f;
            }

            _visualRoot.localPosition = Vector3.Lerp(_visualRoot.localPosition, new Vector3(0f, yOffset, 0f), Time.deltaTime * 15f);
            Quaternion targetRot = Quaternion.Euler(wavePitch, driftTilt * 13f, targetRoll);
            _visualRoot.localRotation = Quaternion.Slerp(_visualRoot.localRotation, targetRot, Time.deltaTime * 11f);

            // 3. True Centered Axle Wheel Rotation (Steering on Pivot Y, Rolling on Tire X)
            _wheelSpinAngle += forwardSpeed * 42f * Time.deltaTime;
            if (_formBricks.TryGetValue(TerrainType.Street, out List<LiveBrickInstance> bricks))
            {
                foreach (var b in bricks)
                {
                    if (b.isSpinningWheel && b.brickObj != null && b.brickObj.activeSelf)
                    {
                        float yaw = b.isSteerableFrontWheel ? steerInput * 30f : 0f;
                        b.brickObj.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        if (b.wheelSpinTransform != null)
                        {
                            b.wheelSpinTransform.localRotation = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);
                        }
                    }
                }
            }

            // 4. Spawn LEGO Trail VFX Puffs (Drift Tire Smoke / Off-Road Dust)
            if (!IsStunned && Mathf.Abs(forwardSpeed) > 12f)
            {
                _vfxTrailTimer += Time.deltaTime;
                float interval = (Mathf.Abs(driftTilt) > 0.1f || isBoosting) ? 0.07f : 0.18f;
                if (_vfxTrailTimer >= interval)
                {
                    _vfxTrailTimer = 0f;
                    SpawnLegoTrailPuff(driftTilt, isBoosting);
                }
            }

            // 5. Stun Halo, Boost flames & Shield visual
            if (_stunHaloVisual != null)
            {
                _stunHaloVisual.SetActive(IsStunned);
                if (IsStunned)
                {
                    _stunHaloVisual.transform.Rotate(Vector3.up, 260f * Time.deltaTime, Space.Self);
                }
            }

            if (_shieldDomeVisual != null)
            {
                _shieldDomeVisual.SetActive(hasShield);
                if (hasShield)
                    _shieldDomeVisual.transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.Self);
            }

            for (int i = 0; i < _boostFlames.Length; i++)
            {
                if (_boostFlames[i] != null)
                {
                    _boostFlames[i].SetActive(isBoosting && !IsStunned);
                    if (isBoosting)
                    {
                        float flicker = Random.Range(0.85f, 1.55f);
                        _boostFlames[i].transform.localScale = new Vector3(1f, 1f, flicker);
                    }
                }
            }
        }

        private void SpawnLegoTrailPuff(float driftTilt, bool isBoosting)
        {
            if (CurrentForm == TerrainType.Street && Mathf.Abs(driftTilt) < 0.1f && !isBoosting)
                return;

            Color puffCol = CurrentForm == TerrainType.Water ? LegoPalette.PureWhite :
                            (CurrentForm == TerrainType.OffRoad ? LegoPalette.TanSand :
                            (isBoosting ? LegoPalette.BrightOrange : LegoPalette.LightGrey));

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 spawnPos = transform.TransformPoint(new Vector3(side * 0.85f, -0.2f, -1.6f));
                GameObject puff = new GameObject("LegoTrailPuff");
                puff.transform.position = spawnPos;
                puff.transform.rotation = Random.rotation;

                MeshFilter mf = puff.AddComponent<MeshFilter>();
                mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(1, 1, 1, BrickShape.FlatTile);
                MeshRenderer mr = puff.AddComponent<MeshRenderer>();
                mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(puffCol, isBoosting || CurrentForm == TerrainType.Water);

                Destroy(puff, 0.38f);
            }
        }

        /// <summary>
        /// Reduces vehicle HP on collision or weapon hit without detaching or spawning any flying bricks.
        /// When HP reaches 0, the vehicle is stunned in place for 5 seconds, then recovers full HP!
        /// </summary>
        public void TakeBrickDamage(int damagePoints, Vector3 impactVelocity)
        {
            if (!DestructionModeEnabled || IsStunned || damagePoints <= 0) return;

            float hpLoss = damagePoints * 12f;
            CurrentHP = Mathf.Max(0f, CurrentHP - hpLoss);
            _hitShakeTimer = 0.24f;

            if (LegoProceduralAudio.Instance != null)
            {
                LegoProceduralAudio.Instance.PlayBrickCrash();
            }

            if (CurrentHP <= 0f)
            {
                CurrentHP = 0f;
                _stunTimer = STUN_DURATION;

                ArcadeKartController kart = GetComponent<ArcadeKartController>();
                if (kart != null)
                {
                    kart.OnVehicleWrecked();
                }
            }
        }

        public void RepairBricks(int count, Vector3 propWorldPos)
        {
            if (IsStunned || CurrentHP >= MAX_HP) return;

            float healAmount = count * 10f;
            CurrentHP = Mathf.Min(MAX_HP, CurrentHP + healAmount);

            if (LegoProceduralAudio.Instance != null)
            {
                LegoProceduralAudio.Instance.PlayBrickClick(0.6f);
            }
        }

        public void FullRepairInstant()
        {
            CurrentHP = MAX_HP;
            _stunTimer = 0f;
            _hitShakeTimer = 0f;
            if (_stunHaloVisual != null)
                _stunHaloVisual.SetActive(false);
            ApplyDamageStateToCurrentForm();
        }
    }
}
