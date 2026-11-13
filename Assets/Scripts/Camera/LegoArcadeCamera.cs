using UnityEngine;

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
        private float _garageOrbitYaw = 38f;
        private float _garageOrbitPitch = 28f;
        private float _garageDistance = 4.8f;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.nearClipPlane = 0.35f;
            _cam.farClipPlane = 750f;
            _cam.allowMSAA = true;
            _cam.allowHDR = true;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material skyMat = new Material(skyShader);
                if (skyMat.HasProperty("_SunSize")) skyMat.SetFloat("_SunSize", 0.055f);
                if (skyMat.HasProperty("_AtmosphereThickness")) skyMat.SetFloat("_AtmosphereThickness", 1.15f);
                if (skyMat.HasProperty("_SkyTint")) skyMat.SetColor("_SkyTint", new Color(0.28f, 0.58f, 0.95f));
                if (skyMat.HasProperty("_GroundColor")) skyMat.SetColor("_GroundColor", new Color(0.42f, 0.56f, 0.45f));
                if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", 0.92f);
                RenderSettings.skybox = skyMat;
                _cam.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0.42f, 0.72f, 0.98f);
            }
        }

        private void LateUpdate()
        {
            if (LegoGameInput.Instance != null && LegoGameInput.Instance.IsPauseMenuOpen) return;
            bool inGarage = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsGarageOpen;
            if (inGarage && targetKart != null)
            {
                UpdateGarageOrbitCamera();
                return;
            }

            bool onFoot = LegoRaceManagerAndHUD.Instance != null && LegoRaceManagerAndHUD.Instance.IsPlayerOnFoot;
            if (onFoot && targetCharacter != null && targetCharacter.gameObject.activeInHierarchy)
            {
                UpdateCharacterCamera();
                return;
            }

            if (targetKart != null)
            {
                UpdateRaceFollowCamera();
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
            bool holdingBrick = LegoGarageBuilder.Instance != null && LegoGarageBuilder.Instance.IsHoldingBrick;
            bool mouseOutsidePanel = Input.mousePosition.x > 375f;

            if (Input.GetMouseButton(1) || Input.GetMouseButton(2) || (!holdingBrick && mouseOutsidePanel && Input.GetMouseButton(0)))
            {
                _garageOrbitYaw += Input.GetAxis("Mouse X") * 4.5f;
                _garageOrbitPitch = Mathf.Clamp(_garageOrbitPitch - Input.GetAxis("Mouse Y") * 3.2f, 10f, 72f);
            }

            _garageDistance = Mathf.Clamp(_garageDistance - Input.mouseScrollDelta.y * 0.45f, 3.0f, 9.5f);

            Quaternion orbitRot = Quaternion.Euler(_garageOrbitPitch, _garageOrbitYaw, 0f);
            Vector3 rightOffset = (orbitRot * Vector3.right) * -0.75f;
            Vector3 pivot = targetKart.transform.position + Vector3.up * 0.45f + rightOffset;
            Vector3 desiredPos = pivot - (orbitRot * Vector3.forward) * _garageDistance;

            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.unscaledDeltaTime * 14f);
            transform.LookAt(pivot);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 48f, Time.unscaledDeltaTime * 8f);
        }

        private void UpdateRaceFollowCamera()
        {
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
