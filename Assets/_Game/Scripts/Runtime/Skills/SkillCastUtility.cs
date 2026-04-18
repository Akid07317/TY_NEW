using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Skills
{
    public readonly struct SkillProjectileCastPlan
    {
        public SkillProjectileCastPlan(
            GameObject projectilePrefab,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            Vector3 direction,
            float damage,
            float projectileSpeed,
            float impactRadius,
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
            ImpactRadius = impactRadius;
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

        public float ImpactRadius { get; }

        public float LifetimeSeconds { get; }

        public ProjectileTrajectoryMode TrajectoryMode { get; }

        public float ArcHeight { get; }
    }

    public static class SkillCastUtility
    {
        public static float ResolveDamage(float baseAttack, SkillDefinitionSO skillDefinition)
        {
            return skillDefinition != null ? baseAttack * skillDefinition.DamageMultiplier : baseAttack;
        }

        public static Vector3 ResolveAimDirection(
            SkillDefinitionSO skillDefinition,
            Transform ownerTransform,
            Transform cameraTransform,
            Transform lockedTarget)
        {
            if (skillDefinition != null
                && skillDefinition.TargetMode == SkillTargetMode.LockedTarget
                && lockedTarget != null
                && ownerTransform != null)
            {
                Vector3 toTarget = lockedTarget.position - ownerTransform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > Mathf.Epsilon)
                {
                    return toTarget.normalized;
                }
            }

            Transform referenceTransform = cameraTransform != null ? cameraTransform : ownerTransform;
            Vector3 aimDirection = referenceTransform != null ? referenceTransform.forward : Vector3.forward;
            aimDirection.y = 0f;

            if (aimDirection.sqrMagnitude <= Mathf.Epsilon && ownerTransform != null)
            {
                aimDirection = ownerTransform.forward;
                aimDirection.y = 0f;
            }

            return aimDirection.sqrMagnitude > Mathf.Epsilon
                ? aimDirection.normalized
                : Vector3.forward;
        }

        public static Vector3 ResolveImpactPoint(
            SkillDefinitionSO skillDefinition,
            Transform origin,
            Transform ownerTransform,
            Transform lockedTarget,
            Vector3 aimDirection)
        {
            if (skillDefinition == null)
            {
                return ownerTransform != null ? ownerTransform.position : Vector3.zero;
            }

            if (skillDefinition.TargetMode == SkillTargetMode.Self)
            {
                return ownerTransform != null ? ownerTransform.position : Vector3.zero;
            }

            if (skillDefinition.TargetMode == SkillTargetMode.LockedTarget && lockedTarget != null)
            {
                return lockedTarget.position;
            }

            Vector3 castOrigin = origin != null
                ? origin.position
                : ownerTransform != null
                    ? ownerTransform.position
                    : Vector3.zero;
            return castOrigin + aimDirection * skillDefinition.Range;
        }

        public static bool TryBuildProjectileLaunchPlan(
            Transform origin,
            SkillDefinitionSO skillDefinition,
            Vector3 aimDirection,
            float damage,
            out SkillProjectileCastPlan plan)
        {
            plan = default;

            if (origin == null || skillDefinition == null || skillDefinition.ProjectilePrefab == null)
            {
                return false;
            }

            Vector3 direction = aimDirection;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = origin.forward;
            }

            direction.Normalize();

            plan = new SkillProjectileCastPlan(
                skillDefinition.ProjectilePrefab,
                origin.position + direction * Mathf.Max(0f, skillDefinition.ProjectileSpawnOffset),
                Quaternion.LookRotation(direction, Vector3.up),
                direction,
                damage,
                skillDefinition.ProjectileSpeed,
                skillDefinition.ImpactRadius,
                skillDefinition.ProjectileLifetimeSeconds,
                skillDefinition.ProjectileTrajectoryMode,
                skillDefinition.ProjectileArcHeight);
            return true;
        }

        public static bool TryLaunchProjectile(GameObject source, SkillProjectileCastPlan plan)
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
                plan.ImpactRadius,
                plan.LifetimeSeconds,
                plan.TrajectoryMode,
                plan.ArcHeight);
            return true;
        }
    }
}
