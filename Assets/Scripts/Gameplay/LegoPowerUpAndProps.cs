using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    /// <summary>
    /// Destructible LEGO environment prop (trees, fences, barriers, crates).
    /// Smashing through them in LEGO 2K Drive scatters bricks and automatically repairs your vehicle!
    /// </summary>
    public class DestructibleLegoProp : MonoBehaviour
    {
        public Color propPrimaryColor = LegoPalette.BrightGreen;
        public Color propSecondaryColor = LegoPalette.BrightOrange;
        public int repairBricksGranted = 3;
        public float respawnTime = 9f;

        private GameObject _visualContainer;
        private Collider _collider;
        private bool _isSmashed;

        public void SetupVisuals(GameObject visualChild, Color col1, Color col2)
        {
            _visualContainer = visualChild;
            propPrimaryColor = col1;
            propSecondaryColor = col2;
            _collider = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isSmashed) return;

            ArcadeKartController kart = other.GetComponent<ArcadeKartController>();
            if (kart == null && other.attachedRigidbody != null)
                kart = other.attachedRigidbody.GetComponent<ArcadeKartController>();

            if (kart != null)
            {
                SmashProp(kart);
            }
        }

        private void SmashProp(ArcadeKartController kart)
        {
            _isSmashed = true;
            if (_visualContainer != null)
                _visualContainer.SetActive(false);
            if (_collider != null)
                _collider.enabled = false;

            // Heal the vehicle's HP + add a little boost (without spawning flying debris blocks on the road)!
            kart.Visuals.RepairBricks(repairBricksGranted, transform.position);
            kart.AddBoost(10f);

            if (kart.isPlayer && LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickCrash();

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnTime);
            _isSmashed = false;
            if (_visualContainer != null)
                _visualContainer.SetActive(true);
            if (_collider != null)
                _collider.enabled = true;
        }
    }

    /// <summary>
    /// Floating, rotating LEGO Mystery Item Box that grants a random Power-Up when touched.
    /// </summary>
    public class LegoPowerUpBox : MonoBehaviour
    {
        private GameObject _visualBrick;
        private Collider _trigger;
        private Vector3 _basePos;
        private bool _collected;

        public void Initialize(Vector3 position)
        {
            transform.position = position;
            _basePos = position;

            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.6f;
            _trigger = sc;

            _visualBrick = new GameObject("MysteryBrickVisual");
            _visualBrick.transform.SetParent(transform, false);

            MeshFilter mf = _visualBrick.AddComponent<MeshFilter>();
            mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 3, 2, BrickShape.StandardBrick);
            MeshRenderer mr = _visualBrick.AddComponent<MeshRenderer>();
            mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true);
        }

        private void Update()
        {
            if (_collected || _visualBrick == null) return;

            _visualBrick.transform.Rotate(new Vector3(35f, 95f, 25f) * Time.deltaTime, Space.Self);
            float bob = Mathf.Sin(Time.time * 3.5f + _basePos.x) * 0.35f;
            _visualBrick.transform.localPosition = new Vector3(0f, bob, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            ArcadeKartController kart = other.GetComponent<ArcadeKartController>();
            if (kart == null && other.attachedRigidbody != null)
                kart = other.attachedRigidbody.GetComponent<ArcadeKartController>();

            if (kart != null)
            {
                PowerUpType randomPowerUp = (PowerUpType)Random.Range(1, 5);
                kart.GrantPowerUp(randomPowerUp);
                StartCoroutine(CollectAndRespawnRoutine());
            }
        }

        private IEnumerator CollectAndRespawnRoutine()
        {
            _collected = true;
            _trigger.enabled = false;
            _visualBrick.SetActive(false);

            yield return new WaitForSeconds(5.5f);

            _collected = false;
            _trigger.enabled = true;
            _visualBrick.SetActive(true);
        }
    }

    /// <summary>
    /// Track Boost Pad that gives vehicles an instant burst of speed & boost meter.
    /// </summary>
    public class LegoBoostPad : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            ArcadeKartController kart = other.GetComponent<ArcadeKartController>();
            if (kart == null && other.attachedRigidbody != null)
                kart = other.attachedRigidbody.GetComponent<ArcadeKartController>();

            if (kart != null)
            {
                kart.AddBoost(35f);
            }
        }
    }

    /// <summary>
    /// Spawns and updates Homing Brick Missiles and Hazard Mines.
    /// </summary>
    public static class LegoPowerUpProjectiles
    {
        public static void SpawnHomingMissile(ArcadeKartController owner)
        {
            GameObject missileObj = new GameObject("HomingBrickMissile");
            missileObj.transform.position = owner.transform.position + owner.transform.forward * 3.2f + Vector3.up * 0.8f;
            missileObj.transform.rotation = owner.transform.rotation;

            // Build LEGO Missile visual
            MeshFilter mf = missileObj.AddComponent<MeshFilter>();
            mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 2, 4, BrickShape.SlopeFront);
            MeshRenderer mr = missileObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightRed, true);

            SphereCollider sc = missileObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.4f;

            HomingMissileBehaviour behaviour = missileObj.AddComponent<HomingMissileBehaviour>();
            behaviour.Initialize(owner);

            if (owner.isPlayer && LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBoost();
        }

        public static void SpawnHazardMine(ArcadeKartController owner)
        {
            GameObject mineObj = new GameObject("HazardLegoMine");
            mineObj.transform.position = owner.transform.position - owner.transform.forward * 3.2f + Vector3.up * 0.2f;

            MeshFilter mf = mineObj.AddComponent<MeshFilter>();
            mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(3, 2, 3, BrickShape.StandardBrick);
            MeshRenderer mr = mineObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.PurpleRival, true);

            SphereCollider sc = mineObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.6f;

            HazardMineBehaviour behaviour = mineObj.AddComponent<HazardMineBehaviour>();
            behaviour.Initialize(owner);

            if (owner.isPlayer && LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.75f);
        }
    }

    public class HomingMissileBehaviour : MonoBehaviour
    {
        private ArcadeKartController _owner;
        private ArcadeKartController _target;
        private float _speed = 58f;
        private float _lifeTimer = 5.5f;

        public void Initialize(ArcadeKartController owner)
        {
            _owner = owner;
            FindBestTarget();
        }

        private void FindBestTarget()
        {
            if (LegoRaceManagerAndHUD.Instance == null) return;
            List<ArcadeKartController> racers = LegoRaceManagerAndHUD.Instance.AllRacers;

            float bestDist = float.MaxValue;
            foreach (var r in racers)
            {
                if (r == null || r == _owner || r.Visuals.IsWrecked) continue;
                Vector3 toRival = r.transform.position - transform.position;
                float d = toRival.magnitude;
                if (Vector3.Dot(transform.forward, toRival.normalized) > -0.2f && d < bestDist)
                {
                    bestDist = d;
                    _target = r;
                }
            }
        }

        private void Update()
        {
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (_target != null && !_target.Visuals.IsWrecked)
            {
                Vector3 desiredDir = (_target.transform.position + Vector3.up * 0.6f - transform.position).normalized;
                desiredDir.y = Mathf.Clamp(desiredDir.y, -0.2f, 0.2f);
                transform.forward = Vector3.Slerp(transform.forward, desiredDir, Time.deltaTime * 8.5f);
            }

            transform.position += transform.forward * (_speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            ArcadeKartController hitKart = other.GetComponent<ArcadeKartController>();
            if (hitKart == null && other.attachedRigidbody != null)
                hitKart = other.attachedRigidbody.GetComponent<ArcadeKartController>();

            if (hitKart != null && hitKart != _owner)
            {
                hitKart.ApplyWeaponImpact(6, transform.forward * 18f + Vector3.up * 6f, true);
                Destroy(gameObject);
            }
        }
    }

    public class HazardMineBehaviour : MonoBehaviour
    {
        private ArcadeKartController _owner;
        private float _armDelay = 0.45f;
        private float _lifeTimer = 18f;

        public void Initialize(ArcadeKartController owner)
        {
            _owner = owner;
        }

        private void Update()
        {
            if (_armDelay > 0f) _armDelay -= Time.deltaTime;
            _lifeTimer -= Time.deltaTime;
            transform.Rotate(Vector3.up, 110f * Time.deltaTime, Space.World);
            if (_lifeTimer <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_armDelay > 0f) return;

            ArcadeKartController hitKart = other.GetComponent<ArcadeKartController>();
            if (hitKart == null && other.attachedRigidbody != null)
                hitKart = other.attachedRigidbody.GetComponent<ArcadeKartController>();

            if (hitKart != null && (hitKart != _owner || _lifeTimer < 16f))
            {
                hitKart.ApplyWeaponImpact(4, Vector3.up * 12f, true);
                Destroy(gameObject);
            }
        }
    }
}

