using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public readonly struct EnemyProjectileLaunchPlan
    {
        public EnemyProjectileLaunchPlan(
            GameObject projectilePrefab,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            Vector3 direction,
            float damage,
            float projectileSpeed,
            float radius,
            float lifetimeSeconds,
            ProjectileTrajectoryMode trajectoryMode,
            float arcHeight)
        {
            ProjectilePrefab = projectilePrefab;
            SpawnPosition = spawnPosition;
            SpawnRotation = spawnRotation;
            Direction = direction;
            Damage = damage;
            ProjectileSpeed = projectileSpeed;
            Radius = radius;
            LifetimeSeconds = lifetimeSeconds;
            TrajectoryMode = trajectoryMode;
            ArcHeight = arcHeight;
        }

        public GameObject ProjectilePrefab { get; }

        public Vector3 SpawnPosition { get; }

        public Quaternion SpawnRotation { get; }

        public Vector3 Direction { get; }

        public float Damage { get; }

        public float ProjectileSpeed { get; }

        public float Radius { get; }

        public float LifetimeSeconds { get; }

        public ProjectileTrajectoryMode TrajectoryMode { get; }

        public float ArcHeight { get; }
    }

    public static class EnemyAttackExecutionUtility
    {
        public static float ResolveDamage(float baseAttack, AttackDefinitionSO attack)
        {
            return attack != null ? baseAttack * attack.DamageMultiplier : baseAttack;
        }

        public static bool TryResolveAttackTarget(
            Transform target,
            Transform origin,
            EnemyArchetypeSO archetype,
            AttackDefinitionSO attack,
            float rangePadding,
            float maxHitAngle,
            bool canAttack,
            bool hasClearShot,
            out IDamageable damageable)
        {
            damageable = null;

            if (target == null || archetype == null || origin == null || !canAttack)
            {
                return false;
            }

            Vector3 flatDirection = target.position - origin.position;
            flatDirection.y = 0f;
            float maxRange = EnemyAttackSelectionResolver.ResolveAttackRange(archetype, attack) + Mathf.Max(0f, rangePadding);

            if (flatDirection.sqrMagnitude > maxRange * maxRange)
            {
                return false;
            }

            if (flatDirection.sqrMagnitude > Mathf.Epsilon)
            {
                float angle = Vector3.Angle(origin.forward, flatDirection.normalized);

                if (angle > maxHitAngle)
                {
                    return false;
                }
            }

            damageable = target.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                return false;
            }

            if (attack != null && attack.ProjectilePrefab != null && !hasClearShot)
            {
                damageable = null;
                return false;
            }

            return true;
        }

        public static bool TryBuildProjectileLaunchPlan(
            Transform target,
            Transform origin,
            AttackDefinitionSO attack,
            float damage,
            out EnemyProjectileLaunchPlan plan)
        {
            plan = default;

            if (target == null || attack == null || attack.ProjectilePrefab == null || origin == null)
            {
                return false;
            }

            Vector3 direction = target.position - origin.position;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = origin.forward;
            }

            direction.Normalize();

            plan = new EnemyProjectileLaunchPlan(
                attack.ProjectilePrefab,
                origin.position + direction * Mathf.Max(0f, attack.ProjectileSpawnOffset),
                Quaternion.LookRotation(direction, Vector3.up),
                direction,
                damage,
                attack.ProjectileSpeed,
                attack.Radius,
                attack.ProjectileLifetimeSeconds,
                attack.ProjectileTrajectoryMode,
                attack.ProjectileArcHeight);
            return true;
        }

        public static bool TryLaunchProjectile(GameObject source, EnemyProjectileLaunchPlan plan)
        {
            if (plan.ProjectilePrefab == null)
            {
                return false;
            }

            GameObject projectileObject = Object.Instantiate(plan.ProjectilePrefab, plan.SpawnPosition, plan.SpawnRotation);
            ProjectileController projectile = projectileObject.GetComponent<ProjectileController>();

            if (projectile == null)
            {
                Object.Destroy(projectileObject);
                return false;
            }

            projectile.Launch(
                source,
                plan.Direction,
                plan.Damage,
                plan.ProjectileSpeed,
                plan.Radius,
                plan.LifetimeSeconds,
                plan.TrajectoryMode,
                plan.ArcHeight);
            return true;
        }
    }
}
