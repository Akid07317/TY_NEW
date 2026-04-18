using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.AI
{
    public static class EnemyAttackLineOfSight
    {
        public static bool HasClearShot(Transform ownerRoot, Transform attackOrigin, Transform target)
        {
            if (attackOrigin == null || target == null)
            {
                return false;
            }

            Vector3 origin = ResolveAimPoint(attackOrigin);
            Transform targetRoot = target.root;
            Vector3 targetPoint = ResolveAimPoint(target);
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                toTarget / distance,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                return true;
            }

            global::System.Array.Sort(hits, CompareHitDistance);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;

                if (collider == null)
                {
                    continue;
                }

                Transform hitRoot = collider.transform.root;

                if (ownerRoot != null && hitRoot == ownerRoot)
                {
                    continue;
                }

                if (ShouldIgnoreClearShotHit(hitRoot, targetRoot))
                {
                    continue;
                }

                return hitRoot == targetRoot;
            }

            return true;
        }

        private static int CompareHitDistance(RaycastHit left, RaycastHit right)
        {
            return left.distance.CompareTo(right.distance);
        }

        private static Vector3 ResolveAimPoint(Transform target)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            Collider collider = target.GetComponentInChildren<Collider>();

            if (collider != null)
            {
                return collider.bounds.center;
            }

            return target.position;
        }

        private static bool ShouldIgnoreClearShotHit(Transform hitRoot, Transform targetRoot)
        {
            if (hitRoot == null || targetRoot == null || hitRoot == targetRoot)
            {
                return false;
            }

            EnemyBrain hitEnemy = hitRoot.GetComponentInParent<EnemyBrain>();
            PlayerCharacter targetPlayer = targetRoot.GetComponentInParent<PlayerCharacter>();
            return hitEnemy != null && targetPlayer != null;
        }
    }
}
