using UnityEngine;

namespace Lego2KDrive
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class LegoBlockCharacterController : MonoBehaviour
    {
        [Header("Character Movement")]
        public float walkSpeed = 11f;
        public float sprintSpeed = 19f;
        public float jumpVelocity = 9.5f;
        public float gravityExtra = 20f;

        public bool IsFirstPerson { get; private set; } = false; // Default 3rd Person, press [V] for 1st Person
        public bool IsGrounded { get; private set; }
        public bool IsInWater { get; private set; }
        public float CurrentLookYaw { get; set; }
        public float CurrentLookPitch { get; set; } = 12f;

        private Rigidbody _rb;
        private Transform _visualRoot;
        private Transform _headPivot;
        private Transform _leftArmPivot;
        private Transform _rightArmPivot;
        private Transform _leftLegPivot;
        private Transform _rightLegPivot;

        private float _walkCycleTime;
        private bool _jumpQueued;
        private Vector3 _groundNormal = Vector3.up;
        private bool _lowPolyStyled;
        private static Mesh _facetedOctaPrismMesh;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = 65f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            cap.radius = 0.48f;
            cap.height = 2.0f;
            cap.center = new Vector3(0f, 1.0f, 0f);

#if UNITY_6000_0_OR_NEWER
            PhysicsMaterial slipMat = new PhysicsMaterial("CharSlip")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
#else
            PhysicMaterial slipMat = new PhysicMaterial("CharSlip")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum
            };
