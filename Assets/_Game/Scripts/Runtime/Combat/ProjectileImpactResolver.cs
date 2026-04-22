using CampusRPG.AI;
using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Combat
{
    internal enum ProjectileOwnerType
    {
        Unknown,
        Player,
        Enemy
    }

    internal readonly struct ProjectileHitResult
    {
        public ProjectileHitResult(IDamageable damageable, Vector3 hitPoint, bool hitBlocker)
        {
            Damageable = damageable;
            HitPoint = hitPoint;
            HitBlocker = hitBlocker;
        }

        public IDamageable Damageable { get; }

        public Vector3 HitPoint { get; }

        public bool HitBlocker { get; }
    }

    internal static class ProjectileImpactResolver
    {
        public static ProjectileOwnerType ResolveOwnerType(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return ProjectileOwnerType.Unknown;
            }

            if (sourceObject.GetComponentInParent<PlayerCharacter>() != null)
            {
                return ProjectileOwnerType.Player;
            }

            if (sourceObject.GetComponentInParent<EnemyBrain>() != null)
            {
                return ProjectileOwnerType.Enemy;
            }

            return ProjectileOwnerType.Unknown;
        }

        public static bool TryHitOnPath(
            Vector3 origin,
            Vector3 travelVector,
            float hitRadius,
            Transform sourceRoot,
            ProjectileOwnerType ownerType,
            out ProjectileHitResult result)
        {
            result = default;
            float travelDistance = travelVector.magnitude;

            if (travelDistance <= Mathf.Epsilon)
            {
                return false;
            }

            Vector3 castDirection = travelVector / travelDistance;

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                hitRadius,
                castDirection,
                travelDistance,
                ~0,
                QueryTriggerInteraction.Collide);

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            float closestDistance = float.MaxValue;
            IDamageable closestDamageable = null;
            bool closestWasBlocker = false;
            Vector3 closestHitPoint = origin;

            for (int i = 0; i < hits.Length; i++)
            {
                if (!TryResolveDamageable(hits[i].collider, sourceRoot, ownerType, out IDamageable candidate))
                {
                    if (!ShouldBlockOnCollider(hits[i].collider, origin, hitRadius, sourceRoot))
                    {
                        continue;
                    }

                    if (hits[i].distance < closestDistance)
                    {
                        closestDistance = hits[i].distance;
                        closestDamageable = null;
                        closestWasBlocker = true;
                        closestHitPoint = hits[i].point;
                    }

                    continue;
                }

                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    closestDamageable = candidate;
                    closestWasBlocker = false;
                    closestHitPoint = hits[i].point;
                }
            }

            if (closestDamageable == null && !closestWasBlocker)
            {
                return false;
            }

            result = new ProjectileHitResult(closestDamageable, closestHitPoint, closestWasBlocker);
            return true;
        }

        public static bool TryHitAtPosition(
            Vector3 position,
            float hitRadius,
            Transform sourceRoot,
            ProjectileOwnerType ownerType,
            out IDamageable damageable,
            out Vector3 hitPoint)
        {
            damageable = null;
            hitPoint = position;
            Collider[] colliders = Physics.OverlapSphere(position, hitRadius, ~0, QueryTriggerInteraction.Collide);

            if (colliders == null || colliders.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (!TryResolveDamageable(colliders[i], sourceRoot, ownerType, out IDamageable candidate))
                {
                    continue;
                }

                damageable = candidate;
                return true;
            }

            return false;
        }

        private static bool TryResolveDamageable(
            Collider collider,
            Transform sourceRoot,
            ProjectileOwnerType ownerType,
            out IDamageable damageable)
        {
            damageable = null;

            if (collider == null)
            {
                return false;
            }

            Component damageableComponent = collider.GetComponentInParent(typeof(IDamageable)) as Component;

            if (damageableComponent == null)
            {
                return false;
            }

            Transform targetRoot = damageableComponent.transform.root;

            if (sourceRoot != null && targetRoot == sourceRoot)
            {
                return false;
            }

            if (!IsLegalTarget(damageableComponent.gameObject, ownerType))
            {
                return false;
            }

            damageable = damageableComponent as IDamageable;
            return damageable != null;
        }

        private static bool IsLegalTarget(GameObject targetObject, ProjectileOwnerType ownerType)
        {
            if (targetObject == null)
            {
                return false;
            }

            return ownerType switch
            {
                ProjectileOwnerType.Player => targetObject.GetComponentInParent<EnemyBrain>() != null,
                ProjectileOwnerType.Enemy => targetObject.GetComponentInParent<PlayerCharacter>() != null,
                _ => true
            };
        }

        private static bool ShouldBlockOnCollider(
            Collider collider,
            Vector3 origin,
            float hitRadius,
            Transform sourceRoot)
        {
            if (collider == null || collider.isTrigger)
            {
                return false;
            }

            Transform colliderRoot = collider.transform.root;

            if (sourceRoot != null && colliderRoot == sourceRoot)
            {
                return false;
            }

            Component damageableComponent = collider.GetComponentInParent(typeof(IDamageable)) as Component;

            if (damageableComponent != null)
            {
                return false;
            }

            Vector3 closestPoint = ResolveHitPoint(collider, origin);
            float overlapThreshold = hitRadius + 0.01f;
            return (closestPoint - origin).sqrMagnitude > overlapThreshold * overlapThreshold;
        }

        private static Vector3 ResolveHitPoint(Collider collider, Vector3 fallbackPoint)
        {
            if (collider == null)
            {
                return fallbackPoint;
            }

            if (CanSafelyUseClosestPoint(collider))
            {
                return collider.ClosestPoint(fallbackPoint);
            }

            return collider.bounds.ClosestPoint(fallbackPoint);
        }

        private static bool CanSafelyUseClosestPoint(Collider collider)
        {
            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
            {
                return true;
            }

            return collider is MeshCollider meshCollider && meshCollider.convex;
        }
    }
}
