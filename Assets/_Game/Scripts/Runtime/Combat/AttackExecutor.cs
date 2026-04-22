using System.Collections.Generic;
using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Combat
{
    public sealed class AttackExecutor : MonoBehaviour
    {
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.15f, 0.35f);

        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        private void Awake()
        {
            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }
        }

        public int Execute(AttackDefinitionSO definition, float attackPower, GameObject source)
        {
            if (!AttackHitboxExecutionUtility.TryBuildExecutionPlan(definition, attackOrigin, attackPower, out AttackHitboxExecutionPlan plan))
            {
                return 0;
            }

            return plan.Shape switch
            {
                AttackHitboxShape.Box => ExecuteBox(plan.Center, plan.HalfExtents, plan.Rotation, plan.Damage, source),
                _ => ExecuteSphere(plan.Center, plan.Radius, plan.Damage, source)
            };
        }

        public int ExecuteSphere(Vector3 sphereCenter, float radius, float damage, GameObject source)
        {
            if (radius <= 0f)
            {
                return 0;
            }

            hitTargets.Clear();

            Collider[] colliders = Physics.OverlapSphere(
                sphereCenter,
                radius,
                targetMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < colliders.Length; i++)
            {
                TryApplyDamage(colliders[i], attackOrigin.position, damage, source);
            }

            return hitTargets.Count;
        }

        public int ExecuteBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, float damage, GameObject source)
        {
            if (halfExtents.x <= 0f || halfExtents.y <= 0f || halfExtents.z <= 0f)
            {
                return 0;
            }

            hitTargets.Clear();

            Collider[] colliders = Physics.OverlapBox(
                center,
                halfExtents,
                rotation,
                targetMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < colliders.Length; i++)
            {
                TryApplyDamage(colliders[i], center, damage, source);
            }

            return hitTargets.Count;
        }

        private void TryApplyDamage(Collider collider, Vector3 hitReferencePoint, float damage, GameObject source)
        {
            if (!AttackHitboxExecutionUtility.TryResolveDamageable(collider, source, out IDamageable damageable))
            {
                return;
            }

            if (!hitTargets.Add(damageable))
            {
                return;
            }

            damageable.ReceiveDamage(damage, ResolveHitPoint(collider, hitReferencePoint), source);
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

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo)
            {
                return;
            }

            Transform origin = attackOrigin != null ? attackOrigin : transform;
            Vector3 center = origin.position + origin.forward * 2f;

            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(center, 0.5f);
        }
    }
}
