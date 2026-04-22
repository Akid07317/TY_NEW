using UnityEngine;

namespace CampusRPG.AI
{
    public enum EnemyAttackPresentationPhase
    {
        None,
        Startup,
        Advance,
        Recovery
    }

    public readonly struct EnemyVisualPresentationPose
    {
        public EnemyVisualPresentationPose(
            Vector3 rootLocalOffset,
            Vector3 rootLocalEulerAngles,
            Vector3 rootLocalScale,
            Vector3 accentLocalEulerAngles,
            Vector3 accentLocalScale)
        {
            RootLocalOffset = rootLocalOffset;
            RootLocalEulerAngles = rootLocalEulerAngles;
            RootLocalScale = rootLocalScale;
            AccentLocalEulerAngles = accentLocalEulerAngles;
            AccentLocalScale = accentLocalScale;
        }

        public Vector3 RootLocalOffset { get; }

        public Vector3 RootLocalEulerAngles { get; }

        public Vector3 RootLocalScale { get; }

        public Vector3 AccentLocalEulerAngles { get; }

        public Vector3 AccentLocalScale { get; }
    }

    public static class EnemyVisualPresentationUtility
    {
        public static EnemyVisualPresentationPose ResolvePose(
            EnemyArchetypeType archetypeType,
            string currentStateName,
            float moveSpeedNormalized,
            float locomotionCycle,
            EnemyAttackPresentationPhase attackPhase,
            float attackProgress)
        {
            float speed = Mathf.Clamp01(moveSpeedNormalized);
            float cycle = locomotionCycle * Mathf.Lerp(1.4f, 4.8f, speed);
            float bob = Mathf.Sin(cycle);
            float sway = Mathf.Sin(cycle + 0.8f);
            float lift = Mathf.Abs(bob);

            Vector3 rootOffset = new Vector3(0f, 0.014f * speed * lift, 0f);
            Vector3 rootEuler = new Vector3(-3f * speed * lift, 0f, 4f * speed * sway);
            Vector3 rootScale = new Vector3(
                1f + (0.016f * speed * lift),
                1f - (0.014f * speed * lift),
                1f + (0.028f * speed * Mathf.Abs(sway)));
            Vector3 accentEuler = new Vector3(0f, 0f, 8f * speed * sway);
            Vector3 accentScale = Vector3.one;

            if (currentStateName == nameof(EnemyIdleGuardState) || currentStateName == nameof(EnemyEngageState))
            {
                float idlePulse = 0.5f + (0.5f * Mathf.Sin(locomotionCycle * 1.6f));
                rootOffset.y = Mathf.Max(rootOffset.y, 0.008f * idlePulse);
                rootScale = Vector3.Lerp(rootScale, new Vector3(1.01f, 0.99f, 1.02f), 0.35f);
            }
            else if (currentStateName == nameof(EnemyStrafeState))
            {
                rootOffset.x = 0.024f * sway * Mathf.Max(0.35f, speed);
                rootEuler.z += 6f * sway;
            }
            else if (currentStateName == nameof(EnemyHitState))
            {
                return new EnemyVisualPresentationPose(
                    new Vector3(0f, -0.015f, -0.05f),
                    new Vector3(9f, 0f, 7f),
                    new Vector3(1.04f, 0.95f, 1.03f),
                    new Vector3(0f, 0f, 28f),
                    new Vector3(1.06f, 0.96f, 1.02f));
            }
            else if (currentStateName == nameof(EnemyDeathState))
            {
                return new EnemyVisualPresentationPose(
                    new Vector3(0f, -0.22f, -0.08f),
                    new Vector3(0f, 0f, 78f),
                    new Vector3(1.04f, 0.92f, 1.08f),
                    Vector3.zero,
                    new Vector3(0.96f, 0.96f, 0.96f));
            }

            if (currentStateName == nameof(EnemyAttackState))
            {
                ApplyAttackPose(
                    archetypeType,
                    attackPhase,
                    attackProgress,
                    ref rootOffset,
                    ref rootEuler,
                    ref rootScale,
                    ref accentEuler,
                    ref accentScale);
            }

            return new EnemyVisualPresentationPose(rootOffset, rootEuler, rootScale, accentEuler, accentScale);
        }

        private static void ApplyAttackPose(
            EnemyArchetypeType archetypeType,
            EnemyAttackPresentationPhase attackPhase,
            float attackProgress,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 rootScale,
            ref Vector3 accentEuler,
            ref Vector3 accentScale)
        {
            float progress = Mathf.Clamp01(attackProgress);

            if (archetypeType == EnemyArchetypeType.Ranged)
            {
                ApplyRangedAttackPose(attackPhase, progress, ref rootOffset, ref rootEuler, ref rootScale, ref accentEuler, ref accentScale);
                return;
            }

            ApplyMeleeAttackPose(attackPhase, progress, ref rootOffset, ref rootEuler, ref rootScale, ref accentEuler, ref accentScale);
        }

