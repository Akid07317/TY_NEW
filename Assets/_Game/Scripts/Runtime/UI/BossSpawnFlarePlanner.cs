using CampusRPG.AI;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossSpawnFlarePlan
    {
        public BossSpawnFlarePlan(
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

    public static class BossSpawnFlarePlanner
    {
        public static BossSpawnFlarePlan CreateActivationPlan(
            EnemyBrain bossEnemy,
            float groundOffset,
            float flareDurationSeconds,
            float maxHeight,
            float maxRadius)
        {
            return BuildRuntimePlan(
                bossEnemy,
                groundOffset,
                flareDurationSeconds,
                maxHeight,
                maxRadius,
                Mathf.Max(0.1f, flareDurationSeconds));
        }

        public static BossSpawnFlarePlan BuildRuntimePlan(
            EnemyBrain bossEnemy,
            float groundOffset,
            float flareDurationSeconds,
            float maxHeight,
            float maxRadius,
            float remainingTime)
        {
            float normalized = flareDurationSeconds > Mathf.Epsilon
                ? Mathf.Clamp01(remainingTime / flareDurationSeconds)
                : 0f;
            float height = Mathf.Lerp(maxHeight * 0.45f, maxHeight, normalized);
            float radius = Mathf.Lerp(maxRadius * 0.35f, maxRadius, normalized);
            Vector3 basePosition = (bossEnemy != null ? bossEnemy.transform.position : Vector3.zero) + Vector3.up * groundOffset;

            return new BossSpawnFlarePlan(
                remainingTime,
                basePosition,
                basePosition + Vector3.up * (height * 0.5f),
                new Vector3(radius * 2f, height * 0.5f, radius * 2f));
        }
    }
}
