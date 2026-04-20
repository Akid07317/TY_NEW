using UnityEngine;

namespace CampusRPG.Character
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementProbe : MonoBehaviour
    {
        [SerializeField] private Transform probeOrigin;
        [SerializeField] private LayerMask environmentMask = ~0;

        public bool TryFindMantleTarget(PlayerBaseStatsSO movementStats, Transform reference, out Vector3 mantleTarget)
        {
            mantleTarget = Vector3.zero;

            if (movementStats == null)
            {
                return false;
            }

            Transform resolvedReference = reference != null ? reference : transform;
            Vector3 origin = probeOrigin != null ? probeOrigin.position : transform.position + Vector3.up * 1.0f;
            Vector3 forward = Vector3.ProjectOnPlane(resolvedReference.forward, Vector3.up);

            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            forward.Normalize();

            if (!Physics.Raycast(
                    origin,
                    forward,
                    out RaycastHit wallHit,
                    movementStats.MantleForwardDistance,
                    environmentMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 topProbeOrigin = wallHit.point + forward * 0.2f;
            topProbeOrigin.y = resolvedReference.position.y + movementStats.MantleMaxHeight + 0.05f;

            if (!Physics.Raycast(
                    topProbeOrigin,
                    Vector3.down,
                    out RaycastHit topHit,
                    movementStats.MantleMaxHeight + 0.2f,
                    environmentMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float ledgeHeight = topHit.point.y - transform.position.y;

            if (ledgeHeight < movementStats.MantleMinHeight || ledgeHeight > movementStats.MantleMaxHeight)
            {
                return false;
            }

            mantleTarget = topHit.point + forward * 0.2f;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = probeOrigin != null ? probeOrigin.position : transform.position + Vector3.up * 1.0f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, 0.05f);
        }
    }
}
