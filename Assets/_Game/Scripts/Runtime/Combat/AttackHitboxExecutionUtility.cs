using UnityEngine;

namespace CampusRPG.Combat
{
    public readonly struct AttackHitboxExecutionPlan
    {
        public AttackHitboxExecutionPlan(
            AttackHitboxShape shape,
            Vector3 center,
            float radius,
            Vector3 halfExtents,
            Quaternion rotation,
            float damage)
        {
            Shape = shape;
            Center = center;
            Radius = radius;
            HalfExtents = halfExtents;
            Rotation = rotation;
            Damage = damage;
        }

        public AttackHitboxShape Shape { get; }

        public Vector3 Center { get; }

        public float Radius { get; }

        public Vector3 HalfExtents { get; }

        public Quaternion Rotation { get; }

        public float Damage { get; }
    }

    public static class AttackHitboxExecutionUtility
    {
        public static bool TryBuildExecutionPlan(
            AttackDefinitionSO definition,
            Transform attackOrigin,
            float attackPower,
            out AttackHitboxExecutionPlan plan)
        {
            plan = default;

            if (definition == null || attackOrigin == null)
            {
                return false;
            }

            float damage = attackPower * definition.DamageMultiplier;
            return definition.HitboxShape switch
            {
                AttackHitboxShape.Sphere => TryBuildConfiguredSpherePlan(definition, attackOrigin, damage, out plan),
                AttackHitboxShape.Box => TryBuildConfiguredBoxPlan(definition, attackOrigin, damage, out plan),
                _ => TryBuildLegacyForwardSpherePlan(definition, attackOrigin, damage, out plan)
            };
        }

        public static bool TryResolveDamageable(Collider collider, GameObject source, out IDamageable damageable)
        {
            damageable = collider != null ? collider.GetComponentInParent<IDamageable>() : null;

            if (damageable == null)
            {
                return false;
            }

            if (source != null && collider.transform.root == source.transform.root)
            {
                damageable = null;
                return false;
            }

            return true;
        }

        private static bool TryBuildConfiguredSpherePlan(
            AttackDefinitionSO definition,
            Transform attackOrigin,
            float damage,
            out AttackHitboxExecutionPlan plan)
        {
            float radius = definition.HitboxRadius > 0f ? definition.HitboxRadius : definition.Radius;

            if (radius <= 0f)
            {
                return TryBuildLegacyForwardSpherePlan(definition, attackOrigin, damage, out plan);
            }

            plan = new AttackHitboxExecutionPlan(
                AttackHitboxShape.Sphere,
                attackOrigin.TransformPoint(definition.HitboxLocalCenter),
                radius,
                Vector3.zero,
                Quaternion.identity,
                damage);
            return true;
        }

        private static bool TryBuildConfiguredBoxPlan(
            AttackDefinitionSO definition,
            Transform attackOrigin,
            float damage,
            out AttackHitboxExecutionPlan plan)
        {
            Vector3 halfExtents = definition.HitboxHalfExtents;

            if (halfExtents.x <= 0f || halfExtents.y <= 0f || halfExtents.z <= 0f)
            {
                return TryBuildLegacyForwardSpherePlan(definition, attackOrigin, damage, out plan);
            }

            plan = new AttackHitboxExecutionPlan(
                AttackHitboxShape.Box,
                attackOrigin.TransformPoint(definition.HitboxLocalCenter),
                0f,
                halfExtents,
                attackOrigin.rotation,
                damage);
            return true;
        }

        private static bool TryBuildLegacyForwardSpherePlan(
            AttackDefinitionSO definition,
            Transform attackOrigin,
            float damage,
            out AttackHitboxExecutionPlan plan)
        {
            plan = new AttackHitboxExecutionPlan(
                AttackHitboxShape.Sphere,
                attackOrigin.position + attackOrigin.forward * definition.Range,
                definition.Radius,
                Vector3.zero,
                Quaternion.identity,
                damage);
            return plan.Radius > 0f;
        }
    }
}
