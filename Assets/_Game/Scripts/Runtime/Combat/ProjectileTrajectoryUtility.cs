using UnityEngine;

namespace CampusRPG.Combat
{
    public static class ProjectileTrajectoryUtility
    {
        public static Vector3 EvaluatePosition(
            Vector3 launchPosition,
            Vector3 direction,
            ProjectileTrajectoryMode trajectoryMode,
            float arcHeight,
            float launchLifetimeSeconds,
            float totalTravelDistance,
            float totalElapsedSeconds)
        {
            Vector3 position = launchPosition + direction * Mathf.Max(0f, totalTravelDistance);

            if (trajectoryMode == ProjectileTrajectoryMode.Arc && arcHeight > 0f && launchLifetimeSeconds > Mathf.Epsilon)
            {
                float normalizedLifetime = Mathf.Clamp01(totalElapsedSeconds / launchLifetimeSeconds);
                position += Vector3.up * Mathf.Sin(normalizedLifetime * Mathf.PI) * arcHeight;
            }

            return position;
        }

        public static void UpdateOrientation(Transform targetTransform, Vector3 facingDirection)
        {
            if (targetTransform == null || facingDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            targetTransform.rotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
        }
    }
}
