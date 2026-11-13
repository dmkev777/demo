using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    [RequireComponent(typeof(ArcadeKartController))]
    public class LegoAIRacer : MonoBehaviour
    {
        public float lateralLaneOffset = 0f;
        public float skillMultiplier = 0.96f;

        private ArcadeKartController _kart;
        private List<Vector3> _waypoints;
        private float _stuckTimer;
        private float _reverseRecoveryTimer;
        private float _itemUseDelayTimer;

        public void InitializeAI(List<Vector3> waypoints, float laneOffset, float skill)
        {
            _kart = GetComponent<ArcadeKartController>();
            _waypoints = waypoints;
            lateralLaneOffset = laneOffset;
            skillMultiplier = skill;
            _kart.baseTopSpeed *= skillMultiplier;
        }

        private void Update()
        {
            if (_kart == null || _waypoints == null || _waypoints.Count == 0) return;

            if ((LegoRaceManagerAndHUD.Instance != null && !LegoRaceManagerAndHUD.Instance.CanRacersDrive) ||
                (_kart.Visuals != null && _kart.Visuals.IsStunned))
            {
                _stuckTimer = 0f;
                _reverseRecoveryTimer = 0f;
                _kart.SetAIInputs(0f, 0f, false, false, false);
                return;
            }

            // Use held Power-Up after a short reaction delay
            bool fireItem = false;
            if (_kart.HeldPowerUp != PowerUpType.None)
            {
                _itemUseDelayTimer += Time.deltaTime;
                if (_itemUseDelayTimer > 1.4f)
                {
                    fireItem = true;
                    _itemUseDelayTimer = 0f;
                }
            }

            int wpIdx = _kart.CurrentWaypointIndex;
            int nextWpIdx = (wpIdx + 1) % _waypoints.Count;

            Vector3 wpPos = _waypoints[wpIdx];
            Vector3 nextWpPos = _waypoints[nextWpIdx];
            Vector3 segmentDir = (nextWpPos - wpPos).normalized;
            Vector3 segmentRight = Vector3.Cross(Vector3.up, segmentDir).normalized;

            Vector3 targetPoint = wpPos + segmentRight * lateralLaneOffset;
            Vector3 toTarget = targetPoint - transform.position;
            toTarget.y = 0f;

            Vector3 localDir = transform.InverseTransformDirection(toTarget.normalized);
            float steer = Mathf.Clamp(localDir.x * 2.4f, -1f, 1f);

            // Stuck recovery check
            if (Mathf.Abs(_kart.ForwardSpeed) < 2.2f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > 1.8f)
                {
                    _reverseRecoveryTimer = 0.9f;
                    _stuckTimer = 0f;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }

            if (_reverseRecoveryTimer > 0f)
            {
                _reverseRecoveryTimer -= Time.deltaTime;
                _kart.SetAIInputs(-1f, -steer, false, false, false);
                return;
            }

            bool sharpCorner = Mathf.Abs(localDir.x) > 0.42f && _kart.ForwardSpeed > 18f;
            bool straightLine = Mathf.Abs(localDir.x) < 0.16f && _kart.BoostMeter > 55f;

            _kart.SetAIInputs(1f, steer, sharpCorner, straightLine, fireItem);
        }
    }
}

