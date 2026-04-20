using UnityEngine;

namespace CampusRPG.Camera
{
    public static class CameraObstacleResolver
    {
        public static Vector3 ResolveAdjustedPosition(
            Vector3 origin,
            Vector3 desiredPosition,
            Transform ignoredRoot,
            float probeRadius,
            float padding,
            LayerMask obstacleMask)
        {
            Vector3 toDesired = desiredPosition - origin;
            float desiredDistance = toDesired.magnitude;

            if (desiredDistance <= Mathf.Epsilon)
            {
                return desiredPosition;
            }

            float safeProbeRadius = Mathf.Max(0.01f, probeRadius);
            float safePadding = Mathf.Max(0f, padding);
            Vector3 direction = toDesired / desiredDistance;
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

                if (ignoredRoot != null
                    && (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

                closestDistance = Mathf.Min(closestDistance, Mathf.Max(0f, hits[i].distance - safePadding));
            }

            return origin + direction * closestDistance;
        }
    }
}
