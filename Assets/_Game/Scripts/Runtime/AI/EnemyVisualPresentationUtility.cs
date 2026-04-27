using CampusRPG.Combat;
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
            float attackProgress,
            EnemyTargetResponseType targetResponse = EnemyTargetResponseType.None)
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

            if (currentStateName == nameof(EnemyChaseState))
            {
                float stride = Mathf.Sin(cycle * 1.05f);
                float drive = Mathf.Abs(stride);
                float plant = 1f - Mathf.Abs(Mathf.Sin(cycle * 2.1f));
                rootOffset.x = 0.014f * speed * sway;
                rootOffset.y = Mathf.Max(rootOffset.y, (0.038f * speed * drive) + (0.01f * speed * plant));
                rootOffset.z = 0.052f * speed * stride;
                rootEuler.x += Mathf.Lerp(0f, -11f, speed) - (5f * speed * drive);
                rootEuler.z += 7f * speed * sway;
                rootScale = Vector3.Lerp(rootScale, new Vector3(0.96f, 1.05f, 1.1f), 0.44f * speed * drive);
                accentEuler += new Vector3(3f * speed * plant, 0f, 18f * speed * stride);
                accentScale = Vector3.Lerp(accentScale, new Vector3(1.04f, 0.98f, 1.08f), 0.5f * speed * drive);

                if (archetypeType == EnemyArchetypeType.Mobile)
                {
                    rootOffset.x += 0.018f * speed * stride;
                    rootEuler.y += 8f * speed * stride;
                    accentEuler += new Vector3(0f, 0f, 10f * speed * sway);
                }
            }
            else if (currentStateName == nameof(EnemyIdleGuardState) || currentStateName == nameof(EnemyEngageState))
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
                    targetResponse,
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
            EnemyTargetResponseType targetResponse,
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

            ApplyMeleeAttackPose(
                archetypeType,
                attackPhase,
                progress,
                targetResponse,
                ref rootOffset,
                ref rootEuler,
                ref rootScale,
                ref accentEuler,
                ref accentScale);
        }

        private static void ApplyMeleeAttackPose(
            EnemyArchetypeType archetypeType,
            EnemyAttackPresentationPhase attackPhase,
            float progress,
            EnemyTargetResponseType targetResponse,
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
                    float anticipationPulse = EasePulse(progress);
                    rootOffset += new Vector3(0f, -0.035f * eased, -0.04f * eased);
                    rootOffset.z -= 0.025f * anticipationPulse;
                    rootEuler += new Vector3(Mathf.Lerp(0f, -18f, eased) - (5f * anticipationPulse), 0f, Mathf.Lerp(0f, -8f, eased));
                    rootScale = Vector3.Lerp(rootScale, new Vector3(1.06f, 0.91f, 1.08f), 0.9f * eased);
                    accentEuler += new Vector3(0f, 0f, Mathf.Lerp(0f, -70f, eased) - (18f * anticipationPulse));
                    accentScale = Vector3.Lerp(accentScale, new Vector3(0.96f, 1.02f, 1.08f), 0.8f * eased);
                    ApplyMeleeArchetypeStartupPose(archetypeType, eased, ref rootOffset, ref rootEuler, ref rootScale, ref accentEuler);
                    ApplyMeleeResponseStartupPose(targetResponse, eased, ref rootOffset, ref rootEuler, ref rootScale, ref accentEuler, ref accentScale);
                    break;
                }

                case EnemyAttackPresentationPhase.Advance:
                {
                    float eased = EaseOut(progress);
                    float strikePulse = EasePulse(progress);
                    rootOffset += new Vector3(0f, Mathf.Lerp(-0.02f, 0.025f, eased), Mathf.Lerp(-0.01f, 0.12f, eased));
                    rootOffset += new Vector3(0f, 0.014f * strikePulse, 0.055f * strikePulse);
                    rootEuler += new Vector3(Mathf.Lerp(-16f, 11f, eased) + (6f * strikePulse), 0f, Mathf.Lerp(-6f, 14f, eased) + (5f * strikePulse));
                    rootScale = Vector3.Lerp(new Vector3(1.03f, 0.94f, 1.04f), new Vector3(0.95f, 1.05f, 1.09f), eased);
                    accentEuler += new Vector3(0f, 0f, Mathf.Lerp(-58f, 96f, eased) + (30f * strikePulse));
                    accentScale = Vector3.Lerp(new Vector3(1f, 1f, 1.04f), new Vector3(1.08f, 0.96f, 1.12f), eased);
                    accentScale = Vector3.Lerp(accentScale, new Vector3(1.13f, 0.93f, 1.16f), 0.5f * strikePulse);
                    ApplyMeleeArchetypeAdvancePose(archetypeType, eased, ref rootOffset, ref rootEuler, ref accentEuler);
                    ApplyMeleeResponseAdvancePose(targetResponse, eased, ref rootOffset, ref rootEuler, ref accentEuler);
                    break;
                }

                case EnemyAttackPresentationPhase.Recovery:
                {
                    float eased = EaseInOut(progress);
                    float recoveryHold = 1f - eased;
                    rootOffset += new Vector3(0f, Mathf.Lerp(0.01f, 0f, eased), Mathf.Lerp(0.07f, 0f, eased));
                    rootOffset += new Vector3(0f, -0.012f * recoveryHold, 0.024f * recoveryHold);
                    rootEuler += new Vector3(Mathf.Lerp(7f, 0f, eased) + (6f * recoveryHold), 0f, Mathf.Lerp(8f, 0f, eased));
                    rootScale = Vector3.Lerp(new Vector3(0.97f, 1.02f, 1.04f), Vector3.one, eased);
                    accentEuler += new Vector3(0f, 0f, Mathf.Lerp(52f, 10f, eased) + (16f * recoveryHold));
                    accentScale = Vector3.Lerp(new Vector3(1.04f, 0.98f, 1.06f), Vector3.one, eased);
                    ApplyMeleeArchetypeRecoveryPose(archetypeType, eased, ref rootOffset, ref rootEuler);
                    ApplyMeleeResponseRecoveryPose(targetResponse, eased, ref rootOffset, ref rootEuler, ref accentEuler);
                    break;
                }
            }
        }

        private static void ApplyMeleeResponseStartupPose(
            EnemyTargetResponseType targetResponse,
            float eased,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 rootScale,
            ref Vector3 accentEuler,
            ref Vector3 accentScale)
        {
            switch (targetResponse)
            {
                case EnemyTargetResponseType.AntiAir:
                    rootOffset.y += 0.062f * eased;
                    rootOffset.z -= 0.012f * eased;
                    rootEuler.x += 8f * eased;
                    rootScale = Vector3.Lerp(rootScale, new Vector3(1.02f, 1.03f, 1.08f), 0.38f * eased);
                    accentEuler.x -= 26f * eased;
                    accentScale = Vector3.Lerp(accentScale, new Vector3(1.03f, 1.1f, 1.08f), 0.5f * eased);
                    break;
                case EnemyTargetResponseType.ChaseRoll:
                    rootOffset.z += 0.075f * eased;
                    rootEuler.y += 12f * eased;
                    rootEuler.x -= 3f * eased;
                    accentEuler.z += 24f * eased;
                    break;
                case EnemyTargetResponseType.GuardBreak:
                    rootOffset.y -= 0.026f * eased;
                    rootOffset.z -= 0.025f * eased;
                    rootEuler.x -= 9f * eased;
                    rootScale = Vector3.Lerp(rootScale, new Vector3(1.13f, 0.84f, 1.13f), 0.42f * eased);
                    accentEuler.z -= 28f * eased;
                    accentScale = Vector3.Lerp(accentScale, new Vector3(1.1f, 0.96f, 1.16f), 0.45f * eased);
                    break;
            }
        }

        private static void ApplyMeleeResponseAdvancePose(
            EnemyTargetResponseType targetResponse,
            float eased,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 accentEuler)
        {
            switch (targetResponse)
            {
                case EnemyTargetResponseType.AntiAir:
                    rootOffset.y += Mathf.Lerp(0.055f, 0.09f, eased);
                    rootEuler.x += Mathf.Lerp(4f, 9f, eased);
                    accentEuler.x -= Mathf.Lerp(24f, -6f, eased);
                    break;
                case EnemyTargetResponseType.ChaseRoll:
                    rootOffset.z += Mathf.Lerp(0.08f, 0.145f, eased);
                    rootEuler.y += Mathf.Lerp(12f, -8f, eased);
                    accentEuler.z += Mathf.Lerp(18f, 42f, eased);
                    break;
                case EnemyTargetResponseType.GuardBreak:
                    rootOffset.y -= Mathf.Lerp(0.026f, 0.01f, eased);
                    rootEuler.x -= Mathf.Lerp(11f, 3f, eased);
                    accentEuler.z -= Mathf.Lerp(28f, 8f, eased);
                    break;
            }
        }

        private static void ApplyMeleeResponseRecoveryPose(
            EnemyTargetResponseType targetResponse,
            float eased,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 accentEuler)
        {
            switch (targetResponse)
            {
                case EnemyTargetResponseType.AntiAir:
                    rootOffset.y += Mathf.Lerp(0.052f, 0f, eased);
                    rootEuler.x += Mathf.Lerp(7f, 0f, eased);
                    accentEuler.x -= Mathf.Lerp(14f, 0f, eased);
                    break;
                case EnemyTargetResponseType.ChaseRoll:
                    rootOffset.z += Mathf.Lerp(0.075f, 0f, eased);
                    rootEuler.y += Mathf.Lerp(-12f, 0f, eased);
                    accentEuler.z += Mathf.Lerp(18f, 0f, eased);
                    break;
                case EnemyTargetResponseType.GuardBreak:
                    rootOffset.y -= Mathf.Lerp(0.026f, 0f, eased);
                    rootEuler.x -= Mathf.Lerp(9f, 0f, eased);
                    accentEuler.z -= Mathf.Lerp(18f, 0f, eased);
                    break;
            }
        }

        private static void ApplyMeleeArchetypeStartupPose(
            EnemyArchetypeType archetypeType,
            float eased,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 rootScale,
            ref Vector3 accentEuler)
        {
            if (archetypeType == EnemyArchetypeType.Mobile)
            {
                rootOffset.x += 0.038f * eased;
                rootEuler.y += 12f * eased;
                rootScale = Vector3.Lerp(rootScale, new Vector3(1.07f, 0.9f, 1.05f), 0.35f * eased);
                accentEuler += new Vector3(0f, -10f * eased, -18f * eased);
                return;
            }

            if (archetypeType == EnemyArchetypeType.Boss)
            {
                rootOffset.y -= 0.014f * eased;
                rootEuler.x -= 5f * eased;
                rootScale = Vector3.Lerp(rootScale, new Vector3(1.09f, 0.88f, 1.11f), 0.35f * eased);
                accentEuler.z -= 10f * eased;
            }
        }

        private static void ApplyMeleeArchetypeAdvancePose(
            EnemyArchetypeType archetypeType,
            float eased,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler,
            ref Vector3 accentEuler)
        {
            if (archetypeType == EnemyArchetypeType.Mobile)
            {
                rootOffset.x += Mathf.Lerp(0.035f, -0.026f, eased);
                rootEuler.y += Mathf.Lerp(10f, -8f, eased);
                accentEuler.z += Mathf.Lerp(-18f, 24f, eased);
                return;
            }

            if (archetypeType == EnemyArchetypeType.Boss)
            {
                rootOffset.y += 0.018f * eased;
                rootEuler.x += 4f * eased;
                accentEuler.z += 12f * eased;
            }
        }

        private static void ApplyMeleeArchetypeRecoveryPose(
            EnemyArchetypeType archetypeType,
            float eased,
            ref Vector3 rootOffset,
            ref Vector3 rootEuler)
        {
            if (archetypeType == EnemyArchetypeType.Mobile)
            {
                rootOffset.x += Mathf.Lerp(-0.024f, 0f, eased);
                rootEuler.y += Mathf.Lerp(-6f, 0f, eased);
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
                    float chargePulse = EasePulse(progress);
                    rootOffset += new Vector3(0f, 0.024f * eased, -0.035f * eased);
                    rootOffset.y += 0.014f * chargePulse;
                    rootEuler += new Vector3(Mathf.Lerp(0f, -13f, eased), 0f, Mathf.Lerp(0f, 6f, eased));
                    rootScale = Vector3.Lerp(rootScale, new Vector3(0.97f, 1.04f, 1.04f), 0.85f * eased);
                    accentEuler += new Vector3(Mathf.Lerp(0f, -26f, eased) - (10f * chargePulse), 0f, Mathf.Lerp(0f, 26f, eased));
                    accentScale = Vector3.Lerp(accentScale, new Vector3(1.26f, 1.26f, 1.26f), 0.95f * eased);
                    accentScale = Vector3.Lerp(accentScale, new Vector3(1.38f, 1.38f, 1.38f), 0.35f * chargePulse);
                    break;
                }

                case EnemyAttackPresentationPhase.Advance:
                {
                    float eased = EaseOut(progress);
                    float releasePulse = EasePulse(progress);
                    rootOffset += new Vector3(0f, Mathf.Lerp(0.015f, 0.03f, eased), Mathf.Lerp(0f, 0.05f, eased));
                    rootOffset.y += 0.018f * releasePulse;
                    rootEuler += new Vector3(Mathf.Lerp(-8f, 4f, eased), 0f, Mathf.Lerp(4f, -2f, eased));
                    rootScale = Vector3.Lerp(new Vector3(0.99f, 1.01f, 1.02f), new Vector3(1.02f, 0.98f, 1.04f), eased);
                    accentEuler += new Vector3(Mathf.Lerp(-8f, 10f, eased) + (16f * releasePulse), 0f, Mathf.Lerp(20f, -12f, eased));
                    accentScale = Vector3.Lerp(new Vector3(1.14f, 1.14f, 1.14f), new Vector3(1.32f, 1.32f, 1.32f), eased);
                    accentScale = Vector3.Lerp(accentScale, new Vector3(1.46f, 1.46f, 1.46f), 0.6f * releasePulse);
                    break;
                }

                case EnemyAttackPresentationPhase.Recovery:
                {
                    float eased = EaseInOut(progress);
                    rootOffset += new Vector3(0f, Mathf.Lerp(-0.008f, 0f, eased), Mathf.Lerp(-0.035f, 0f, eased));
                    rootEuler += new Vector3(Mathf.Lerp(7f, 0f, eased), 0f, Mathf.Lerp(-6f, 0f, eased));
                    rootScale = Vector3.Lerp(new Vector3(1.03f, 0.97f, 1.03f), Vector3.one, eased);
                    accentEuler += new Vector3(Mathf.Lerp(14f, 0f, eased), 0f, Mathf.Lerp(-18f, 0f, eased));
                    accentScale = Vector3.Lerp(new Vector3(1.18f, 1.18f, 1.18f), Vector3.one, eased);
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

        private static float EasePulse(float value)
        {
            return Mathf.Sin(Mathf.Clamp01(value) * Mathf.PI);
        }
    }
}
