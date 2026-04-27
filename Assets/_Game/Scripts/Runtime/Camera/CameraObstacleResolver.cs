using CampusRPG.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.Camera
{
    public readonly struct CameraObstacleResolution
    {
        public CameraObstacleResolution(
            Vector3 position,
            float desiredDistance,
            float resolvedDistance,
            bool hasStaticObstruction,
            bool usedNarrowObstacleSidestep,
            bool overlapsDynamicActor)
        {
            Position = position;
            DesiredDistance = desiredDistance;
            ResolvedDistance = resolvedDistance;
            HasStaticObstruction = hasStaticObstruction;
            UsedNarrowObstacleSidestep = usedNarrowObstacleSidestep;
            OverlapsDynamicActor = overlapsDynamicActor;
        }

        public Vector3 Position { get; }

        public float DesiredDistance { get; }

        public float ResolvedDistance { get; }

        public bool HasStaticObstruction { get; }

        public bool UsedNarrowObstacleSidestep { get; }

        public bool OverlapsDynamicActor { get; }

        public float RetractionRatio => DesiredDistance <= Mathf.Epsilon
            ? 1f
            : Mathf.Clamp01(ResolvedDistance / DesiredDistance);
    }

    public static class CameraObstacleResolver
    {
        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private const float DistancePromotionEpsilon = 0.12f;
        private const float DesiredOffsetPromotionEpsilon = 0.18f;
        private const float StabilityPromotionEpsilon = 0.08f;
        private const float DistanceStabilityTolerance = 0.2f;
        private const float DesiredOffsetStabilityTolerance = 0.35f;
        private const float NarrowObstacleSevereRetractionRatio = 0.72f;
        private const float StaticOverlapSeparation = 0.01f;
        private const int StaticOverlapResolutionIterations = 4;
        private static SphereCollider penetrationProbe;

        public static Vector3 ResolveAdjustedPosition(
            Vector3 origin,
            Vector3 desiredPosition,
            Vector3 currentPosition,
            Transform ignoredRoot,
            float probeRadius,
            float padding,
            LayerMask obstacleMask)
        {
            return Resolve(
                origin,
                desiredPosition,
                currentPosition,
                ignoredRoot,
                probeRadius,
                padding,
                obstacleMask).Position;
        }

        public static CameraObstacleResolution Resolve(
            Vector3 origin,
            Vector3 desiredPosition,
            Vector3 currentPosition,
            Transform ignoredRoot,
            float probeRadius,
            float padding,
            LayerMask obstacleMask)
        {
            Vector3 toDesired = desiredPosition - origin;
            float desiredDistance = toDesired.magnitude;

            if (desiredDistance <= Mathf.Epsilon)
            {
                return new CameraObstacleResolution(desiredPosition, 0f, 0f, false, false, false);
            }

            float safeProbeRadius = Mathf.Max(0.01f, probeRadius);
            float safePadding = Mathf.Max(0f, padding);
            Vector3 direction = toDesired / desiredDistance;
            Vector3 bestPosition = ResolveAlongSegment(
                origin,
                desiredPosition,
                ignoredRoot,
                safeProbeRadius,
                safePadding,
                obstacleMask,
                out float bestDistance,
                out bool hasStaticObstruction);
            float bestDesiredOffset = Vector3.Distance(bestPosition, desiredPosition);
            float bestStabilityOffset = Vector3.Distance(bestPosition, currentPosition);
            bool bestOverlapsDynamicActor = OverlapsDynamicActor(
                bestPosition,
                ignoredRoot,
                safeProbeRadius * 1.1f,
                obstacleMask,
                includeIgnoredRootHierarchy: true);

            if (!hasStaticObstruction && !bestOverlapsDynamicActor)
            {
                return new CameraObstacleResolution(
                    bestPosition,
                    desiredDistance,
                    bestDistance,
                    false,
                    false,
                    false);
            }

            bool usedNarrowObstacleSidestep = false;

            Vector3 right = Vector3.Cross(Vector3.up, direction);

            if (right.sqrMagnitude <= Mathf.Epsilon)
            {
                right = Vector3.right;
            }
            else
            {
                right.Normalize();
            }

            Collider primaryObstacle = FindClosestStaticObstacle(
                origin,
                desiredPosition,
                ignoredRoot,
                safeProbeRadius,
                obstacleMask);
            float lateralOffset = Mathf.Max(safeProbeRadius * 6f, desiredDistance * 0.35f);
            bool isBroadObstacle = IsBroadObstacle(primaryObstacle, right, safeProbeRadius);
            bool shouldTrySidestep = !isBroadObstacle
                && (bestOverlapsDynamicActor || bestDistance <= desiredDistance * NarrowObstacleSevereRetractionRatio);

            if (shouldTrySidestep)
            {
                float currentSide = Vector3.Dot(currentPosition - desiredPosition, right);
                Vector3 firstSide = currentSide < -0.05f ? -right : right;
                Vector3 secondSide = -firstSide;

                TryPromoteCandidate(
                    origin,
                    desiredPosition,
                    desiredPosition + (firstSide * lateralOffset),
                    currentPosition,
                    ignoredRoot,
                    safeProbeRadius,
                    safePadding,
                    obstacleMask,
                    ref bestPosition,
                    ref bestDistance,
                    ref bestDesiredOffset,
                    ref bestStabilityOffset,
                    ref bestOverlapsDynamicActor,
                    ref usedNarrowObstacleSidestep,
                    candidateUsesSidestep: true);
                TryPromoteCandidate(
                    origin,
                    desiredPosition,
                    desiredPosition + (secondSide * lateralOffset),
                    currentPosition,
                    ignoredRoot,
                    safeProbeRadius,
                    safePadding,
                    obstacleMask,
                    ref bestPosition,
                    ref bestDistance,
                    ref bestDesiredOffset,
                    ref bestStabilityOffset,
                    ref bestOverlapsDynamicActor,
                    ref usedNarrowObstacleSidestep,
                    candidateUsesSidestep: true);
            }

            bestPosition = ResolveClearPositionAlongSegment(
                origin,
                bestPosition,
                ignoredRoot,
                safeProbeRadius * 1.1f,
                obstacleMask,
                out bestOverlapsDynamicActor);
            bestDistance = Vector3.Distance(origin, bestPosition);

            return new CameraObstacleResolution(
                bestPosition,
                desiredDistance,
                bestDistance,
                hasStaticObstruction,
                usedNarrowObstacleSidestep,
                bestOverlapsDynamicActor);
        }

        private static Collider FindClosestStaticObstacle(
            Vector3 origin,
            Vector3 desiredPosition,
            Transform ignoredRoot,
            float safeProbeRadius,
            LayerMask obstacleMask)
        {
            Vector3 toDesired = desiredPosition - origin;
            float desiredDistance = toDesired.magnitude;

            if (desiredDistance <= Mathf.Epsilon)
            {
                return null;
            }

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                safeProbeRadius,
                toDesired / desiredDistance,
                desiredDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.PositiveInfinity;
            Collider closestCollider = null;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;

                if (collider == null || ShouldIgnoreCollider(collider, collider.transform, ignoredRoot))
                {
                    continue;
                }

                if (hits[i].distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hits[i].distance;
                closestCollider = collider;
            }

            return closestCollider;
        }

        private static bool IsBroadObstacle(Collider collider, Vector3 right, float safeProbeRadius)
        {
            if (collider == null)
            {
                return false;
            }

            Bounds bounds = collider.bounds;
            float rightSpan = Mathf.Abs(right.x) * bounds.size.x + Mathf.Abs(right.z) * bounds.size.z;
            return rightSpan >= Mathf.Max(1.4f, safeProbeRadius * 5f);
        }

        public static bool IsPositionOccupied(
            Vector3 position,
            Transform ignoredRoot,
            float radius,
            LayerMask obstacleMask)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                position,
                Mathf.Max(0.01f, radius),
                OverlapBuffer,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = OverlapBuffer[i];

                if (collider == null)
                {
                    continue;
                }

                Transform hitTransform = collider.transform;

                if (ignoredRoot != null
                    && hitTransform != null
                    && (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public static bool IsSegmentObstructed(
            Vector3 from,
            Vector3 to,
            Transform ignoredRoot,
            float radius,
            LayerMask obstacleMask)
        {
            Vector3 offset = to - from;
            float distance = offset.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                return false;
            }

            RaycastHit[] hits = Physics.SphereCastAll(
                from,
                Mathf.Max(0.01f, radius),
                offset / distance,
                distance,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;

                if (collider == null || ShouldIgnoreCollider(collider, collider.transform, ignoredRoot))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public static bool IsSegmentOccupiedByDynamicActor(
            Vector3 from,
            Vector3 to,
            Transform ignoredRoot,
            float radius,
            LayerMask obstacleMask)
        {
            Vector3 offset = to - from;
            float distance = offset.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                return false;
            }

            RaycastHit[] hits = Physics.SphereCastAll(
                from,
                Mathf.Max(0.01f, radius),
                offset / distance,
                distance,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;

                if (IsDynamicActorCollider(collider, ignoredRoot, includeIgnoredRootHierarchy: false))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 ResolveAlongSegment(
            Vector3 origin,
            Vector3 desiredPosition,
            Transform ignoredRoot,
            float safeProbeRadius,
            float safePadding,
            LayerMask obstacleMask,
            out float resolvedDistance,
            out bool hasStaticObstruction)
        {
            Vector3 toDesired = desiredPosition - origin;
            float desiredDistance = toDesired.magnitude;

            if (desiredDistance <= Mathf.Epsilon)
            {
                resolvedDistance = 0f;
                hasStaticObstruction = false;
                return desiredPosition;
            }

            Vector3 direction = toDesired / desiredDistance;
            bool hasInitialStaticOverlap = OverlapsStaticObstacle(origin, ignoredRoot, safeProbeRadius, obstacleMask);
            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                safeProbeRadius,
                direction,
                desiredDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore);
            float closestDistance = desiredDistance;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;

                if (collider == null)
                {
                    continue;
                }

                Transform hitTransform = collider.transform;

                if (ShouldIgnoreCollider(collider, hitTransform, ignoredRoot))
                {
                    continue;
                }

                closestDistance = Mathf.Min(closestDistance, Mathf.Max(0f, hits[i].distance - safePadding));
            }

            Vector3 resolvedPosition = origin + direction * closestDistance;
            resolvedPosition = ResolveStaticOverlap(
                resolvedPosition,
                ignoredRoot,
                safeProbeRadius + safePadding,
                obstacleMask,
                out bool adjustedForStaticOverlap);
            resolvedDistance = Vector3.Distance(origin, resolvedPosition);
            hasStaticObstruction = hasInitialStaticOverlap
                || adjustedForStaticOverlap
                || closestDistance + 0.001f < desiredDistance;
            return resolvedPosition;
        }

        private static bool ShouldIgnoreCollider(Collider collider, Transform hitTransform, Transform ignoredRoot)
        {
            if (collider == null)
            {
                return true;
            }

            if (ignoredRoot != null
                && hitTransform != null
                && (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot)))
            {
                return true;
            }

            return collider.GetComponentInParent<CharacterController>() != null
                || collider.GetComponentInParent<NavMeshAgent>() != null;
        }

        private static void TryPromoteCandidate(
            Vector3 origin,
            Vector3 originalDesiredPosition,
            Vector3 candidateDesiredPosition,
            Vector3 currentPosition,
            Transform ignoredRoot,
            float safeProbeRadius,
            float safePadding,
            LayerMask obstacleMask,
            ref Vector3 bestPosition,
            ref float bestDistance,
            ref float bestDesiredOffset,
            ref float bestStabilityOffset,
            ref bool bestOverlapsDynamicActor,
            ref bool usedNarrowObstacleSidestep,
            bool candidateUsesSidestep)
        {
            Vector3 candidatePosition = ResolveAlongSegment(
                origin,
                candidateDesiredPosition,
                ignoredRoot,
                safeProbeRadius,
                safePadding,
                obstacleMask,
                out float candidateDistance,
                out bool candidateHasStaticObstruction);
            float candidateDesiredOffset = Vector3.Distance(candidatePosition, originalDesiredPosition);
            float candidateStabilityOffset = Vector3.Distance(candidatePosition, currentPosition);
            bool candidateOverlapsDynamicActor = OverlapsDynamicActor(
                candidatePosition,
                ignoredRoot,
                safeProbeRadius * 1.1f,
                obstacleMask,
                includeIgnoredRootHierarchy: true);

            if (bestOverlapsDynamicActor && !candidateOverlapsDynamicActor)
            {
                bestPosition = candidatePosition;
                bestDistance = candidateDistance;
                bestDesiredOffset = candidateDesiredOffset;
                bestStabilityOffset = candidateStabilityOffset;
                bestOverlapsDynamicActor = false;
                usedNarrowObstacleSidestep = candidateUsesSidestep;
                return;
            }

            if (!bestOverlapsDynamicActor && candidateOverlapsDynamicActor)
            {
                return;
            }

            if (candidateHasStaticObstruction && candidateDistance <= bestDistance + DistancePromotionEpsilon)
            {
                return;
            }

            bool improvesDistance = candidateDistance > bestDistance + DistancePromotionEpsilon
                && candidateDesiredOffset <= bestDesiredOffset + DesiredOffsetStabilityTolerance;
            bool improvesDesiredOffset = candidateDesiredOffset + DesiredOffsetPromotionEpsilon < bestDesiredOffset
                && candidateDistance >= bestDistance - DistanceStabilityTolerance;
            bool keepsComparableDistance = candidateDistance >= bestDistance - DistanceStabilityTolerance;
            bool keepsComparableDesiredOffset = candidateDesiredOffset <= bestDesiredOffset + DesiredOffsetStabilityTolerance;
            bool improvesStability = candidateStabilityOffset + StabilityPromotionEpsilon < bestStabilityOffset;

            if (!improvesDistance
                && !improvesDesiredOffset
                && !(keepsComparableDistance && keepsComparableDesiredOffset && improvesStability))
            {
                return;
            }

            bestPosition = candidatePosition;
            bestDistance = candidateDistance;
            bestDesiredOffset = candidateDesiredOffset;
            bestStabilityOffset = candidateStabilityOffset;
            bestOverlapsDynamicActor = candidateOverlapsDynamicActor;
            usedNarrowObstacleSidestep = candidateUsesSidestep;
        }

        private static Vector3 ResolveClearPositionAlongSegment(
            Vector3 origin,
            Vector3 blockedPosition,
            Transform ignoredRoot,
            float overlapRadius,
            LayerMask obstacleMask,
            out bool stillOverlapping)
        {
            stillOverlapping = OverlapsDynamicActor(
                blockedPosition,
                ignoredRoot,
                overlapRadius,
                obstacleMask,
                includeIgnoredRootHierarchy: true);

            if (!stillOverlapping)
            {
                return blockedPosition;
            }

            Vector3 offset = blockedPosition - origin;
            float blockedDistance = offset.magnitude;

            if (blockedDistance <= Mathf.Epsilon)
            {
                return blockedPosition;
            }

            Vector3 direction = offset / blockedDistance;
            Vector3 blockedSample = blockedPosition;
            int stepCount = Mathf.Max(6, Mathf.CeilToInt(blockedDistance / Mathf.Max(0.12f, overlapRadius * 0.8f)));

            for (int i = 1; i <= stepCount; i++)
            {
                float sampleDistance = blockedDistance * (1f - (float)i / stepCount);
                Vector3 samplePosition = origin + direction * sampleDistance;

                if (OverlapsDynamicActor(samplePosition, ignoredRoot, overlapRadius, obstacleMask, includeIgnoredRootHierarchy: true))
                {
                    blockedSample = samplePosition;
                    continue;
                }

                Vector3 clearSample = samplePosition;

                for (int refinement = 0; refinement < 6; refinement++)
                {
                    Vector3 midpoint = Vector3.Lerp(clearSample, blockedSample, 0.5f);

                    if (OverlapsDynamicActor(midpoint, ignoredRoot, overlapRadius, obstacleMask, includeIgnoredRootHierarchy: true))
                    {
                        blockedSample = midpoint;
                    }
                    else
                    {
                        clearSample = midpoint;
                    }
                }

                stillOverlapping = false;
                return clearSample;
            }

            return blockedPosition;
        }

        private static Vector3 ResolveStaticOverlap(
            Vector3 position,
            Transform ignoredRoot,
            float radius,
            LayerMask obstacleMask,
            out bool adjusted)
        {
            adjusted = false;
            float safeRadius = Mathf.Max(0.01f, radius);
            Vector3 resolvedPosition = position;
            SphereCollider probe = null;

            for (int iteration = 0; iteration < StaticOverlapResolutionIterations; iteration++)
            {
                int hitCount = Physics.OverlapSphereNonAlloc(
                    resolvedPosition,
                    safeRadius,
                    OverlapBuffer,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore);
                bool movedThisIteration = false;

                for (int i = 0; i < hitCount; i++)
                {
                    Collider collider = OverlapBuffer[i];

                    if (collider == null || ShouldIgnoreCollider(collider, collider.transform, ignoredRoot))
                    {
                        continue;
                    }

                    if (probe == null)
                    {
                        probe = GetPenetrationProbe(safeRadius);
                    }
                    probe.transform.position = resolvedPosition;
                    probe.enabled = true;

                    bool penetrates = Physics.ComputePenetration(
                        probe,
                        resolvedPosition,
                        Quaternion.identity,
                        collider,
                        collider.transform.position,
                        collider.transform.rotation,
                        out Vector3 direction,
                        out float distance);
                    probe.enabled = false;

                    if (!penetrates || distance <= Mathf.Epsilon || direction.sqrMagnitude <= Mathf.Epsilon)
                    {
                        continue;
                    }

                    resolvedPosition += direction.normalized * (distance + StaticOverlapSeparation);
                    adjusted = true;
                    movedThisIteration = true;
                }

                if (!movedThisIteration)
                {
                    break;
                }
            }

            if (probe != null)
            {
                probe.enabled = false;
            }

            return resolvedPosition;
        }

        private static bool OverlapsStaticObstacle(
            Vector3 position,
            Transform ignoredRoot,
            float radius,
            LayerMask obstacleMask)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                position,
                Mathf.Max(0.01f, radius),
                OverlapBuffer,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = OverlapBuffer[i];

                if (collider == null || ShouldIgnoreCollider(collider, collider.transform, ignoredRoot))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static SphereCollider GetPenetrationProbe(float radius)
        {
            if (penetrationProbe == null)
            {
                GameObject probeObject = new GameObject("CameraObstacleResolverPenetrationProbe");
                probeObject.hideFlags = HideFlags.HideAndDontSave;
                penetrationProbe = probeObject.AddComponent<SphereCollider>();
                penetrationProbe.isTrigger = true;
                penetrationProbe.enabled = false;
            }

            penetrationProbe.radius = Mathf.Max(0.01f, radius);
            penetrationProbe.center = Vector3.zero;
            penetrationProbe.transform.localScale = Vector3.one;
            return penetrationProbe;
        }

        private static bool OverlapsDynamicActor(
            Vector3 position,
            Transform ignoredRoot,
            float radius,
            LayerMask obstacleMask,
            bool includeIgnoredRootHierarchy = false)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                position,
                Mathf.Max(0.01f, radius),
                OverlapBuffer,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = OverlapBuffer[i];

                if (!IsDynamicActorCollider(collider, ignoredRoot, includeIgnoredRootHierarchy))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsDynamicActorCollider(Collider collider, Transform ignoredRoot, bool includeIgnoredRootHierarchy)
        {
            if (collider == null)
            {
                return false;
            }

            Transform hitTransform = collider.transform;

            if (!includeIgnoredRootHierarchy
                && ignoredRoot != null
                && hitTransform != null
                && (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot)))
            {
                return false;
            }

            HealthComponent health = collider.GetComponentInParent<HealthComponent>();

            if (health != null && health.IsDead)
            {
                return false;
            }

            return collider.GetComponentInParent<CharacterController>() != null
                || collider.GetComponentInParent<NavMeshAgent>() != null;
        }
    }
}
