using CampusRPG.Composition;
using CampusRPG.Input;
using UnityEngine;

namespace CampusRPG.Camera
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lockOnTarget;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.8f, -4.5f);
        [SerializeField] private float followSharpness = 14f;
        [SerializeField] private float horizontalLookSensitivity = 0.15f;
        [SerializeField] private float verticalLookSensitivity = 0.12f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 55f;
        [SerializeField] private float followLookHeight = 1.5f;
        [SerializeField] private float lockOnLookHeight = 1.25f;
        [SerializeField] private float lockOnRotationSpeed = 360f;
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField] private float obstacleProbeRadius = 0.25f;
        [SerializeField] private float obstaclePadding = 0.1f;

        private bool isLockOnActive;
        private float yaw;
        private float pitch = 10f;

        public Transform FollowTarget => followTarget;

        public Transform LockOnTarget => lockOnTarget;

        public bool IsLockOnActive => isLockOnActive;

        private void Awake()
        {
            ResolveInputReader();
            ResolveFollowTarget();
            ThirdPersonCameraOrbitAngles initialAngles = ThirdPersonCameraOrbitUtility.ResolveInitialAngles(transform.rotation, minPitch, maxPitch);
            yaw = initialAngles.Yaw;
            pitch = initialAngles.Pitch;
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void SetLockOnTarget(Transform target)
        {
            lockOnTarget = target;
        }

        public void SetLockOnActive(bool value)
        {
            isLockOnActive = value;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            if (!isLockOnActive || lockOnTarget == null)
            {
                UpdateFreeLook();
            }
            else
            {
                UpdateLockOnLook();
            }

            Vector3 focusPoint = followTarget.position;
            Vector3 playerLookPoint = focusPoint + Vector3.up * followLookHeight;
            ThirdPersonCameraFollowStep followStep = ThirdPersonCameraOrbitUtility.ResolveFollowStep(
                transform.position,
                focusPoint,
                followOffset,
                yaw,
                pitch,
                followSharpness,
                Time.deltaTime);
            transform.position = CameraObstacleResolver.ResolveAdjustedPosition(
                playerLookPoint,
                followStep.Position,
                followTarget,
                obstacleProbeRadius,
                obstaclePadding,
                obstacleMask);

            Vector3 lookPoint = ThirdPersonCameraOrbitUtility.ResolveLookPoint(
                followTarget.position,
                lockOnTarget != null ? lockOnTarget.position : Vector3.zero,
                isLockOnActive,
                lockOnTarget != null,
                followLookHeight,
                lockOnLookHeight);
            Vector3 lookDirection = lookPoint - transform.position;

            if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                followStep.LerpFactor);
        }

        private void UpdateFreeLook()
        {
            ResolveInputReader();

            if (inputReader == null)
            {
                return;
            }

            ThirdPersonCameraOrbitAngles freeLookAngles = ThirdPersonCameraOrbitUtility.ResolveFreeLookAngles(
                yaw,
                pitch,
                inputReader.LookValue,
                horizontalLookSensitivity,
                verticalLookSensitivity,
                minPitch,
                maxPitch);
            yaw = freeLookAngles.Yaw;
            pitch = freeLookAngles.Pitch;
        }

        private void UpdateLockOnLook()
        {
            if (lockOnTarget == null)
            {
                return;
            }

            if (!ThirdPersonCameraOrbitUtility.TryResolveLockOnAngles(
                followTarget.position,
                lockOnTarget.position,
                followLookHeight,
                lockOnLookHeight,
                yaw,
                pitch,
                lockOnRotationSpeed,
                Time.deltaTime,
                minPitch,
                maxPitch,
                out ThirdPersonCameraOrbitAngles lockOnAngles))
            {
                return;
            }

            yaw = lockOnAngles.Yaw;
            pitch = lockOnAngles.Pitch;
        }

        private void ResolveInputReader()
        {
            inputReader = SceneRuntimeReferenceUtility.ResolveInputReader(inputReader);
        }

        private void ResolveFollowTarget()
        {
            followTarget = SceneRuntimeReferenceUtility.ResolveFollowTarget(followTarget);
        }

    }
}
