using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossImpactMarkerPlan
    {
        public BossImpactMarkerPlan(
            BossImpactMarkerPresenter.MarkerShape shape,
            float radius,
            float length,
            float lifetimeSeconds,
            Vector3 direction,
            Vector3 position)
        {
            Shape = shape;
            Radius = radius;
            Length = length;
            LifetimeSeconds = lifetimeSeconds;
            Direction = direction;
            Position = position;
        }

        public BossImpactMarkerPresenter.MarkerShape Shape { get; }

        public float Radius { get; }

        public float Length { get; }

        public float LifetimeSeconds { get; }

        public Vector3 Direction { get; }

        public Vector3 Position { get; }
    }

    public static class BossImpactMarkerPlanner
    {
        public static BossImpactMarkerPlan Build(
            EnemyBrain bossEnemy,
            float groundOffset,
            float minimumLifetimeSeconds)
        {
            AttackDefinitionSO attack = BossAttackPreviewUtility.PreviewCurrentAttack(bossEnemy);
            BossImpactMarkerPresenter.MarkerShape shape = ResolveShape(attack);
            Vector3 direction = ResolveDirection(bossEnemy);
            float radius = ResolveRadius(attack, shape);
            float length = shape == BossImpactMarkerPresenter.MarkerShape.AttackLane
                ? ResolveAttackDistance(bossEnemy, attack)
                : radius * 2f;
            Vector3 position = ResolvePosition(bossEnemy, groundOffset, attack, shape, direction, length);
            float lifetime = ResolveLifetime(minimumLifetimeSeconds, attack);

            return new BossImpactMarkerPlan(shape, radius, length, lifetime, direction, position);
        }

        public static Vector3 ResolveDirection(EnemyBrain bossEnemy)
        {
            return BossPresentationRules.ResolveFlatDirection(bossEnemy, Vector3.forward);
        }

        public static Vector3 ResolvePosition(
            EnemyBrain bossEnemy,
            float groundOffset,
            AttackDefinitionSO attack,
            BossImpactMarkerPresenter.MarkerShape shape,
            Vector3 direction,
            float laneLength)
        {
            Vector3 origin = bossEnemy != null ? bossEnemy.transform.position : Vector3.zero;
            Vector3 position = origin + direction * ResolveAttackDistance(bossEnemy, attack);

            if (shape == BossImpactMarkerPresenter.MarkerShape.AttackLane)
            {
                position = origin + direction * (laneLength * 0.5f);
            }

            position.y = origin.y + groundOffset;
            return position;
        }

        private static float ResolveRadius(AttackDefinitionSO attack, BossImpactMarkerPresenter.MarkerShape shape)
        {
            if (attack != null)
            {
                float minimumRadius = shape == BossImpactMarkerPresenter.MarkerShape.AttackLane ? 0.3f : 0.45f;
                return Mathf.Max(minimumRadius, attack.Radius);
            }

            return shape == BossImpactMarkerPresenter.MarkerShape.AttackLane ? 0.35f : 0.55f;
        }

        private static float ResolveLifetime(float minimumLifetimeSeconds, AttackDefinitionSO attack)
        {
            if (attack != null)
            {
                return Mathf.Max(minimumLifetimeSeconds, attack.StartupSeconds);
            }

            return Mathf.Max(0.1f, minimumLifetimeSeconds);
        }

        private static float ResolveAttackDistance(EnemyBrain bossEnemy, AttackDefinitionSO attack)
        {
            float attackDistance = bossEnemy != null && bossEnemy.Archetype != null ? bossEnemy.Archetype.AttackDistance : 1.5f;

            if (attack != null)
            {
                attackDistance = Mathf.Max(attackDistance, attack.Range);
            }

            return attackDistance;
        }

        private static BossImpactMarkerPresenter.MarkerShape ResolveShape(AttackDefinitionSO attack)
        {
            if (attack == null)
            {
                return BossImpactMarkerPresenter.MarkerShape.ImpactCircle;
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.ChaseRoll)
            {
                return BossImpactMarkerPresenter.MarkerShape.AttackLane;
            }

            return attack.ProjectilePrefab != null && attack.ProjectileTrajectoryMode == ProjectileTrajectoryMode.Straight
                ? BossImpactMarkerPresenter.MarkerShape.AttackLane
                : BossImpactMarkerPresenter.MarkerShape.ImpactCircle;
        }
    }
}
