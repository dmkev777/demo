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

            // 1. Torso (Minecraft-style blocky shirt)
            GameObject torso = CreateBlockPart("Torso", _visualRoot, new Vector3(0f, 0.72f, 0f),
                new Vector3Int(2, 3, 1), LegoPalette.CyanGlow);
            torso.transform.localScale = new Vector3(1.1f, 1.0f, 1.1f);

            // 2. Head Pivot & Face with Eyes + Hair
            GameObject headPivotObj = new GameObject("HeadPivot");
            headPivotObj.transform.SetParent(_visualRoot, false);
            headPivotObj.transform.localPosition = new Vector3(0f, 1.56f, 0f);
            _headPivot = headPivotObj.transform;

            CreateBlockPart("HeadBox", _headPivot, Vector3.zero,
                new Vector3Int(2, 3, 2), new Color(0.96f, 0.78f, 0.58f));
            CreateBlockPart("HairCap", _headPivot, new Vector3(0f, 0.75f, 0f),
                new Vector3Int(2, 1, 2), new Color(0.22f, 0.14f, 0.08f));

            // Blocky Eyes
            GameObject eyeL = CreateBlockPart("EyeL", _headPivot, new Vector3(-0.18f, 0.34f, 0.41f),
                new Vector3Int(1, 1, 1), LegoPalette.CarbonBlack);
            eyeL.transform.localScale = new Vector3(0.32f, 0.38f, 0.15f);

            GameObject eyeR = CreateBlockPart("EyeR", _headPivot, new Vector3(0.18f, 0.34f, 0.41f),
                new Vector3Int(1, 1, 1), LegoPalette.CarbonBlack);
            eyeR.transform.localScale = new Vector3(0.32f, 0.38f, 0.15f);

            // 3. Left & Right Swinging Arms
            _leftArmPivot = CreateLimb("LeftArm", new Vector3(-0.62f, 1.50f, 0f),
                new Vector3(0f, -0.75f, 0f), new Vector3Int(1, 3, 1), new Color(0.96f, 0.78f, 0.58f));
            _rightArmPivot = CreateLimb("RightArm", new Vector3(0.62f, 1.50f, 0f),
                new Vector3(0f, -0.75f, 0f), new Vector3Int(1, 3, 1), new Color(0.96f, 0.78f, 0.58f));

            // 4. Left & Right Walking Legs
            _leftLegPivot = CreateLimb("LeftLeg", new Vector3(-0.22f, 0.72f, 0f),
                new Vector3(0f, -0.72f, 0f), new Vector3Int(1, 3, 1), LegoPalette.BrightBlue);
            _rightLegPivot = CreateLimb("RightLeg", new Vector3(0.22f, 0.72f, 0f),
                new Vector3(0f, -0.72f, 0f), new Vector3Int(1, 3, 1), LegoPalette.BrightBlue);
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
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(size.x, size.y, size.z, BrickShape.FlatTile);
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(col, false, 0.45f);
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
            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
                return;

            // Toggle 1st Person / 3rd Person view with [V]
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleCameraPerspective();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpQueued = true;
            }

            // Mouse look rotation (paused when Inventory is open or holding Left Alt to click UI)
            bool inventoryOpen = MinecraftWorldBuilderAndInventory.Instance != null && MinecraftWorldBuilderAndInventory.Instance.IsInventoryOpen;
            bool holdingAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool mouseOverTopRightButtons = (Input.mousePosition.x > Screen.width - 260f && Input.mousePosition.y > Screen.height - 175f);
            bool mouseOverBottomHotbar = (Input.mousePosition.y < 82f && Mathf.Abs(Input.mousePosition.x - Screen.width * 0.5f) < 320f);

            if (!inventoryOpen && !holdingAlt && !mouseOverTopRightButtons && !mouseOverBottomHotbar)
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                if (Mathf.Abs(mx) > 0.001f || Mathf.Abs(my) > 0.001f)
                {
                    CurrentLookYaw += mx * 3.6f;
                    CurrentLookPitch = Mathf.Clamp(CurrentLookPitch - my * 2.8f, -55f, 70f);
                }
            }

            UpdateLimbWalkAnimation();
        }

        private void FixedUpdate()
        {
            if (LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen)
            {
                LegoMeshAndMaterialFactory.SetVelocity(_rb, Vector3.zero);
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

            if (!IsGrounded)
            {
                vy -= gravityExtra * Time.fixedDeltaTime;
            }

            Vector3 finalVel = new Vector3(moveDir.x * moveSpd, vy, moveDir.z * moveSpd);
            LegoMeshAndMaterialFactory.SetVelocity(_rb, finalVel);
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

