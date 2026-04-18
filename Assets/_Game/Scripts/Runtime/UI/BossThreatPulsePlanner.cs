using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossThreatPulsePlan
    {
        public BossThreatPulsePlan(BossThreatPulsePresenter.PulseKind kind, Color color, float duration)
        {
            Kind = kind;
            Color = color;
            Duration = duration;
        }

        public BossThreatPulsePresenter.PulseKind Kind { get; }

        public Color Color { get; }

        public float Duration { get; }
    }

    public static class BossThreatPulsePlanner
    {
        private static readonly Color FallbackEncounterPulseColor = new Color(0.95f, 0.54f, 0.14f, 0.22f);
        private static readonly Color FallbackAttackPulseColor = new Color(0.88f, 0.16f, 0.14f, 0.26f);

        public static BossThreatPulsePlan CreateEncounterPlan(
            BossTelegraphStyleSO telegraphStyle,
            float encounterPulseSeconds,
            Color encounterPulseColor)
        {
            return new BossThreatPulsePlan(
                BossThreatPulsePresenter.PulseKind.Encounter,
                telegraphStyle != null
                    ? telegraphStyle.EncounterPulseColor
                    : encounterPulseColor != default
                        ? encounterPulseColor
                        : FallbackEncounterPulseColor,
                Mathf.Max(0.05f, encounterPulseSeconds));
        }

        public static BossThreatPulsePlan CreateAttackPlan(
            BossTelegraphStyleSO telegraphStyle,
            float attackPulseSeconds,
            Color attackPulseColor)
        {
            return new BossThreatPulsePlan(
                BossThreatPulsePresenter.PulseKind.Attack,
                telegraphStyle != null
                    ? telegraphStyle.AttackPulseColor
                    : attackPulseColor != default
                        ? attackPulseColor
                        : FallbackAttackPulseColor,
                Mathf.Max(0.05f, attackPulseSeconds));
        }

        public static float EvaluateAlpha(float pulseDuration, float pulseRemaining)
        {
            if (pulseDuration <= Mathf.Epsilon)
            {
                return 0f;
            }

            float normalized = 1f - (pulseRemaining / pulseDuration);
            float fadeIn = Mathf.Clamp01(normalized / 0.18f);
            float fadeOut = Mathf.Clamp01(pulseRemaining / pulseDuration);
            return Mathf.Min(fadeIn, fadeOut);
        }
    }
}