#endif
            cap.material = slipMat;

            BuildBlockyCharacterModel();
            CurrentLookYaw = transform.eulerAngles.y;
        }

        private void BuildBlockyCharacterModel()
        {
            GameObject root = new GameObject("BlockyCharacterVisual");
            root.transform.SetParent(transform, false);
            _visualRoot = root.transform;

            // 1. Torso (Faceted Low-Poly Explorer Jacket)
            GameObject torso = CreateBlockPart("Torso", _visualRoot, new Vector3(0f, 0.72f, 0f),
                new Vector3Int(2, 3, 1), LegoPalette.WorkshopTeal);
            torso.transform.localScale = new Vector3(1.1f, 1.0f, 1.1f);
            CreateBlockPart("JacketCollar", torso.transform, new Vector3(0f, 0.80f, 0.02f),
                new Vector3Int(2, 1, 1), LegoPalette.BrightOrange);
            CreateBlockPart("Backpack", torso.transform, new Vector3(0f, 0.14f, -0.34f),
                new Vector3Int(2, 2, 1), LegoPalette.WorkshopWood);

            // 2. Head Pivot & Face with Eyes + Explorer Cap
            GameObject headPivotObj = new GameObject("HeadPivot");
            headPivotObj.transform.SetParent(_visualRoot, false);
            headPivotObj.transform.localPosition = new Vector3(0f, 1.56f, 0f);
            _headPivot = headPivotObj.transform;

            CreateBlockPart("HeadBox", _headPivot, Vector3.zero,
                new Vector3Int(2, 3, 2), new Color(0.96f, 0.78f, 0.58f));
            CreateBlockPart("HairCap", _headPivot, new Vector3(0f, 0.74f, 0f),
                new Vector3Int(2, 1, 2), LegoPalette.BrightBlue);
            GameObject visor = CreateBlockPart("CapVisor", _headPivot, new Vector3(0f, 0.72f, 0.48f),
                new Vector3Int(2, 1, 1), LegoPalette.BrightOrange);
            visor.transform.localScale = new Vector3(0.95f, 0.42f, 0.85f);

            // Low-Poly Eyes
            GameObject eyeL = CreateBlockPart("EyeL", _headPivot, new Vector3(-0.18f, 0.34f, 0.39f),
                new Vector3Int(1, 1, 1), LegoPalette.CarbonBlack);
            eyeL.transform.localScale = new Vector3(0.32f, 0.38f, 0.15f);

            GameObject eyeR = CreateBlockPart("EyeR", _headPivot, new Vector3(0.18f, 0.34f, 0.39f),
                new Vector3Int(1, 1, 1), LegoPalette.CarbonBlack);
            eyeR.transform.localScale = new Vector3(0.32f, 0.38f, 0.15f);

            // 3. Left & Right Swinging Arms
            _leftArmPivot = CreateLimb("LeftArm", new Vector3(-0.62f, 1.50f, 0f),
                new Vector3(0f, -0.75f, 0f), new Vector3Int(1, 3, 1), new Color(0.96f, 0.78f, 0.58f));
            _rightArmPivot = CreateLimb("RightArm", new Vector3(0.62f, 1.50f, 0f),
                new Vector3(0f, -0.75f, 0f), new Vector3Int(1, 3, 1), new Color(0.96f, 0.78f, 0.58f));

            // 4. Left & Right Walking Legs
            _leftLegPivot = CreateLimb("LeftLeg", new Vector3(-0.22f, 0.72f, 0f),
                new Vector3(0f, -0.72f, 0f), new Vector3Int(1, 3, 1), LegoPalette.LowPolyMountainSlate);
            _rightLegPivot = CreateLimb("RightLeg", new Vector3(0.22f, 0.72f, 0f),
                new Vector3(0f, -0.72f, 0f), new Vector3Int(1, 3, 1), LegoPalette.LowPolyMountainSlate);
        }

        private Transform CreateLimb(string name, Vector3 pivotLocalPos, Vector3 meshOffset, Vector3Int brickSize, Color color)
        {
            GameObject pivot = new GameObject(name + "_Pivot");
            pivot.transform.SetParent(_visualRoot, false);
            pivot.transform.localPosition = pivotLocalPos;

            CreateBlockPart(name + "_Mesh", pivot.transform, meshOffset, brickSize, color);
            return pivot.transform;
        }

        private static GameObject CreateBlockPart(string name, Transform parent, Vector3 localPos, Vector3Int size, Color col)
        {
            if (_facetedOctaPrismMesh == null)
                _facetedOctaPrismMesh = BuildUnitOctagonalLowPolyMesh();

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;

            Vector3 dims = new Vector3(size.x * 0.38f, size.y * 0.28f, size.z * 0.38f);
            GameObject meshChild = new GameObject("LowPolyFacet");
            meshChild.transform.SetParent(obj.transform, false);
            meshChild.transform.localPosition = new Vector3(0f, dims.y * 0.5f, 0f);
            meshChild.transform.localScale = dims;
            meshChild.AddComponent<MeshFilter>().sharedMesh = _facetedOctaPrismMesh;
            meshChild.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(col, false, 0.16f);
            return obj;
        }

        public void ToggleCameraPerspective()
        {
            SetFirstPerson(!IsFirstPerson);
        }

        public void SetFirstPerson(bool firstPerson)
        {
            IsFirstPerson = firstPerson;
            if (_headPivot != null)
            {
                // Hide head in 1st-person view so it doesn't block the camera
                _headPivot.gameObject.SetActive(!IsFirstPerson);
            }
            if (LegoProceduralAudio.Instance != null)
                LegoProceduralAudio.Instance.PlayBrickClick(0.5f);
        }

        private void Update()
        {
            if (!LegoGameInput.CharacterControlAllowed)
            {
                _jumpQueued = false;
                return;
            }

            // Toggle 1st Person / 3rd Person view with [V]
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleCameraPerspective();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpQueued = true;
            }

            // Relative mouse movement keeps turning even when cursor is newly locked.
            if (LegoGameInput.CharacterControlAllowed)
            {
                float mx = Input.GetAxisRaw("Mouse X");
                float my = Input.GetAxisRaw("Mouse Y");
                if (Mathf.Abs(mx) > 0.001f || Mathf.Abs(my) > 0.001f)
                {
                    CurrentLookYaw += mx * 3.6f;
                    CurrentLookPitch = Mathf.Clamp(CurrentLookPitch - my * 2.8f, -89f, 89f);
                }
            }

            UpdateLimbWalkAnimation();
        }

        private void FixedUpdate()
        {
            if (!LegoGameInput.CharacterControlAllowed)
            {
                _jumpQueued = false;
                Vector3 velocity = LegoMeshAndMaterialFactory.GetVelocity(_rb);
                LegoMeshAndMaterialFactory.SetVelocity(_rb, new Vector3(0f, velocity.y, 0f));
                return;
            }

            CheckGroundAndWater();

            float h = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;

            float v = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;

            bool sprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float moveSpd = sprinting ? sprintSpeed : walkSpeed;

            Quaternion yawRot = Quaternion.Euler(0f, CurrentLookYaw, 0f);
            Vector3 moveDir = (yawRot * new Vector3(h, 0f, v));
            if (moveDir.sqrMagnitude > 1f)
                moveDir.Normalize();

            // Rotate character body to face movement direction in 3rd-person, or camera yaw in 1st-person
            if (IsFirstPerson)
            {
                transform.rotation = yawRot;
            }
            else if (moveDir.sqrMagnitude > 0.02f)
            {
                Quaternion targetBodyRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRot, Time.fixedDeltaTime * 14f);
            }

            Vector3 curVel = LegoMeshAndMaterialFactory.GetVelocity(_rb);
            float vy = curVel.y;

            if (_jumpQueued && (IsGrounded || IsInWater))
            {
                vy = jumpVelocity;
                IsGrounded = false;
                if (LegoProceduralAudio.Instance != null)
                    LegoProceduralAudio.Instance.PlayJump();
            }
            _jumpQueued = false;

            EnsureLowPolyCharacterStyle();

            if (!IsGrounded)
            {
                _groundNormal = Vector3.up;
                vy -= gravityExtra * Time.fixedDeltaTime;
            }

            Vector3 finalVel = new Vector3(moveDir.x * moveSpd, vy, moveDir.z * moveSpd);
            if (IsGrounded && vy <= 0.1f && moveDir.sqrMagnitude > 0.001f)
            {
                Vector3 planarVel = new Vector3(moveDir.x * moveSpd, 0f, moveDir.z * moveSpd);
                Vector3 projected = Vector3.ProjectOnPlane(planarVel, _groundNormal);
                if (projected.sqrMagnitude > 0.0001f)
                {
                    projected = projected.normalized * planarVel.magnitude;
                }
                finalVel = projected + Vector3.down * 0.35f;
            }
            LegoMeshAndMaterialFactory.SetVelocity(_rb, finalVel);
        }

        private void EnsureLowPolyCharacterStyle()
        {
            if (_lowPolyStyled || _visualRoot == null) return;
            _lowPolyStyled = true;

            if (_facetedOctaPrismMesh == null)
                _facetedOctaPrismMesh = BuildUnitOctagonalLowPolyMesh();

            foreach (MeshRenderer mr in _visualRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                Color c = mr.sharedMaterial != null ? mr.sharedMaterial.color : LegoPalette.BrightBlue;
                mr.sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(c, false, 0.16f);
            }
        }

        private static Mesh BuildUnitOctagonalLowPolyMesh()
        {
            const int sides = 8;
            // 8 top tris + 8 bottom tris + 8 side quads (16 tris) = 32 tris = 96 flat-shaded verts
            Vector3[] verts = new Vector3[96];
            int[] tris = new int[96];
            int vi = 0;

            Vector3 topC = new Vector3(0f, 0.5f, 0f);
            Vector3 botC = new Vector3(0f, -0.5f, 0f);

            for (int i = 0; i < sides; i++)
            {
                float a0 = ((i + 0.5f) / sides) * Mathf.PI * 2f;
                float a1 = ((i + 1.5f) / sides) * Mathf.PI * 2f;
                float rTop = 0.52f;
                float rBot = 0.54f;

                Vector3 t0 = new Vector3(Mathf.Cos(a0) * rTop, 0.46f, Mathf.Sin(a0) * rTop);
                Vector3 t1 = new Vector3(Mathf.Cos(a1) * rTop, 0.46f, Mathf.Sin(a1) * rTop);
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * rBot, -0.48f, Mathf.Sin(a0) * rBot);
                Vector3 b1 = new Vector3(Mathf.Cos(a1) * rBot, -0.48f, Mathf.Sin(a1) * rBot);

                // Side quad (two tris, outward winding for CW/CCW check)
                verts[vi] = b0; verts[vi + 1] = t0; verts[vi + 2] = t1;
                tris[vi] = vi; tris[vi + 1] = vi + 1; tris[vi + 2] = vi + 2; vi += 3;

                verts[vi] = b0; verts[vi + 1] = t1; verts[vi + 2] = b1;
                tris[vi] = vi; tris[vi + 1] = vi + 1; tris[vi + 2] = vi + 2; vi += 3;

                // Top cap
                verts[vi] = topC; verts[vi + 1] = t1; verts[vi + 2] = t0;
                tris[vi] = vi; tris[vi + 1] = vi + 1; tris[vi + 2] = vi + 2; vi += 3;

                // Bottom cap
                verts[vi] = botC; verts[vi + 1] = b0; verts[vi + 2] = b1;
                tris[vi] = vi; tris[vi + 1] = vi + 1; tris[vi + 2] = vi + 2; vi += 3;
            }

            Mesh m = new Mesh { name = "LowPolyCharacterLimb" };
            m.vertices = verts;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private void CheckGroundAndWater()
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 1.25f, ~0, QueryTriggerInteraction.Ignore);

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
                _groundNormal = bestHit.normal.y > 0.45f ? bestHit.normal : Vector3.up;
                TerrainSurfaceTag tag = bestHit.collider.GetComponent<TerrainSurfaceTag>();
                IsInWater = (tag != null && tag.terrainType == TerrainType.Water);
            }
            else
            {
                IsGrounded = false;
                IsInWater = false;
            }
        }

        private void UpdateLimbWalkAnimation()
        {
            Vector3 vel = LegoMeshAndMaterialFactory.GetVelocity(_rb);
            float flatSpeed = new Vector2(vel.x, vel.z).magnitude;

            if (flatSpeed > 0.4f)
            {
                _walkCycleTime += Time.deltaTime * (flatSpeed * 1.15f);
                float swingAngle = Mathf.Sin(_walkCycleTime) * 42f;

                if (_leftLegPivot) _leftLegPivot.localRotation = Quaternion.Euler(swingAngle, 0f, 0f);
                if (_rightLegPivot) _rightLegPivot.localRotation = Quaternion.Euler(-swingAngle, 0f, 0f);
                if (_leftArmPivot) _leftArmPivot.localRotation = Quaternion.Euler(-swingAngle * 0.85f, 0f, 0f);
                if (_rightArmPivot) _rightArmPivot.localRotation = Quaternion.Euler(swingAngle * 0.85f, 0f, 0f);
            }
            else
            {
                float t = Time.deltaTime * 10f;
                if (_leftLegPivot) _leftLegPivot.localRotation = Quaternion.Slerp(_leftLegPivot.localRotation, Quaternion.identity, t);
                if (_rightLegPivot) _rightLegPivot.localRotation = Quaternion.Slerp(_rightLegPivot.localRotation, Quaternion.identity, t);
                if (_leftArmPivot) _leftArmPivot.localRotation = Quaternion.Slerp(_leftArmPivot.localRotation, Quaternion.identity, t);
                if (_rightArmPivot) _rightArmPivot.localRotation = Quaternion.Slerp(_rightArmPivot.localRotation, Quaternion.identity, t);
            }
        }
    }
}