        private static void ApplyMeleeAttackPose(
            EnemyAttackPresentationPhase attackPhase,
            float progress,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 rootScale,
            ref Vector3 accentEuler,
            ref Vector3 accentScale)
        {
            switch (attackPhase)
            {
                case EnemyAttackPresentationPhase.Startup:
                {
                    float eased = EaseInOut(progress);
                    rootOffset += new Vector3(0f, -0.035f * eased, -0.04f * eased);
                    rootEuler += new Vector3(Mathf.Lerp(0f, -18f, eased), 0f, Mathf.Lerp(0f, -8f, eased));
                    rootScale = Vector3.Lerp(rootScale, new Vector3(1.06f, 0.91f, 1.08f), 0.9f * eased);
                    accentEuler += new Vector3(0f, 0f, Mathf.Lerp(0f, -70f, eased));
                    accentScale = Vector3.Lerp(accentScale, new Vector3(0.96f, 1.02f, 1.08f), 0.8f * eased);
                    break;
                }

                case EnemyAttackPresentationPhase.Advance:
                {
                    float eased = EaseOut(progress);
                    rootOffset += new Vector3(0f, Mathf.Lerp(-0.02f, 0.025f, eased), Mathf.Lerp(-0.01f, 0.12f, eased));
                    rootEuler += new Vector3(Mathf.Lerp(-16f, 11f, eased), 0f, Mathf.Lerp(-6f, 14f, eased));
                    rootScale = Vector3.Lerp(new Vector3(1.03f, 0.94f, 1.04f), new Vector3(0.95f, 1.05f, 1.09f), eased);
                    accentEuler += new Vector3(0f, 0f, Mathf.Lerp(-58f, 96f, eased));
                    accentScale = Vector3.Lerp(new Vector3(1f, 1f, 1.04f), new Vector3(1.08f, 0.96f, 1.12f), eased);
                    break;
                }

                case EnemyAttackPresentationPhase.Recovery:
                {
                    float eased = EaseInOut(progress);
                    rootOffset += new Vector3(0f, Mathf.Lerp(0.01f, 0f, eased), Mathf.Lerp(0.07f, 0f, eased));
                    rootEuler += new Vector3(Mathf.Lerp(7f, 0f, eased), 0f, Mathf.Lerp(8f, 0f, eased));
                    rootScale = Vector3.Lerp(new Vector3(0.97f, 1.02f, 1.04f), Vector3.one, eased);
                    accentEuler += new Vector3(0f, 0f, Mathf.Lerp(52f, 10f, eased));
                    accentScale = Vector3.Lerp(new Vector3(1.04f, 0.98f, 1.06f), Vector3.one, eased);
                    break;
                }
            }
        }

        private static void ApplyRangedAttackPose(
            EnemyAttackPresentationPhase attackPhase,
            float progress,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 rootScale,
            ref Vector3 accentEuler,
            ref Vector3 accentScale)
        {
            switch (attackPhase)
            {
                case EnemyAttackPresentationPhase.Startup:
                {
                    float eased = EaseInOut(progress);
                    rootOffset += new Vector3(0f, 0.015f * eased, -0.02f * eased);
                    rootEuler += new Vector3(Mathf.Lerp(0f, -10f, eased), 0f, Mathf.Lerp(0f, 4f, eased));
                    rootScale = Vector3.Lerp(rootScale, new Vector3(0.98f, 1.02f, 1.03f), 0.8f * eased);
                    accentEuler += new Vector3(Mathf.Lerp(0f, -18f, eased), 0f, Mathf.Lerp(0f, 18f, eased));
                    accentScale = Vector3.Lerp(accentScale, new Vector3(1.18f, 1.18f, 1.18f), 0.95f * eased);
                    break;
                }

                case EnemyAttackPresentationPhase.Advance:
                {
                    float eased = EaseOut(progress);
                    rootOffset += new Vector3(0f, Mathf.Lerp(0.015f, 0.03f, eased), Mathf.Lerp(0f, 0.05f, eased));
                    rootEuler += new Vector3(Mathf.Lerp(-8f, 4f, eased), 0f, Mathf.Lerp(4f, -2f, eased));
                    rootScale = Vector3.Lerp(new Vector3(0.99f, 1.01f, 1.02f), new Vector3(1.02f, 0.98f, 1.04f), eased);
                    accentEuler += new Vector3(Mathf.Lerp(-8f, 10f, eased), 0f, Mathf.Lerp(20f, -12f, eased));
                    accentScale = Vector3.Lerp(new Vector3(1.14f, 1.14f, 1.14f), new Vector3(1.32f, 1.32f, 1.32f), eased);
                    break;
                }

                case EnemyAttackPresentationPhase.Recovery:
                {
                    float eased = EaseInOut(progress);
                    rootOffset += new Vector3(0f, Mathf.Lerp(0.015f, 0f, eased), Mathf.Lerp(0.03f, 0f, eased));
                    rootEuler += new Vector3(Mathf.Lerp(3f, 0f, eased), 0f, Mathf.Lerp(-2f, 0f, eased));
                    rootScale = Vector3.Lerp(new Vector3(1.01f, 0.99f, 1.02f), Vector3.one, eased);
                    accentEuler += new Vector3(Mathf.Lerp(8f, 0f, eased), 0f, Mathf.Lerp(-8f, 0f, eased));
                    accentScale = Vector3.Lerp(new Vector3(1.1f, 1.1f, 1.1f), Vector3.one, eased);
                    break;
                }
            }
        }

        private static float EaseInOut(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - (2f * t));
        }

        private static float EaseOut(float value)
        {
            float t = Mathf.Clamp01(value);
            float inverse = 1f - t;
            return 1f - (inverse * inverse);
        }
    }
}
