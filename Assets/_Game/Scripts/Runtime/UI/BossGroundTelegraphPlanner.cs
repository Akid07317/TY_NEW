using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossGroundTelegraphPlan
    {
        public BossGroundTelegraphPlan(
            BossGroundTelegraphPresenter.TelegraphShape shape,
            float radius,
            float length,
            Vector3 direction,
            Vector3 position)
        {
            Shape = shape;
            Radius = radius;
            Length = length;
            Direction = direction;
            Position = position;
        }

        public BossGroundTelegraphPresenter.TelegraphShape Shape { get; }

        public float Radius { get; }

        public float Length { get; }

        public Vector3 Direction { get; }

        public Vector3 Position { get; }
    }

    public static class BossGroundTelegraphPlanner
    {
        public static BossGroundTelegraphPlan Build(
            EnemyBrain bossEnemy,
            Transform fallbackTransform,
            float groundOffset,
            BossGroundTelegraphPresenter.TelegraphMode mode)
        {
            AttackDefinitionSO attack = mode == BossGroundTelegraphPresenter.TelegraphMode.Attack
                ? BossAttackPreviewUtility.PreviewCurrentAttack(bossEnemy)
                : null;
            BossGroundTelegraphPresenter.TelegraphShape shape = ResolveShape(mode, attack);
            float radius = ResolveRadius(bossEnemy, mode, attack, shape);
            float length = shape == BossGroundTelegraphPresenter.TelegraphShape.AttackLane
                ? ResolveAttackDistance(bossEnemy, attack)
                : radius * 2f;
            Vector3 direction = ResolveDirection(bossEnemy, fallbackTransform, shape);
            Vector3 position = ResolvePosition(bossEnemy, fallbackTransform, groundOffset, shape, direction, length);

            return new BossGroundTelegraphPlan(shape, radius, length, direction, position);
        }

        public static Vector3 ResolveDirection(
            EnemyBrain bossEnemy,
            Transform fallbackTransform,
            BossGroundTelegraphPresenter.TelegraphShape shape)
        {
            if (shape != BossGroundTelegraphPresenter.TelegraphShape.AttackLane)
            {
                return Vector3.forward;
            }

            return BossPresentationRules.ResolveFlatDirection(
                bossEnemy,
                fallbackTransform != null ? fallbackTransform.forward : Vector3.forward);
        }

        public static Vector3 ResolvePosition(
            EnemyBrain bossEnemy,
            Transform fallbackTransform,
            float groundOffset,
            BossGroundTelegraphPresenter.TelegraphShape shape,
            Vector3 direction,
            float laneLength)
        {
            Vector3 origin = bossEnemy != null
                ? bossEnemy.transform.position
                : fallbackTransform != null
                    ? fallbackTransform.position
                    : Vector3.zero;
            Vector3 position = origin;

            if (shape == BossGroundTelegraphPresenter.TelegraphShape.AttackLane)
            {
                position += direction * (laneLength * 0.5f);
            }

            position.y = origin.y + groundOffset;
            return position;
        }

        private static float ResolveRadius(
            EnemyBrain bossEnemy,
            BossGroundTelegraphPresenter.TelegraphMode mode,
            AttackDefinitionSO attack,
            BossGroundTelegraphPresenter.TelegraphShape shape)
        {
            if (shape == BossGroundTelegraphPresenter.TelegraphShape.AttackLane)
            {
                return attack != null ? Mathf.Max(0.3f, attack.Radius) : 0.35f;
            }

            if (attack != null)
            {
                return Mathf.Max(1.5f, ResolveAttackCoverageRange(bossEnemy, attack));
            }

            if (bossEnemy == null || bossEnemy.Archetype == null)
            {
                return 1.5f;
            }

            if (mode != BossGroundTelegraphPresenter.TelegraphMode.Attack && bossEnemy.AttackController != null)
            {
                return Mathf.Max(1.5f, bossEnemy.AttackController.GetAttackRangeForTarget(bossEnemy.CurrentTarget, bossEnemy.Archetype));
            }

            return Mathf.Max(1.5f, bossEnemy.Archetype.AttackDistance);
        }

        private static float ResolveAttackDistance(EnemyBrain bossEnemy, AttackDefinitionSO attack)
        {
            float attackDistance = bossEnemy != null && bossEnemy.Archetype != null
                ? bossEnemy.Archetype.AttackDistance
                : 1.5f;

            if (attack != null)
            {
                attackDistance = Mathf.Max(attackDistance, attack.Range);
            }

            return attackDistance;
        }

        private static float ResolveAttackCoverageRange(EnemyBrain bossEnemy, AttackDefinitionSO attack)
        {
            float attackCoverageRange = bossEnemy != null && bossEnemy.Archetype != null
                ? bossEnemy.Archetype.AttackDistance
                : 1.5f;

            if (attack != null)
            {
                attackCoverageRange = Mathf.Max(attackCoverageRange, attack.Range + attack.Radius);
            }

            return attackCoverageRange;
        }

        private static BossGroundTelegraphPresenter.TelegraphShape ResolveShape(
            BossGroundTelegraphPresenter.TelegraphMode mode,
            AttackDefinitionSO attack)
        {
            if (mode != BossGroundTelegraphPresenter.TelegraphMode.Attack || attack == null)
            {
                return BossGroundTelegraphPresenter.TelegraphShape.GroundCircle;
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.ChaseRoll)
            {
                return BossGroundTelegraphPresenter.TelegraphShape.AttackLane;
            }

            return attack.ProjectilePrefab != null && attack.ProjectileTrajectoryMode == ProjectileTrajectoryMode.Straight
                ? BossGroundTelegraphPresenter.TelegraphShape.AttackLane
                : BossGroundTelegraphPresenter.TelegraphShape.GroundCircle;
        }
    }
}
