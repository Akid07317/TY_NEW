using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Camera
{
    public readonly struct ActionCameraImpulsePlan
    {
        public ActionCameraImpulsePlan(float distance, float durationSeconds)
            : this(distance, durationSeconds, 0)
        {
        }

        public ActionCameraImpulsePlan(float distance, float durationSeconds, int priority)
        {
            Distance = Mathf.Max(0f, distance);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Priority = Mathf.Max(0, priority);
        }

        public float Distance { get; }

        public float DurationSeconds { get; }

        public int Priority { get; }

        public bool HasImpulse => Distance > 0f && DurationSeconds > 0f;

        public static ActionCameraImpulsePlan None => default;
    }

    public static class ActionCameraFeedbackUtility
    {
        public const int ImpulsePriorityMinor = 10;
        public const int ImpulsePrioritySwordArt = 20;
        public const int ImpulsePriorityHeavyRead = 30;
        public const int ImpulsePriorityGuardBreak = 40;

        private const string SwordArtPrefix = "SwordArt_";
        private const string SidewindCutState = "SwordArt_SidewindCut";
        private const string CrossStepState = "SwordArt_CrossStep";
        private const string RisingCleaveState = "SwordArt_RisingCleave";
        private const string IronGateBreakState = "SwordArt_IronGateBreak";
        private const string FallingStarState = "SwordArt_FallingStar";
        private const string MoonSeverState = "SwordArt_MoonSever";

        public static ActionCameraImpulsePlan ResolveEvasiveImpulse(PlayerEvasiveActionType actionType)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => new ActionCameraImpulsePlan(0.055f, 0.1f, ImpulsePriorityMinor),
                PlayerEvasiveActionType.AirDodge => new ActionCameraImpulsePlan(0.065f, 0.12f, ImpulsePriorityMinor),
                _ => ActionCameraImpulsePlan.None
            };
        }

        public static ActionCameraImpulsePlan ResolvePlayerAttackImpulse(AttackDefinitionSO attackDefinition)
        {
            if (attackDefinition == null)
            {
                return ActionCameraImpulsePlan.None;
            }

            return ResolveNormalizedAttackName(attackDefinition) switch
            {
                FallingStarState => new ActionCameraImpulsePlan(0.16f, 0.16f, ImpulsePriorityHeavyRead),
                IronGateBreakState => new ActionCameraImpulsePlan(0.12f, 0.12f, ImpulsePriorityGuardBreak),
                RisingCleaveState => new ActionCameraImpulsePlan(0.095f, 0.11f, ImpulsePrioritySwordArt),
                MoonSeverState => new ActionCameraImpulsePlan(0.085f, 0.1f, ImpulsePrioritySwordArt),
                CrossStepState => new ActionCameraImpulsePlan(0.075f, 0.09f, ImpulsePrioritySwordArt),
                SidewindCutState => new ActionCameraImpulsePlan(0.065f, 0.08f, ImpulsePrioritySwordArt),
                _ => ActionCameraImpulsePlan.None
            };
        }

        public static ActionCameraImpulsePlan ResolveGuardBreakImpulse(float distance, float durationSeconds)
        {
            return new ActionCameraImpulsePlan(distance, durationSeconds, ImpulsePriorityGuardBreak);
        }

        public static ActionCameraImpulsePlan ResolveEnemyResponseImpulse(AttackDefinitionSO attackDefinition)
        {
            if (attackDefinition == null)
            {
                return ActionCameraImpulsePlan.None;
            }

            if (attackDefinition.EnemyTargetResponse == EnemyTargetResponseType.AntiAir)
            {
                return new ActionCameraImpulsePlan(0.085f, 0.12f, ImpulsePrioritySwordArt);
            }

            if (attackDefinition.EnemyTargetResponse == EnemyTargetResponseType.ChaseRoll)
            {
                return new ActionCameraImpulsePlan(0.11f, 0.13f, ImpulsePriorityHeavyRead);
            }

            return attackDefinition.BreaksGuard
                ? ResolveGuardBreakImpulse(0.095f, 0.12f)
                : ActionCameraImpulsePlan.None;
        }

        public static bool TryRequestImpulse(
            ThirdPersonCameraController cameraController,
            Transform source,
            ActionCameraImpulsePlan plan)
        {
            if (cameraController == null || !plan.HasImpulse)
            {
                return false;
            }

            return cameraController.TryRequestImpactImpulse(source, plan.Distance, plan.DurationSeconds, plan.Priority);
        }

        private static string ResolveNormalizedAttackName(AttackDefinitionSO attackDefinition)
        {
            string stateName = attackDefinition.AnimationStateName;

            if (!string.IsNullOrWhiteSpace(stateName))
            {
                return stateName;
            }

            string attackId = attackDefinition.AttackId;

            if (!string.IsNullOrWhiteSpace(attackId))
            {
                return attackId;
            }

            string displayName = attackDefinition.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return string.Empty;
            }

            string compactName = displayName.Replace(" ", string.Empty);
            return compactName.StartsWith(SwordArtPrefix, System.StringComparison.Ordinal)
                ? compactName
                : SwordArtPrefix + compactName;
        }
    }
}
