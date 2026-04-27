using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public readonly struct EnemyCombatAnimationPlan
    {
        public EnemyCombatAnimationPlan(string stateName, float groundSpeedNormalized)
            : this(
                stateName,
                stateName,
                groundSpeedNormalized,
                EnemyTargetResponseType.None,
                0f,
                string.Empty)
        {
        }

        public EnemyCombatAnimationPlan(
            string stateName,
            string fallbackStateName,
            float groundSpeedNormalized,
            EnemyTargetResponseType targetResponse,
            float responseReadNormalized,
            string responseReadLabel)
        {
            StateName = stateName;
            FallbackStateName = string.IsNullOrEmpty(fallbackStateName) ? stateName : fallbackStateName;
            GroundSpeedNormalized = Mathf.Clamp01(groundSpeedNormalized);
            TargetResponse = targetResponse;
            ResponseReadNormalized = Mathf.Clamp01(responseReadNormalized);
            ResponseReadLabel = responseReadLabel ?? string.Empty;
        }

        public string StateName { get; }

        public string FallbackStateName { get; }

        public float GroundSpeedNormalized { get; }

        public EnemyTargetResponseType TargetResponse { get; }

        public float ResponseReadNormalized { get; }

        public string ResponseReadLabel { get; }
    }

    public static class EnemyCombatAnimationPlanUtility
    {
        private const float LocomotionDeadZone = 0.05f;
        private const float ChaseMinGroundSpeed = 0.36f;
        private const float ChaseMaxGroundSpeed = 1f;
        private const float StrafeMinGroundSpeed = 0.24f;
        private const float StrafeMaxGroundSpeed = 0.82f;

        public const string GroundSpeedParameterName = "GroundSpeed";
        public const string LocomotionStateName = "Locomotion";
        public const string HitStateName = "Hit";
        public const string DeathStateName = "Death";
        public const string MeleeAttackStateName = "Attack_Melee";
        public const string MobileAttackStateName = "Attack_Mobile";
        public const string RangedAttackStateName = "Attack_Ranged";
        public const string AntiAirAttackStateName = "Attack_AntiAir";
        public const string ChaseRollAttackStateName = "Attack_ChaseRoll";
        public const string GuardBreakAttackStateName = "Attack_GuardBreak";
        public const string ResponseReadParameterName = "ResponseRead";
        public const string AntiAirReadParameterName = "AntiAirRead";
        public const string ChaseRollReadParameterName = "ChaseRollRead";
        public const string GuardBreakReadParameterName = "GuardBreakRead";

        public static EnemyCombatAnimationPlan ResolvePlan(
            EnemyArchetypeType archetypeType,
            string currentStateName,
            float moveSpeedNormalized)
        {
            return ResolvePlan(archetypeType, currentStateName, moveSpeedNormalized, null);
        }

        public static EnemyCombatAnimationPlan ResolvePlan(
            EnemyArchetypeType archetypeType,
            string currentStateName,
            float moveSpeedNormalized,
            AttackDefinitionSO attackDefinition)
        {
            return ResolvePlan(
                archetypeType,
                currentStateName,
                moveSpeedNormalized,
                attackDefinition,
                EnemyAttackPresentationPhase.None,
                0f);
        }

        public static EnemyCombatAnimationPlan ResolvePlan(
            EnemyArchetypeType archetypeType,
            string currentStateName,
            float moveSpeedNormalized,
            AttackDefinitionSO attackDefinition,
            EnemyAttackPresentationPhase attackPhase,
            float attackProgress)
        {
            EnemyTargetResponseType targetResponse = ResolveTargetResponse(currentStateName, attackDefinition);
            string stateName = ResolveStateName(archetypeType, currentStateName, targetResponse);
            string fallbackStateName = ResolveFallbackStateName(archetypeType, currentStateName, targetResponse);
            float groundSpeed = ResolveGroundSpeedNormalized(currentStateName, stateName, moveSpeedNormalized);
            return new EnemyCombatAnimationPlan(
                stateName,
                fallbackStateName,
                groundSpeed,
                targetResponse,
                ResolveResponseReadNormalized(targetResponse, attackPhase, attackProgress),
                ResolveResponseReadLabel(targetResponse));
        }

        public static string ResolveStateName(EnemyArchetypeType archetypeType, string currentStateName)
        {
            return ResolveStateName(archetypeType, currentStateName, EnemyTargetResponseType.None);
        }

        public static string ResolveStateName(
            EnemyArchetypeType archetypeType,
            string currentStateName,
            EnemyTargetResponseType targetResponse)
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
                switch (targetResponse)
                {
                    case EnemyTargetResponseType.AntiAir:
                        return AntiAirAttackStateName;
                    case EnemyTargetResponseType.ChaseRoll:
                        return ChaseRollAttackStateName;
                    case EnemyTargetResponseType.GuardBreak:
                        return GuardBreakAttackStateName;
                }

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

        public static string ResolveFallbackStateName(
            EnemyArchetypeType archetypeType,
            string currentStateName,
            EnemyTargetResponseType targetResponse)
        {
            if (!string.Equals(currentStateName, nameof(EnemyAttackState), System.StringComparison.Ordinal))
            {
                return ResolveStateName(archetypeType, currentStateName);
            }

            switch (targetResponse)
            {
                case EnemyTargetResponseType.AntiAir:
                    return RangedAttackStateName;
                case EnemyTargetResponseType.ChaseRoll:
                    return MobileAttackStateName;
                case EnemyTargetResponseType.GuardBreak:
                    return MeleeAttackStateName;
                default:
                    return ResolveStateName(archetypeType, currentStateName);
            }
        }

        private static EnemyTargetResponseType ResolveTargetResponse(
            string currentStateName,
            AttackDefinitionSO attackDefinition)
        {
            if (!string.Equals(currentStateName, nameof(EnemyAttackState), System.StringComparison.Ordinal)
                || attackDefinition == null)
            {
                return EnemyTargetResponseType.None;
            }

            if (attackDefinition.EnemyTargetResponse != EnemyTargetResponseType.None)
            {
                return attackDefinition.EnemyTargetResponse;
            }

            return attackDefinition.BreaksGuard
                ? EnemyTargetResponseType.GuardBreak
                : EnemyTargetResponseType.None;
        }

        private static float ResolveResponseReadNormalized(
            EnemyTargetResponseType targetResponse,
            EnemyAttackPresentationPhase attackPhase,
            float attackProgress)
        {
            float peak;

            switch (targetResponse)
            {
                case EnemyTargetResponseType.AntiAir:
                    peak = 1f;
                    break;
                case EnemyTargetResponseType.ChaseRoll:
                    peak = 0.9f;
                    break;
                case EnemyTargetResponseType.GuardBreak:
                    peak = 0.95f;
                    break;
                default:
                    return 0f;
            }

            float progress = SmoothStep01(attackProgress);

            switch (attackPhase)
            {
                case EnemyAttackPresentationPhase.Startup:
                    return Mathf.Lerp(peak * 0.28f, peak, progress);
                case EnemyAttackPresentationPhase.Advance:
                    return peak;
                case EnemyAttackPresentationPhase.Recovery:
                    return Mathf.Lerp(peak, peak * 0.2f, progress);
                default:
                    return peak;
            }
        }

        private static string ResolveResponseReadLabel(EnemyTargetResponseType targetResponse)
        {
            switch (targetResponse)
            {
                case EnemyTargetResponseType.AntiAir:
                    return "Anti-Air Read";
                case EnemyTargetResponseType.ChaseRoll:
                    return "Roll Catch Read";
                case EnemyTargetResponseType.GuardBreak:
                    return "Guard Break Read";
                default:
                    return string.Empty;
            }
        }

        private static float ResolveGroundSpeedNormalized(
            string currentStateName,
            string resolvedStateName,
            float moveSpeedNormalized)
        {
            if (!string.Equals(resolvedStateName, LocomotionStateName, System.StringComparison.Ordinal))
            {
                return 0f;
            }

            float clampedMoveSpeed = Mathf.Clamp01(moveSpeedNormalized);

            if (clampedMoveSpeed <= LocomotionDeadZone)
            {
                return 0f;
            }

            if (string.Equals(currentStateName, nameof(EnemyChaseState), System.StringComparison.Ordinal))
            {
                return Mathf.Lerp(ChaseMinGroundSpeed, ChaseMaxGroundSpeed, clampedMoveSpeed);
            }

            if (string.Equals(currentStateName, nameof(EnemyStrafeState), System.StringComparison.Ordinal))
            {
                return Mathf.Lerp(StrafeMinGroundSpeed, StrafeMaxGroundSpeed, clampedMoveSpeed);
            }

            return clampedMoveSpeed;
        }

        private static float SmoothStep01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - (2f * t));
        }
    }
}
