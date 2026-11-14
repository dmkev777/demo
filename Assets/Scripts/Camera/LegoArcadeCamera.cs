using UnityEngine;
using UnityEngine.Rendering;

namespace Lego2KDrive
{
    [RequireComponent(typeof(Camera))]
    public class LegoArcadeCamera : MonoBehaviour
    {
        public ArcadeKartController targetKart;
        public LegoBlockCharacterController targetCharacter;
        public Vector3 followOffset = new Vector3(0f, 4.6f, -9.2f);
        public float positionSmoothSpeed = 10f;
        public float rotationSmoothSpeed = 9f;

        private Camera _cam;
        private Material _postProcessMat;
        private float _garageOrbitYaw = 38f;
        private float _garageOrbitPitch = 28f;
        private float _garageDistance = 4.8f;
        private Vector2 _garageAimPan = Vector2.zero;
        private bool _wasInGarage = false;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.nearClipPlane = 0.35f;
            _cam.farClipPlane = 750f;
            _cam.allowMSAA = true;
            _cam.allowHDR = true;

            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                _cam.depthTextureMode |= DepthTextureMode.DepthNormals;
                Shader ppShader = Resources.Load<Shader>("Shaders/MotorversePostProcess");
                if (ppShader == null) ppShader = Shader.Find("Hidden/MotorversePostProcess");
                if (ppShader != null && ppShader.isSupported)
                {
                    _postProcessMat = new Material(ppShader);
                }
            }

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material skyMat = new Material(skyShader);
                if (skyMat.HasProperty("_SunSize")) skyMat.SetFloat("_SunSize", 0.044f);
                if (skyMat.HasProperty("_AtmosphereThickness")) skyMat.SetFloat("_AtmosphereThickness", 1.04f);
                if (skyMat.HasProperty("_SkyTint")) skyMat.SetColor("_SkyTint", new Color(0.28f, 0.46f, 0.78f));
                if (skyMat.HasProperty("_GroundColor")) skyMat.SetColor("_GroundColor", new Color(0.58f, 0.52f, 0.46f));
                if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", 0.90f);
                RenderSettings.skybox = skyMat;
                _cam.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0.38f, 0.66f, 0.96f);
            }
        }

        private void OnDestroy()
        {
            if (_postProcessMat != null)
            {
                Destroy(_postProcessMat);
                _postProcessMat = null;
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_postProcessMat != null && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                Graphics.Blit(src, dest, _postProcessMat);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        private void LateUpdate()
        {
            if (LegoGameInput.Instance != null && LegoGameInput.Instance.IsPauseMenuOpen) return;
            bool inGarage = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen;
            bool garageOrbitView = inGarage && LegoGarageBuilder.Instance.IsCarInspectView;
            if (garageOrbitView && targetKart != null)
            {
                UpdateGarageOrbitCamera();
                return;
            }
            _wasInGarage = false;

            bool onFoot = LegoRaceManagerAndHUD.Instance != null && LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot;
            if ((onFoot || inGarage) && targetCharacter != null && targetCharacter.gameObject.activeInHierarchy)
            {
                UpdateCharacterCamera();
                return;
            }

            if (targetKart != null)
            {
                UpdateRaceFollowCamera();
            }
        }

        public void SnapToTargetImmediate()
        {
            bool onFoot = LegoRaceManagerAndHUD.Instance != null && LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot;
            if (onFoot && targetCharacter != null)
            {
                UpdateCharacterCamera();
                return;
            }

            if (targetKart != null)
            {
                bool inTestDrive = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsInTestDrive;
                if (inTestDrive)
                {
                    _testOrbitYaw = targetKart.transform.eulerAngles.y + 22f;
                    _testOrbitPitch = 16f;
                    _wasInTestDrive = true;
                    Quaternion orbitRot = Quaternion.Euler(_testOrbitPitch, _testOrbitYaw, 0f);
                    Vector3 focusPoint = targetKart.transform.position + Vector3.up * 1.15f;
                    transform.position = focusPoint + (orbitRot * new Vector3(0f, 0f, -7.8f));
                    transform.LookAt(focusPoint);
                    if (_cam != null) _cam.fieldOfView = 58f;
                }
                else
                {
                    _wasInTestDrive = false;
                    Vector3 flatForward = Vector3.ProjectOnPlane(targetKart.transform.forward, Vector3.up).normalized;
                    if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;
                    Vector3 desiredPos = targetKart.transform.position + flatForward * followOffset.z + Vector3.up * followOffset.y;
                    Vector3 lookTarget = targetKart.transform.position + flatForward * 4.5f + Vector3.up * 1.1f;
                    transform.position = desiredPos;
                    transform.rotation = Quaternion.LookRotation(lookTarget - desiredPos, Vector3.up);
                    if (_cam != null) _cam.fieldOfView = 62f;
                }
            }
        }

        private void UpdateCharacterCamera()
        {
            Quaternion lookRot = Quaternion.Euler(targetCharacter.CurrentLookPitch, targetCharacter.CurrentLookYaw, 0f);

            if (targetCharacter.IsFirstPerson)
            {
                // First-Person View (Goc nhin thu 1)
                Vector3 eyePos = targetCharacter.transform.position + Vector3.up * 1.82f;
                transform.position = eyePos;
                transform.rotation = lookRot;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 68f, Time.deltaTime * 12f);
            }
            else
            {
                // Third-Person View (Goc nhin thu 3)
                Vector3 pivot = targetCharacter.transform.position + Vector3.up * 1.48f;
                float dist = 5.5f;
                Vector3 desiredPos = pivot - (lookRot * Vector3.forward) * dist;

                // Preserve exact mouse aim, and pull the camera in before walls/ground.
                float clearDistance = dist;
                foreach (RaycastHit hit in Physics.SphereCastAll(pivot, 0.25f, -(lookRot * Vector3.forward), dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.collider.transform.IsChildOf(targetCharacter.transform))
                        clearDistance = Mathf.Min(clearDistance, Mathf.Max(0.1f, hit.distance - 0.1f));
                }
                desiredPos = pivot - (lookRot * Vector3.forward) * clearDistance;
                transform.SetPositionAndRotation(desiredPos, lookRot);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 60f, Time.deltaTime * 10f);
            }
        }

        private void UpdateGarageOrbitCamera()
        {
            if (!_wasInGarage)
            {
                _garageOrbitYaw = targetKart.transform.eulerAngles.y + 145f;
                _garageOrbitPitch = 22f;
                _garageAimPan = Vector2.zero;
                _wasInGarage = true;
            }

            bool aimingActive = LegoGameInput.IsGarageAimingActive;
            if (aimingActive || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                _garageOrbitYaw += mx * 3.6f;
                _garageOrbitPitch = Mathf.Clamp(_garageOrbitPitch - my * 2.6f, 6f, 78f);
                _garageAimPan.x = Mathf.Clamp(_garageAimPan.x + mx * 0.045f, -0.85f, 0.85f);
                _garageAimPan.y = Mathf.Clamp(_garageAimPan.y + my * 0.035f, -0.35f, 0.55f);
            }

            float panStep = 2.0f * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.A)) _garageAimPan.x = Mathf.Clamp(_garageAimPan.x - panStep, -0.85f, 0.85f);
            if (Input.GetKey(KeyCode.D)) _garageAimPan.x = Mathf.Clamp(_garageAimPan.x + panStep, -0.85f, 0.85f);
            if (Input.GetKey(KeyCode.W)) _garageAimPan.y = Mathf.Clamp(_garageAimPan.y + panStep, -0.35f, 0.55f);
            if (Input.GetKey(KeyCode.S)) _garageAimPan.y = Mathf.Clamp(_garageAimPan.y - panStep, -0.35f, 0.55f);

            _garageDistance = Mathf.Clamp(_garageDistance - Input.mouseScrollDelta.y * 0.45f, 2.6f, 8.5f);

            Quaternion orbitRot = Quaternion.Euler(_garageOrbitPitch, _garageOrbitYaw, 0f);
            Vector3 aimShift = (orbitRot * Vector3.right) * _garageAimPan.x + (orbitRot * Vector3.up) * _garageAimPan.y;
            Vector3 pivot = targetKart.transform.position + Vector3.up * 0.42f + aimShift;
            Vector3 desiredPos = pivot - (orbitRot * Vector3.forward) * _garageDistance;

            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.unscaledDeltaTime * 14f);
            transform.LookAt(pivot);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 48f, Time.unscaledDeltaTime * 8f);
        }

        private float _testOrbitYaw;
        private float _testOrbitPitch = 16f;
        private bool _wasInTestDrive;

        private void UpdateRaceFollowCamera()
        {
            bool inTestDrive = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsInTestDrive;
            if (inTestDrive)
            {
                if (!_wasInTestDrive)
                {
                    _testOrbitYaw = targetKart.transform.eulerAngles.y + 22f;
                    _testOrbitPitch = 16f;
                    _wasInTestDrive = true;
                }

                if (LegoGameInput.GameplayAllowed)
                {
                    _testOrbitYaw += Input.GetAxis("Mouse X") * 3.2f;
                    _testOrbitPitch = Mathf.Clamp(_testOrbitPitch - Input.GetAxis("Mouse Y") * 2.4f, 4f, 52f);
                }

                Quaternion orbitRot = Quaternion.Euler(_testOrbitPitch, _testOrbitYaw, 0f);
                Vector3 orbitOffset = orbitRot * new Vector3(0f, 0f, -7.8f);
                Vector3 focusPoint = targetKart.transform.position + Vector3.up * 1.15f;
                transform.position = Vector3.Lerp(transform.position, focusPoint + orbitOffset, Time.deltaTime * 16f);
                transform.LookAt(focusPoint);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 58f, Time.deltaTime * 6f);
                return;
            }
            _wasInTestDrive = false;

            bool lookBack = LegoGameInput.GameplayAllowed && Input.GetKey(KeyCode.C);
            Vector3 flatForward = Vector3.ProjectOnPlane(targetKart.transform.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;

            if (lookBack) flatForward = -flatForward;

            Vector3 desiredPos = targetKart.transform.position
                                 + flatForward * followOffset.z
                                 + Vector3.up * followOffset.y;

            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * positionSmoothSpeed);

            Vector3 lookTarget = targetKart.transform.position + flatForward * 4.5f + Vector3.up * 1.1f;
            Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationSmoothSpeed);

            float targetFov = targetKart.IsBoosting ? 74f : 62f;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * 6f);
        }
    }
}
