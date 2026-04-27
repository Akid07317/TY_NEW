using System;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Input;
using UnityEngine;

namespace CampusRPG.Camera
{
    [DisallowMultipleComponent]
    public sealed class LockOnTargetSelector : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;
        [SerializeField] private ThirdPersonCameraController cameraController;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private float searchRadius = 16f;
        [SerializeField] private float maxAcquireAngle = 75f;
        [SerializeField] private bool clearTargetIfInvalid = true;

        private Transform currentTarget;

        public event Action<Transform> TargetChanged;

        public Transform CurrentTarget => currentTarget;

        public bool HasTarget => currentTarget != null;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (inputReader != null)
            {
                inputReader.LockOnPressed += ToggleLockOn;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.LockOnPressed -= ToggleLockOn;
            }
        }

        private void Update()
        {
            if (!clearTargetIfInvalid || currentTarget == null)
            {
                return;
            }

            if (!LockOnTargetSearchUtility.IsValidTarget(currentTarget, transform.position, searchRadius))
            {
                ClearTarget();
            }
        }

        public void ToggleLockOn()
        {
            if (currentTarget != null)
            {
                ClearTarget();
                return;
            }

            AcquireTarget();
        }

        public bool AcquireTarget()
        {
            Transform nextTarget = FindBestTarget();
            SetCurrentTarget(nextTarget);
            return nextTarget != null;
        }

        public void ClearTarget()
        {
            SetCurrentTarget(null);
        }

        public void ResetRuntimeState()
        {
            SetCurrentTarget(null);
        }

        private Transform FindBestTarget()
        {
            return LockOnTargetSearchUtility.FindBestTarget(
                Physics.OverlapSphere(
                    transform.position,
                    searchRadius,
                    targetMask,
                    QueryTriggerInteraction.Ignore),
                transform,
                cameraTransform,
                searchRadius,
                maxAcquireAngle);
        }

        private void SetCurrentTarget(Transform target)
        {
            currentTarget = target;

            if (cameraController != null)
            {
                cameraController.SetLockOnTarget(currentTarget);
                cameraController.SetLockOnActive(currentTarget != null);
            }

            TargetChanged?.Invoke(currentTarget);
        }

        private void ResolveReferences()
        {
            if (cameraController == null)
            {
                cameraController = GetComponentInChildren<ThirdPersonCameraController>();

                if (cameraController == null)
                {
                    cameraController = SceneRuntimeReferenceUtility.ResolveCameraController(cameraController);
                }
            }

            cameraTransform = SceneRuntimeReferenceUtility.ResolveCameraTransform(
                cameraTransform,
                GetComponent<PlayerCharacter>(),
                cameraController);
            inputReader = SceneRuntimeReferenceUtility.ResolveInputReader(inputReader);
        }
    }
}
