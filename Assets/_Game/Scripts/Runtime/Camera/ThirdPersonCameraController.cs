using System.Collections.Generic;
using CampusRPG.Composition;
using CampusRPG.Input;
using UnityEngine;

namespace CampusRPG.Camera
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        private const string ImportedVisualRootName = "ImportedVisualRoot";
        private const string ImportedWeaponVisualRootName = "ImportedWeaponVisualRoot";
        private const string CombatProxyVisualRootName = "CombatProxyVisualRoot";

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
        [SerializeField] private float obstructionEnterDistance = 0.08f;
        [SerializeField] private float obstructionExitDistance = 0.03f;
        [SerializeField] private float obstructionFollowSharpness = 10f;
        [SerializeField] private float obstructionOverheadRetractionRatio = 0.82f;
        [SerializeField] private float obstructionOverheadDelay = 0.12f;
        [SerializeField] private float obstructionOverheadLift = 4.5f;
        [SerializeField] private float obstructionOverheadEnterSharpness = 4.5f;
        [SerializeField] private float obstructionOverheadExitSharpness = 7.5f;
        [SerializeField] private float ownerRendererHideDistance = 1.1f;
        [SerializeField] private float ownerRendererRestorePadding = 0.15f;
        [SerializeField] private float impactImpulseVerticalBias = 0.12f;

        private bool isLockOnActive;
        private float yaw;
        private float pitch = 10f;
        private readonly List<Renderer> ownerRenderers = new List<Renderer>();
        private readonly List<bool> ownerRendererForceRenderingOffStates = new List<bool>();
        private Transform cachedOwnerRoot;
        private Transform cachedImportedVisualRoot;
        private Transform cachedImportedWeaponVisualRoot;
        private Transform cachedProxyVisualRoot;
        private bool isObstacleAdjustmentActive;
        private bool ownerRenderersHidden;
        private float obstructionOverheadBlend;
        private float obstructionSeconds;
        private Vector3 impactImpulseDirection;
        private float impactImpulseDistance;
        private float impactImpulseDurationSeconds;
        private float impactImpulseRemainingSeconds;
        private int impactImpulsePriority;

        public Transform FollowTarget => followTarget;

        public Transform LockOnTarget => lockOnTarget;

        public bool IsLockOnActive => isLockOnActive;

        public bool HasActiveImpactImpulse =>
            impactImpulseRemainingSeconds > 0f || impactImpulseDistance > 0.0001f;

        public int CurrentImpactImpulsePriority => HasActiveImpactImpulse ? impactImpulsePriority : 0;

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
            if (followTarget == target)
            {
                return;
            }

            RestoreOwnerRenderers();
            ClearOwnerRendererCache();
            isObstacleAdjustmentActive = false;
            ResetObstructionBlend();
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

        public void ResetRuntimeState()
        {
            lockOnTarget = null;
            isLockOnActive = false;
            isObstacleAdjustmentActive = false;
            ResetObstructionBlend();
            ResetImpactImpulse();
            RestoreOwnerRenderers();
        }

        public void RequestImpactImpulse(Transform source, float distance, float durationSeconds)
        {
            TryRequestImpactImpulse(source, distance, durationSeconds);
        }

        public bool TryRequestImpactImpulse(Transform source, float distance, float durationSeconds, int priority = 0)
        {
            Vector3 sourcePosition = source != null
                ? source.position
                : followTarget != null
                    ? followTarget.position
                    : transform.position - transform.forward;
            return TryRequestImpactImpulse(transform.position - sourcePosition, distance, durationSeconds, priority);
        }

        public void RequestImpactImpulse(Vector3 worldDirection, float distance, float durationSeconds)
        {
            TryRequestImpactImpulse(worldDirection, distance, durationSeconds);
        }

        public bool TryRequestImpactImpulse(Vector3 worldDirection, float distance, float durationSeconds, int priority = 0)
        {
            float resolvedDistance = Mathf.Max(0f, distance);
            float resolvedDuration = Mathf.Max(0f, durationSeconds);

            if (resolvedDistance <= 0f || resolvedDuration <= 0f)
            {
                return false;
            }

            int resolvedPriority = Mathf.Max(0, priority);

            if (HasActiveImpactImpulse && resolvedPriority < impactImpulsePriority)
            {
                return false;
            }

            Vector3 flatDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);

            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                flatDirection = -transform.forward;
            }

            Vector3 biasedDirection = flatDirection.normalized
                + Vector3.up * Mathf.Max(0f, impactImpulseVerticalBias);
            impactImpulseDirection = biasedDirection.sqrMagnitude > 0.0001f
                ? biasedDirection.normalized
                : -transform.forward;
            impactImpulseDistance = resolvedDistance;
            impactImpulseDurationSeconds = resolvedDuration;
            impactImpulseRemainingSeconds = resolvedDuration;
            impactImpulsePriority = resolvedPriority;
            return true;
        }

        private void LateUpdate()
        {
            if (followTarget == null)
            {
                isObstacleAdjustmentActive = false;
                ResetObstructionBlend();
                ResetImpactImpulse();
                RestoreOwnerRenderers();
                return;
            }

            EnsureOwnerRendererCache();

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
            Vector3 desiredPosition = ThirdPersonCameraOrbitUtility.ResolveDesiredPosition(
                focusPoint,
                followOffset,
                yaw,
                pitch);
            CameraObstacleResolution baseResolution = CameraObstacleResolver.Resolve(
                playerLookPoint,
                desiredPosition,
                transform.position,
                followTarget,
                obstacleProbeRadius,
                obstaclePadding,
                obstacleMask);
            UpdateObstructionOverheadBlend(baseResolution);

            Vector3 adjustedPosition = baseResolution.Position;

            if (obstructionOverheadBlend > 0.001f)
            {
                Vector3 overheadDesiredPosition = ResolveOverheadDesiredPosition(baseResolution.Position);
                adjustedPosition = CameraObstacleResolver.Resolve(
                    playerLookPoint,
                    overheadDesiredPosition,
                    transform.position,
                    followTarget,
                    obstacleProbeRadius,
                    obstaclePadding,
                    obstacleMask).Position;
            }

            adjustedPosition += ResolveImpactImpulseOffset(Time.deltaTime);

            float desiredOffset = Vector3.Distance(adjustedPosition, desiredPosition);
            isObstacleAdjustmentActive = isObstacleAdjustmentActive
                ? desiredOffset >= Mathf.Max(0f, obstructionExitDistance) || obstructionOverheadBlend > 0.001f
                : desiredOffset >= Mathf.Max(0f, obstructionEnterDistance) || baseResolution.HasStaticObstruction;
            float clearLerpFactor = ThirdPersonCameraOrbitUtility.ResolveLerpFactor(followSharpness, Time.deltaTime);
            float obstructionLerpFactor = ThirdPersonCameraOrbitUtility.ResolveLerpFactor(obstructionFollowSharpness, Time.deltaTime);
            bool currentPositionRequiresSnap = CameraObstacleResolver.IsPositionOccupied(
                transform.position,
                followTarget,
                obstacleProbeRadius,
                obstacleMask);
            bool movementPathRequiresSnap = CameraObstacleResolver.IsSegmentObstructed(
                    transform.position,
                    adjustedPosition,
                    followTarget,
                    obstacleProbeRadius,
                    obstacleMask)
                || CameraObstacleResolver.IsSegmentOccupiedByDynamicActor(
                    transform.position,
                    adjustedPosition,
                    followTarget,
                    obstacleProbeRadius,
                    obstacleMask);
            bool shouldSnapToObstacle = isObstacleAdjustmentActive && (currentPositionRequiresSnap || movementPathRequiresSnap);
            float positionLerpFactor = isObstacleAdjustmentActive
                ? obstructionLerpFactor
                : clearLerpFactor;
            transform.position = shouldSnapToObstacle
                ? adjustedPosition
                : Vector3.Lerp(transform.position, adjustedPosition, positionLerpFactor);

            Vector3 lookPoint = ThirdPersonCameraOrbitUtility.ResolveLookPoint(
                followTarget.position,
                lockOnTarget != null ? lockOnTarget.position : Vector3.zero,
                isLockOnActive,
                lockOnTarget != null,
                followLookHeight,
                lockOnLookHeight);
            UpdateOwnerRendererVisibility(playerLookPoint, adjustedPosition);
            Vector3 lookDirection = lookPoint - transform.position;

            if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                positionLerpFactor);
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

        private void OnDisable()
        {
            isObstacleAdjustmentActive = false;
            ResetObstructionBlend();
            ResetImpactImpulse();
            RestoreOwnerRenderers();
        }

        private void UpdateObstructionOverheadBlend(CameraObstacleResolution resolution)
        {
            bool shouldBuildOverhead = resolution.HasStaticObstruction
                && !resolution.UsedNarrowObstacleSidestep
                && resolution.RetractionRatio <= Mathf.Clamp01(obstructionOverheadRetractionRatio);

            if (shouldBuildOverhead)
            {
                obstructionSeconds += Time.deltaTime;
            }
            else
            {
                obstructionSeconds = 0f;
            }

            float targetBlend = obstructionSeconds >= Mathf.Max(0f, obstructionOverheadDelay) ? 1f : 0f;
            float sharpness = targetBlend > obstructionOverheadBlend
                ? obstructionOverheadEnterSharpness
                : obstructionOverheadExitSharpness;
            float lerpFactor = ThirdPersonCameraOrbitUtility.ResolveLerpFactor(sharpness, Time.deltaTime);
            obstructionOverheadBlend = Mathf.Lerp(obstructionOverheadBlend, targetBlend, lerpFactor);

            if (targetBlend <= 0f && obstructionOverheadBlend < 0.001f)
            {
                obstructionOverheadBlend = 0f;
            }
        }

        private Vector3 ResolveOverheadDesiredPosition(Vector3 retractedPosition)
        {
            return retractedPosition
                + Vector3.up * (Mathf.Max(0f, obstructionOverheadLift) * Mathf.Clamp01(obstructionOverheadBlend));
        }

        private void ResetObstructionBlend()
        {
            obstructionOverheadBlend = 0f;
            obstructionSeconds = 0f;
        }

        private Vector3 ResolveImpactImpulseOffset(float deltaTime)
        {
            if (impactImpulseRemainingSeconds <= 0f || impactImpulseDistance <= 0f)
            {
                ResetImpactImpulse();
                return Vector3.zero;
            }

            float duration = Mathf.Max(0.0001f, impactImpulseDurationSeconds);
            float normalizedRemaining = Mathf.Clamp01(impactImpulseRemainingSeconds / duration);
            Vector3 offset = impactImpulseDirection * (impactImpulseDistance * normalizedRemaining * normalizedRemaining);
            impactImpulseRemainingSeconds = Mathf.Max(0f, impactImpulseRemainingSeconds - Mathf.Max(0f, deltaTime));

            if (impactImpulseRemainingSeconds <= 0f)
            {
                ResetImpactImpulse();
            }

            return offset;
        }

        private void ResetImpactImpulse()
        {
            impactImpulseDirection = Vector3.zero;
            impactImpulseDistance = 0f;
            impactImpulseDurationSeconds = 0f;
            impactImpulseRemainingSeconds = 0f;
            impactImpulsePriority = 0;
        }

        private void UpdateOwnerRendererVisibility(Vector3 playerLookPoint, Vector3 adjustedPosition)
        {
            if (ownerRenderers.Count == 0)
            {
                RestoreOwnerRenderers();
                return;
            }

            float hideDistance = Mathf.Max(0f, ownerRendererHideDistance);
            float restoreDistance = hideDistance + Mathf.Max(0f, ownerRendererRestorePadding);
            float currentDistance = Vector3.Distance(transform.position, playerLookPoint);
            float resolvedDistance = Vector3.Distance(adjustedPosition, playerLookPoint);
            float effectiveDistance = isObstacleAdjustmentActive
                ? Mathf.Min(currentDistance, resolvedDistance)
                : ownerRenderersHidden
                    ? Mathf.Max(currentDistance, resolvedDistance)
                    : currentDistance;
            bool shouldHide = ownerRenderersHidden
                ? effectiveDistance <= restoreDistance
                : effectiveDistance <= hideDistance;

            if (shouldHide)
            {
                HideOwnerRenderers();
            }
            else
            {
                RestoreOwnerRenderers();
            }
        }

        private void EnsureOwnerRendererCache()
        {
            if (followTarget == null)
            {
                RestoreOwnerRenderers();
                ClearOwnerRendererCache();
                return;
            }

            Transform importedVisualRoot = followTarget.Find(ImportedVisualRootName);
            Transform importedWeaponVisualRoot = followTarget.Find(ImportedWeaponVisualRootName);
            Transform proxyVisualRoot = followTarget.Find(CombatProxyVisualRootName);
            bool cacheChanged = cachedOwnerRoot != followTarget
                || cachedImportedVisualRoot != importedVisualRoot
                || cachedImportedWeaponVisualRoot != importedWeaponVisualRoot
                || cachedProxyVisualRoot != proxyVisualRoot
                || HasMissingOwnerRenderer();

            if (!cacheChanged)
            {
                return;
            }

            RestoreOwnerRenderers();
            ClearOwnerRendererCache();
            cachedOwnerRoot = followTarget;
            cachedImportedVisualRoot = importedVisualRoot;
            cachedImportedWeaponVisualRoot = importedWeaponVisualRoot;
            cachedProxyVisualRoot = proxyVisualRoot;
            CollectOwnerRenderers(importedVisualRoot);
            CollectOwnerRenderers(importedWeaponVisualRoot);
            CollectOwnerRenderers(proxyVisualRoot);

            if (ownerRenderers.Count == 0)
            {
                CollectOwnerRenderers(followTarget);
            }
        }

        private void CollectOwnerRenderers(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null || ownerRenderers.Contains(renderer))
                {
                    continue;
                }

                ownerRenderers.Add(renderer);
                ownerRendererForceRenderingOffStates.Add(renderer.forceRenderingOff);
            }
        }

        private void HideOwnerRenderers()
        {
            if (ownerRenderersHidden)
            {
                return;
            }

            for (int i = 0; i < ownerRenderers.Count; i++)
            {
                Renderer renderer = ownerRenderers[i];

                if (renderer == null)
                {
                    continue;
                }

                ownerRendererForceRenderingOffStates[i] = renderer.forceRenderingOff;
                renderer.forceRenderingOff = true;
            }

            ownerRenderersHidden = true;
        }

        private void RestoreOwnerRenderers()
        {
            if (!ownerRenderersHidden)
            {
                return;
            }

            for (int i = 0; i < ownerRenderers.Count; i++)
            {
                Renderer renderer = ownerRenderers[i];

                if (renderer == null)
                {
                    continue;
                }

                if (renderer.forceRenderingOff)
                {
                    renderer.forceRenderingOff = ownerRendererForceRenderingOffStates[i];
                }
            }

            ownerRenderersHidden = false;
        }

        private void ClearOwnerRendererCache()
        {
            ownerRenderers.Clear();
            ownerRendererForceRenderingOffStates.Clear();
            cachedOwnerRoot = null;
            cachedImportedVisualRoot = null;
            cachedImportedWeaponVisualRoot = null;
            cachedProxyVisualRoot = null;
        }

        private bool HasMissingOwnerRenderer()
        {
            for (int i = 0; i < ownerRenderers.Count; i++)
            {
                if (ownerRenderers[i] == null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
