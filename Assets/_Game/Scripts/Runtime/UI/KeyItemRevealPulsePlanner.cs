using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct KeyItemRevealPulsePlan
    {
        public KeyItemRevealPulsePlan(
            float remainingTime,
            Vector3 basePosition,
            Vector3 visualPosition,
            Vector3 visualScale)
        {
            RemainingTime = remainingTime;
            BasePosition = basePosition;
            VisualPosition = visualPosition;
            VisualScale = visualScale;
        }

        public float RemainingTime { get; }

        public Vector3 BasePosition { get; }

        public Vector3 VisualPosition { get; }

        public Vector3 VisualScale { get; }
    }

    public static class KeyItemRevealPulsePlanner
    {
        public static KeyItemRevealPulsePlan CreateActivationPlan(
            Vector3 worldPosition,
            float groundOffset,
            float pulseDurationSeconds,
            float maxHeight,
            float maxRadius)
        {
            return BuildRuntimePlan(
                worldPosition,
                groundOffset,
                pulseDurationSeconds,
                maxHeight,
                maxRadius,
                Mathf.Max(0.1f, pulseDurationSeconds));
        }

        public static KeyItemRevealPulsePlan BuildRuntimePlan(
            Vector3 worldPosition,
            float groundOffset,
            float pulseDurationSeconds,
            float maxHeight,
            float maxRadius,
            float remainingTime)
        {
            float normalized = pulseDurationSeconds > Mathf.Epsilon
                ? Mathf.Clamp01(remainingTime / pulseDurationSeconds)
                : 0f;
            float height = Mathf.Lerp(maxHeight * 0.3f, maxHeight, normalized);
            float radius = Mathf.Lerp(maxRadius * 0.28f, maxRadius, normalized);
            Vector3 basePosition = worldPosition + Vector3.up * groundOffset;

            return new KeyItemRevealPulsePlan(
                remainingTime,
                basePosition,
                basePosition + Vector3.up * (height * 0.5f),
                new Vector3(radius * 2f, height * 0.5f, radius * 2f));
        }
    }
}
