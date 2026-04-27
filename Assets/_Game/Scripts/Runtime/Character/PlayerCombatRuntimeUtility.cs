using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    public readonly struct PlayerCombatComboState
    {
        public PlayerCombatComboState(int nextLightAttackIndex, float comboResetTimer)
        {
            NextLightAttackIndex = nextLightAttackIndex;
            ComboResetTimer = comboResetTimer;
        }

        public int NextLightAttackIndex { get; }

        public float ComboResetTimer { get; }
    }

    public static class PlayerCombatRuntimeUtility
    {
        public static PlayerCombatComboState TickComboState(int nextLightAttackIndex, float comboResetTimer, float deltaTime)
        {
            if (comboResetTimer <= 0f)
            {
                return new PlayerCombatComboState(nextLightAttackIndex, 0f);
            }

            float nextComboResetTimer = comboResetTimer - Mathf.Max(0f, deltaTime);

            if (nextComboResetTimer <= 0f)
            {
                return new PlayerCombatComboState(0, 0f);
            }

            return new PlayerCombatComboState(nextLightAttackIndex, nextComboResetTimer);
        }

        public static AttackDefinitionSO ResolveLightAttack(AttackDefinitionSO[] lightAttackCombo, int nextLightAttackIndex, float comboResetTimer)
        {
            if (lightAttackCombo == null || lightAttackCombo.Length == 0)
            {
                return null;
            }

            int resolvedIndex = comboResetTimer > 0f
                ? Mathf.Clamp(nextLightAttackIndex, 0, lightAttackCombo.Length - 1)
                : 0;
            return lightAttackCombo[resolvedIndex];
        }

        public static PlayerCombatComboState ResolveComboStateAfterAttackFinished(
            PlayerAttackRequest request,
            int nextLightAttackIndex,
            int comboLength,
            float comboResetSeconds)
        {
            if (request == PlayerAttackRequest.Light
                && comboLength > 0
                && nextLightAttackIndex < comboLength - 1)
            {
                return new PlayerCombatComboState(nextLightAttackIndex + 1, Mathf.Max(0f, comboResetSeconds));
            }

            return new PlayerCombatComboState(0, 0f);
        }

        public static AttackDefinitionSO ResolveCounterAttack(
            GaugeComponent gauges,
            AttackDefinitionSO counterAttack,
            AttackDefinitionSO empoweredCounterAttack)
        {
            if (gauges != null && gauges.TryConsumeCounterFull() && empoweredCounterAttack != null)
            {
                return empoweredCounterAttack;
            }

            return counterAttack;
        }

        public static AttackDefinitionSO ResolveDodgeFollowUpAttack(
            GaugeComponent gauges,
            AttackDefinitionSO dodgeFollowUpAttack,
            AttackDefinitionSO empoweredDodgeFollowUpAttack)
        {
            if (gauges != null && gauges.TryConsumeAgilityFull() && empoweredDodgeFollowUpAttack != null)
            {
                return empoweredDodgeFollowUpAttack;
            }

            return dodgeFollowUpAttack;
        }

        public static float ResolveAttackRecoverySeconds(AttackDefinitionSO attackDefinition)
        {
            if (attackDefinition == null)
            {
                return 0f;
            }

            float configuredRecovery = Mathf.Max(0f, attackDefinition.RecoverySeconds);
            float configuredDuration = attackDefinition.AnimationDurationSeconds;

            if (configuredDuration <= 0f)
            {
                return configuredRecovery;
            }

            float animationRecoverySeconds = Mathf.Max(
                0f,
                configuredDuration - attackDefinition.StartupSeconds - attackDefinition.ActiveSeconds);

            if (animationRecoverySeconds <= configuredRecovery)
            {
                return configuredRecovery;
            }

            float extraTailSeconds = animationRecoverySeconds - configuredRecovery;
            float cappedRecovery = configuredRecovery + Mathf.Min(extraTailSeconds, ResolveRecoveryExtensionBudget(attackDefinition.AnimationStateName));
            float minimumVisibleRecovery = Mathf.Max(
                configuredRecovery,
                ResolveMinimumVisibleAttackDuration(attackDefinition) - attackDefinition.StartupSeconds - attackDefinition.ActiveSeconds);
            return Mathf.Min(configuredDuration, attackDefinition.StartupSeconds + attackDefinition.ActiveSeconds + Mathf.Max(cappedRecovery, minimumVisibleRecovery))
                - attackDefinition.StartupSeconds
                - attackDefinition.ActiveSeconds;
        }

        public static float TickWindow(float currentTimer, float deltaTime)
        {
            return Mathf.Max(0f, currentTimer - Mathf.Max(0f, deltaTime));
        }

        public static float OpenWindow(float currentTimer, float duration)
        {
            return Mathf.Max(currentTimer, duration);
        }

        public static bool ShouldSnapProxyWeaponFollow(
            bool hasSnappedToAnchor,
            bool isImmediateFollowState,
            float immediateFollowTimer,
            float rotationDeltaDegrees,
            float positionDelta,
            float snapRotationThresholdDegrees,
            float snapPositionThreshold)
        {
            if (!hasSnappedToAnchor)
            {
                return true;
            }

            if (isImmediateFollowState || immediateFollowTimer > 0f)
            {
                return true;
            }

            return rotationDeltaDegrees >= Mathf.Max(0f, snapRotationThresholdDegrees)
                || positionDelta >= Mathf.Max(0f, snapPositionThreshold);
        }

        private static float ResolveRecoveryExtensionBudget(string animationStateName)
        {
            if (string.IsNullOrWhiteSpace(animationStateName))
            {
                return 0.08f;
            }

            if (IsSwordArtState(animationStateName))
            {
                return 0.18f;
            }

            if (animationStateName.StartsWith("Light_", System.StringComparison.Ordinal))
            {
                return 0.06f;
            }

            if (animationStateName.StartsWith("Heavy_", System.StringComparison.Ordinal))
            {
                return 0.1f;
            }

            if (animationStateName.StartsWith("DodgeFollowUp", System.StringComparison.Ordinal)
                || animationStateName.StartsWith("Counter", System.StringComparison.Ordinal))
            {
                return 0.08f;
            }

            return 0.08f;
        }

        private static float ResolveMinimumVisibleAttackDuration(AttackDefinitionSO attackDefinition)
        {
            float baseDuration = Mathf.Max(
                0f,
                attackDefinition.StartupSeconds + attackDefinition.ActiveSeconds + attackDefinition.RecoverySeconds);
            float configuredDuration = Mathf.Max(baseDuration, attackDefinition.AnimationDurationSeconds);
            string animationStateName = attackDefinition.AnimationStateName;

            if (string.IsNullOrWhiteSpace(animationStateName))
            {
                return Mathf.Min(configuredDuration, baseDuration + 0.18f);
            }

            if (IsSwordArtState(animationStateName))
            {
                return Mathf.Min(
                    configuredDuration,
                    Mathf.Max(baseDuration + 0.24f, configuredDuration * 0.88f));
            }

            if (animationStateName.StartsWith("Light_", System.StringComparison.Ordinal))
            {
                return Mathf.Min(configuredDuration, Mathf.Max(baseDuration + 0.18f, 0.72f));
            }

            if (animationStateName.StartsWith("Heavy_", System.StringComparison.Ordinal))
            {
                return Mathf.Min(configuredDuration, Mathf.Max(baseDuration + 0.22f, 0.96f));
            }

            if (animationStateName.StartsWith("DodgeFollowUp", System.StringComparison.Ordinal)
                || animationStateName.StartsWith("Counter", System.StringComparison.Ordinal))
            {
                return Mathf.Min(configuredDuration, Mathf.Max(baseDuration + 0.2f, 0.8f));
            }

            return Mathf.Min(configuredDuration, baseDuration + 0.18f);
        }

        private static bool IsSwordArtState(string animationStateName)
        {
            return animationStateName.StartsWith("SwordArt_", System.StringComparison.Ordinal);
        }
    }
}
