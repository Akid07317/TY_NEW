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

        public static float TickWindow(float currentTimer, float deltaTime)
        {
            return Mathf.Max(0f, currentTimer - Mathf.Max(0f, deltaTime));
        }

        public static float OpenWindow(float currentTimer, float duration)
        {
            return Mathf.Max(currentTimer, duration);
        }
    }
}
