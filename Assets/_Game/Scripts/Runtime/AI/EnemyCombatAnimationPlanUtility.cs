using UnityEngine;

namespace CampusRPG.AI
{
    public readonly struct EnemyCombatAnimationPlan
    {
        public EnemyCombatAnimationPlan(string stateName, float groundSpeedNormalized)
        {
            StateName = stateName;
            GroundSpeedNormalized = Mathf.Clamp01(groundSpeedNormalized);
        }

        public string StateName { get; }

        public float GroundSpeedNormalized { get; }
    }

    public static class EnemyCombatAnimationPlanUtility
    {
        public const string GroundSpeedParameterName = "GroundSpeed";
        public const string LocomotionStateName = "Locomotion";
        public const string HitStateName = "Hit";
        public const string DeathStateName = "Death";
        public const string MeleeAttackStateName = "Attack_Melee";
        public const string MobileAttackStateName = "Attack_Mobile";
        public const string RangedAttackStateName = "Attack_Ranged";

        public static EnemyCombatAnimationPlan ResolvePlan(
            EnemyArchetypeType archetypeType,
            string currentStateName,
            float moveSpeedNormalized)
        {
            string stateName = ResolveStateName(archetypeType, currentStateName);
            float groundSpeed = string.Equals(stateName, LocomotionStateName, System.StringComparison.Ordinal)
                ? Mathf.Clamp01(moveSpeedNormalized)
                : 0f;
            return new EnemyCombatAnimationPlan(stateName, groundSpeed);
        }

        public static string ResolveStateName(EnemyArchetypeType archetypeType, string currentStateName)
        {
            if (string.Equals(currentStateName, nameof(EnemyDeathState), System.StringComparison.Ordinal))
            {
                return DeathStateName;
            }

            if (string.Equals(currentStateName, nameof(EnemyHitState), System.StringComparison.Ordinal))
            {
                return HitStateName;
            }

            if (string.Equals(currentStateName, nameof(EnemyAttackState), System.StringComparison.Ordinal))
            {
                switch (archetypeType)
                {
                    case EnemyArchetypeType.Mobile:
                        return MobileAttackStateName;
                    case EnemyArchetypeType.Ranged:
                        return RangedAttackStateName;
                    default:
                        return MeleeAttackStateName;
                }
            }

            return LocomotionStateName;
        }
    }
}
