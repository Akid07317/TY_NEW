using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Camera
{
    public static class LockOnTargetSearchUtility
    {
        public static Transform FindBestTarget(
            Collider[] colliders,
            Transform ownerTransform,
            Transform cameraTransform,
            float searchRadius,
            float maxAcquireAngle)
        {
            if (ownerTransform == null || colliders == null || colliders.Length == 0)
            {
                return null;
            }

            float normalizedSearchRadius = Mathf.Max(0f, searchRadius);
            Vector3 origin = ownerTransform.position;
            Vector3 referenceForward = ResolveReferenceForward(ownerTransform, cameraTransform);
            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                Transform candidate = ResolveTargetTransform(colliders[i], ownerTransform.root);

                if (!IsValidTarget(candidate, origin, normalizedSearchRadius))
                {
                    continue;
                }

                float score = EvaluateAcquireScore(origin, referenceForward, candidate, maxAcquireAngle);

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        public static bool IsValidTarget(Transform target, Vector3 origin, float searchRadius)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3 offset = target.position - origin;
            offset.y = 0f;

            if (offset.sqrMagnitude > searchRadius * searchRadius)
            {
                return false;
            }

            HealthComponent health = target.GetComponentInParent<HealthComponent>();
            return health == null || !health.IsDead;
        }

        private static Transform ResolveTargetTransform(Collider collider, Transform ownerRoot)
        {
            if (collider == null || collider.transform.root == ownerRoot)
            {
                return null;
            }

            LockOnTarget explicitTarget = collider.GetComponentInParent<LockOnTarget>();

            if (explicitTarget != null)
            {
                return explicitTarget.TargetTransform;
            }

            EnemyBrain enemyBrain = collider.GetComponentInParent<EnemyBrain>();
            return enemyBrain != null ? enemyBrain.transform : null;
        }

        private static float EvaluateAcquireScore(
            Vector3 origin,
            Vector3 referenceForward,
            Transform candidate,
            float maxAcquireAngle)
        {
            Vector3 toCandidate = candidate.position - origin;
            toCandidate.y = 0f;
            float distance = toCandidate.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                return float.MaxValue;
            }

            float angle = Vector3.Angle(referenceForward, toCandidate / distance);

            if (angle > maxAcquireAngle)
            {
                return float.MaxValue;
            }

            return angle * 2f + distance;
        }

        private static Vector3 ResolveReferenceForward(Transform ownerTransform, Transform cameraTransform)
        {
            Vector3 referenceForward = cameraTransform != null ? cameraTransform.forward : ownerTransform.forward;
            referenceForward.y = 0f;

            if (referenceForward.sqrMagnitude <= Mathf.Epsilon)
            {
                referenceForward = ownerTransform.forward;
                referenceForward.y = 0f;
            }

            if (referenceForward.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.forward;
            }

            return referenceForward.normalized;
        }
    }
}
