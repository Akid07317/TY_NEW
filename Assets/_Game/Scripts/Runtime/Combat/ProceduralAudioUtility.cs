using System.Collections.Generic;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Core;
using UnityEngine;

namespace CampusRPG.Combat
{
    public enum ProceduralActionAudioMixGroup
    {
        None,
        Movement,
        SwordArt,
        Impact,
        BossResponse
    }

    public enum ProceduralActionAudioDecisionKind
    {
        None,
        Played,
        NoAudio,
        BatchMode,
        Muted,
        Cooldown,
        DominanceBlocked
    }

    public readonly struct ProceduralActionAudioDecision
    {
        public ProceduralActionAudioDecision(
            ProceduralActionAudioDecisionKind kind,
            ProceduralActionAudioPlan plan,
            float currentTimeSeconds,
            float blockUntilSeconds = 0f,
            int activeDominantPriority = 0)
        {
            Kind = kind;
            CueId = plan.CueId ?? string.Empty;
            MixGroup = plan.MixGroup;
            Priority = plan.Priority;
            CurrentTimeSeconds = Mathf.Max(0f, currentTimeSeconds);
            BlockUntilSeconds = Mathf.Max(0f, blockUntilSeconds);
            ActiveDominantPriority = Mathf.Max(0, activeDominantPriority);
        }

        public ProceduralActionAudioDecisionKind Kind { get; }

        public string CueId { get; }

        public ProceduralActionAudioMixGroup MixGroup { get; }

        public int Priority { get; }

        public float CurrentTimeSeconds { get; }

        public float BlockUntilSeconds { get; }

        public int ActiveDominantPriority { get; }

        public bool ShouldPlay => Kind == ProceduralActionAudioDecisionKind.Played;

        public bool IsVisible => Kind != ProceduralActionAudioDecisionKind.None
            && Kind != ProceduralActionAudioDecisionKind.NoAudio
            && !string.IsNullOrWhiteSpace(CueId);

        public float SecondsRemaining => Mathf.Max(0f, BlockUntilSeconds - CurrentTimeSeconds);

        public static ProceduralActionAudioDecision None => default;
    }

    public readonly struct ProceduralActionAudioPlan
    {
        public ProceduralActionAudioPlan(
            string cueId,
            float startFrequency,
            float endFrequency,
            float durationSeconds,
            float volume,
            ProceduralActionAudioMixGroup mixGroup = ProceduralActionAudioMixGroup.None,
            float cooldownSeconds = 0.08f,
            float spatialBlend = 0.65f,
            float minDistance = 1.2f,
            float maxDistance = 14f,
            int priority = 0,
            float dominanceSeconds = 0f)
        {
            CueId = cueId ?? string.Empty;
            StartFrequency = Mathf.Max(0f, startFrequency);
            EndFrequency = Mathf.Max(0f, endFrequency);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Volume = Mathf.Clamp01(volume);
            MixGroup = mixGroup;
            CooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            SpatialBlend = Mathf.Clamp01(spatialBlend);
            MinDistance = Mathf.Max(0.01f, minDistance);
            MaxDistance = Mathf.Max(MinDistance + 0.01f, maxDistance);
            Priority = Mathf.Max(0, priority);
            DominanceSeconds = Mathf.Max(0f, dominanceSeconds);
        }

        public string CueId { get; }

        public float StartFrequency { get; }

        public float EndFrequency { get; }

        public float DurationSeconds { get; }

        public float Volume { get; }

        public ProceduralActionAudioMixGroup MixGroup { get; }

        public float CooldownSeconds { get; }

        public float SpatialBlend { get; }

        public float MinDistance { get; }

        public float MaxDistance { get; }

        public int Priority { get; }

        public float DominanceSeconds { get; }

        public bool HasAudio => !string.IsNullOrWhiteSpace(CueId)
            && StartFrequency > 0f
            && EndFrequency > 0f
            && DurationSeconds > 0f
            && Volume > 0f;

        public static ProceduralActionAudioPlan None => default;
    }

    public static class ProceduralAudioUtility
    {
        private const int SampleRate = 44100;
        public const int ActionAudioPriorityMovement = 10;
        public const int ActionAudioPrioritySwordArt = 20;
        public const int ActionAudioPriorityHeavyRead = 30;
        public const int ActionAudioPriorityGuardBreak = 40;
        private const string SwordArtPrefix = "SwordArt_";
        private const string SidewindCutState = "SwordArt_SidewindCut";
        private const string CrossStepState = "SwordArt_CrossStep";
        private const string RisingCleaveState = "SwordArt_RisingCleave";
        private const string IronGateBreakState = "SwordArt_IronGateBreak";
        private const string FallingStarState = "SwordArt_FallingStar";
        private const string MoonSeverState = "SwordArt_MoonSever";
        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<string, float> LastActionCueTimes = new Dictionary<string, float>();
        private static int activeDominantActionCuePriority;
        private static float activeDominantActionCueUntil;

        public static ProceduralActionAudioDecision LastActionCueDecision { get; private set; }

        public static float ResolveSfxVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            AudioSettingsSO audioSettings = SceneRuntimeReferenceUtility.ResolveAudioSettings();
            return audioSettings != null
                ? audioSettings.ResolveSfxVolume(clampedVolume)
                : clampedVolume;
        }

        public static ProceduralActionAudioPlan ResolveEvasiveActionCue(PlayerEvasiveActionType actionType)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => new ProceduralActionAudioPlan(
                    "CombatRoll",
                    180f,
                    90f,
                    0.09f,
                    0.22f,
                    ProceduralActionAudioMixGroup.Movement,
                    0.12f,
                    0.45f,
                    1f,
                    10f,
                    ActionAudioPriorityMovement,
                    0.04f),
                PlayerEvasiveActionType.AirDodge => new ProceduralActionAudioPlan(
                    "AirDodge",
                    760f,
                    1120f,
                    0.075f,
                    0.2f,
                    ProceduralActionAudioMixGroup.Movement,
                    0.1f,
                    0.5f,
                    1f,
                    12f,
                    ActionAudioPriorityMovement,
                    0.04f),
                _ => ProceduralActionAudioPlan.None
            };
        }

        public static ProceduralActionAudioPlan ResolvePlayerAttackCue(AttackDefinitionSO attackDefinition)
        {
            if (attackDefinition == null)
            {
                return ProceduralActionAudioPlan.None;
            }

            return ResolveNormalizedAttackName(attackDefinition) switch
            {
                FallingStarState => new ProceduralActionAudioPlan(
                    "FallingStar",
                    1120f,
                    140f,
                    0.14f,
                    0.34f,
                    ProceduralActionAudioMixGroup.SwordArt,
                    0.18f,
                    0.6f,
                    1.5f,
                    16f,
                    ActionAudioPriorityHeavyRead,
                    0.12f),
                IronGateBreakState => new ProceduralActionAudioPlan(
                    "IronGateBreak",
                    240f,
                    110f,
                    0.12f,
                    0.3f,
                    ProceduralActionAudioMixGroup.SwordArt,
                    0.16f,
                    0.6f,
                    1.5f,
                    15f,
                    ActionAudioPriorityGuardBreak,
                    0.14f),
                RisingCleaveState => new ProceduralActionAudioPlan(
                    "RisingCleave",
                    520f,
                    980f,
                    0.1f,
                    0.23f,
                    ProceduralActionAudioMixGroup.SwordArt,
                    0.14f,
                    0.55f,
                    1.5f,
                    14f,
                    ActionAudioPrioritySwordArt,
                    0.08f),
                MoonSeverState => new ProceduralActionAudioPlan(
                    "MoonSever",
                    980f,
                    620f,
                    0.09f,
                    0.23f,
                    ProceduralActionAudioMixGroup.SwordArt,
                    0.14f,
                    0.55f,
                    1.5f,
                    14f,
                    ActionAudioPrioritySwordArt,
                    0.08f),
                CrossStepState => new ProceduralActionAudioPlan(
                    "CrossStep",
                    880f,
                    520f,
                    0.09f,
                    0.22f,
                    ProceduralActionAudioMixGroup.SwordArt,
                    0.14f,
                    0.55f,
                    1.5f,
                    14f,
                    ActionAudioPrioritySwordArt,
                    0.08f),
                SidewindCutState => new ProceduralActionAudioPlan(
                    "SidewindCut",
                    650f,
                    420f,
                    0.08f,
                    0.18f,
                    ProceduralActionAudioMixGroup.SwordArt,
                    0.12f,
                    0.5f,
                    1.2f,
                    12f,
                    ActionAudioPrioritySwordArt,
                    0.08f),
                _ => attackDefinition.BreaksGuard
                    ? new ProceduralActionAudioPlan(
                        "GuardBreakAttack",
                        190f,
                        80f,
                        0.13f,
                        0.28f,
                        ProceduralActionAudioMixGroup.Impact,
                        0.2f,
                        0.7f,
                        1.5f,
                        16f,
                        ActionAudioPriorityGuardBreak,
                        0.14f)
                    : ProceduralActionAudioPlan.None
            };
        }

        public static ProceduralActionAudioPlan ResolveHitReactionCue(PlayerHitReactionType reactionType)
        {
            return reactionType == PlayerHitReactionType.GuardBreak
                ? new ProceduralActionAudioPlan(
                    "GuardBreakHit",
                    120f,
                    55f,
                    0.14f,
                    0.35f,
                    ProceduralActionAudioMixGroup.Impact,
                    0.22f,
                    0.7f,
                    1.4f,
                    14f,
                    ActionAudioPriorityGuardBreak,
                    0.16f)
                : ProceduralActionAudioPlan.None;
        }

        public static ProceduralActionAudioPlan ResolveEnemyResponseCue(AttackDefinitionSO attackDefinition)
        {
            if (attackDefinition == null)
            {
                return ProceduralActionAudioPlan.None;
            }

            if (attackDefinition.EnemyTargetResponse == EnemyTargetResponseType.AntiAir)
            {
                return new ProceduralActionAudioPlan(
                    "SkyHook",
                    950f,
                    1500f,
                    0.11f,
                    0.28f,
                    ProceduralActionAudioMixGroup.BossResponse,
                    0.24f,
                    0.82f,
                    2f,
                    20f,
                    ActionAudioPriorityHeavyRead,
                    0.12f);
            }

            if (attackDefinition.EnemyTargetResponse == EnemyTargetResponseType.ChaseRoll)
            {
                return new ProceduralActionAudioPlan(
                    "PursuitSlam",
                    310f,
                    95f,
                    0.13f,
                    0.32f,
                    ProceduralActionAudioMixGroup.BossResponse,
                    0.24f,
                    0.85f,
                    2f,
                    18f,
                    ActionAudioPriorityHeavyRead,
                    0.12f);
            }

            return attackDefinition.BreaksGuard
                ? new ProceduralActionAudioPlan(
                    "EnemyGuardBreak",
                    180f,
                    70f,
                    0.13f,
                    0.3f,
                    ProceduralActionAudioMixGroup.BossResponse,
                    0.24f,
                    0.8f,
                    2f,
                    18f,
                    ActionAudioPriorityGuardBreak,
                    0.14f)
                : ProceduralActionAudioPlan.None;
        }

        public static bool CanPlayActionCue(
            ProceduralActionAudioPlan plan,
            float previousPlayTimeSeconds,
            float currentTimeSeconds)
        {
            if (!plan.HasAudio)
            {
                return false;
            }

            if (previousPlayTimeSeconds < 0f)
            {
                return true;
            }

            return Mathf.Max(0f, currentTimeSeconds - previousPlayTimeSeconds) >= plan.CooldownSeconds;
        }

        public static bool CanPlayActionCue(
            ProceduralActionAudioPlan plan,
            float previousPlayTimeSeconds,
            float currentTimeSeconds,
            int activeDominantPriority,
            float activeDominantUntilSeconds)
        {
            return CanPlayActionCue(plan, previousPlayTimeSeconds, currentTimeSeconds)
                && CanPassActionCueDominance(
                    plan,
                    activeDominantPriority,
                    activeDominantUntilSeconds,
                    currentTimeSeconds);
        }

        public static bool CanPassActionCueDominance(
            ProceduralActionAudioPlan plan,
            int activeDominantPriority,
            float activeDominantUntilSeconds,
            float currentTimeSeconds)
        {
            if (!plan.HasAudio)
            {
                return false;
            }

            if (currentTimeSeconds >= activeDominantUntilSeconds)
            {
                return true;
            }

            return plan.Priority >= Mathf.Max(0, activeDominantPriority);
        }

        public static ProceduralActionAudioDecision EvaluateActionCueDecision(
            ProceduralActionAudioPlan plan,
            float previousPlayTimeSeconds,
            float currentTimeSeconds,
            int activeDominantPriority,
            float activeDominantUntilSeconds,
            float resolvedVolume,
            bool isBatchMode = false)
        {
            if (!plan.HasAudio)
            {
                return new ProceduralActionAudioDecision(
                    ProceduralActionAudioDecisionKind.NoAudio,
                    plan,
                    currentTimeSeconds);
            }

            if (isBatchMode)
            {
                return new ProceduralActionAudioDecision(
                    ProceduralActionAudioDecisionKind.BatchMode,
                    plan,
                    currentTimeSeconds);
            }

            if (resolvedVolume <= 0f)
            {
                return new ProceduralActionAudioDecision(
                    ProceduralActionAudioDecisionKind.Muted,
                    plan,
                    currentTimeSeconds);
            }

            if (previousPlayTimeSeconds >= 0f)
            {
                float nextAllowedTime = previousPlayTimeSeconds + plan.CooldownSeconds;

                if (currentTimeSeconds < nextAllowedTime)
                {
                    return new ProceduralActionAudioDecision(
                        ProceduralActionAudioDecisionKind.Cooldown,
                        plan,
                        currentTimeSeconds,
                        nextAllowedTime);
                }
            }

            if (!CanPassActionCueDominance(
                plan,
                activeDominantPriority,
                activeDominantUntilSeconds,
                currentTimeSeconds))
            {
                return new ProceduralActionAudioDecision(
                    ProceduralActionAudioDecisionKind.DominanceBlocked,
                    plan,
                    currentTimeSeconds,
                    activeDominantUntilSeconds,
                    activeDominantPriority);
            }

            return new ProceduralActionAudioDecision(
                ProceduralActionAudioDecisionKind.Played,
                plan,
                currentTimeSeconds,
                currentTimeSeconds + plan.DominanceSeconds,
                activeDominantPriority);
        }

        public static int ResolveUnityAudioSourcePriority(int actionCuePriority)
        {
            if (actionCuePriority >= ActionAudioPriorityGuardBreak)
            {
                return 64;
            }

            if (actionCuePriority >= ActionAudioPriorityHeavyRead)
            {
                return 80;
            }

            if (actionCuePriority >= ActionAudioPrioritySwordArt)
            {
                return 96;
            }

            if (actionCuePriority >= ActionAudioPriorityMovement)
            {
                return 128;
            }

            return 160;
        }

        public static bool TryPlayActionCue(Vector3 position, ProceduralActionAudioPlan plan)
        {
            float currentTimeSeconds = Time.unscaledTime;
            float resolvedVolume = ResolveSfxVolume(plan.Volume);

            if (!TryReserveActionCue(
                plan,
                currentTimeSeconds,
                resolvedVolume,
                Application.isBatchMode,
                out ProceduralActionAudioDecision decision))
            {
                return false;
            }

            PlayChirp(
                position,
                plan.StartFrequency,
                plan.EndFrequency,
                plan.DurationSeconds,
                plan.Volume,
                plan.SpatialBlend,
                plan.MinDistance,
                plan.MaxDistance,
                plan.Priority);
            return true;
        }

        public static void ResetActionCueStateForTests()
        {
            LastActionCueTimes.Clear();
            activeDominantActionCuePriority = 0;
            activeDominantActionCueUntil = 0f;
            LastActionCueDecision = ProceduralActionAudioDecision.None;
        }

        public static void PlayChirp(
            Vector3 position,
            float startFrequency,
            float endFrequency,
            float durationSeconds,
            float volume)
        {
            PlayChirp(
                position,
                startFrequency,
                endFrequency,
                durationSeconds,
                volume,
                1f,
                1f,
                18f);
        }

        private static void PlayChirp(
            Vector3 position,
            float startFrequency,
            float endFrequency,
            float durationSeconds,
            float volume,
            float spatialBlend,
            float minDistance,
            float maxDistance,
            int priority = 0)
        {
            float resolvedVolume = ResolveSfxVolume(volume);

            if (Application.isBatchMode || resolvedVolume <= 0f || durationSeconds <= 0f)
            {
                return;
            }

            AudioClip clip = GetOrCreateChirpClip(startFrequency, endFrequency, durationSeconds);

            if (clip != null)
            {
                GameObject sourceObject = new GameObject("ProceduralChirp_" + clip.name)
                {
                    hideFlags = HideFlags.HideInHierarchy
                };
                sourceObject.transform.position = position;

                AudioSource source = sourceObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.volume = resolvedVolume;
                source.spatialBlend = Mathf.Clamp01(spatialBlend);
                source.minDistance = Mathf.Max(0.01f, minDistance);
                source.maxDistance = Mathf.Max(source.minDistance + 0.01f, maxDistance);
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.priority = ResolveUnityAudioSourcePriority(priority);
                source.playOnAwake = false;
                source.Play();
                Object.Destroy(sourceObject, clip.length + 0.05f);
            }
        }

        private static bool TryReserveActionCue(
            ProceduralActionAudioPlan plan,
            float currentTimeSeconds,
            float resolvedVolume,
            bool isBatchMode,
            out ProceduralActionAudioDecision decision)
        {
            if (!plan.HasAudio)
            {
                decision = EvaluateActionCueDecision(
                    plan,
                    -1f,
                    currentTimeSeconds,
                    activeDominantActionCuePriority,
                    activeDominantActionCueUntil,
                    resolvedVolume,
                    isBatchMode);
                LastActionCueDecision = decision;
                return false;
            }

            if (!LastActionCueTimes.TryGetValue(plan.CueId, out float previousPlayTimeSeconds))
            {
                previousPlayTimeSeconds = -1f;
            }

            decision = EvaluateActionCueDecision(
                plan,
                previousPlayTimeSeconds,
                currentTimeSeconds,
                activeDominantActionCuePriority,
                activeDominantActionCueUntil,
                resolvedVolume,
                isBatchMode);
            LastActionCueDecision = decision;

            if (!decision.ShouldPlay)
            {
                return false;
            }

            LastActionCueTimes[plan.CueId] = Mathf.Max(0f, currentTimeSeconds);
            ReserveDominantActionCue(plan, currentTimeSeconds);
            return true;
        }

        private static void ReserveDominantActionCue(ProceduralActionAudioPlan plan, float currentTimeSeconds)
        {
            if (plan.Priority <= 0 || plan.DominanceSeconds <= 0f)
            {
                return;
            }

            float nextDominantUntil = currentTimeSeconds + plan.DominanceSeconds;

            if (currentTimeSeconds >= activeDominantActionCueUntil
                || plan.Priority >= activeDominantActionCuePriority)
            {
                activeDominantActionCuePriority = plan.Priority;
                activeDominantActionCueUntil = nextDominantUntil;
            }
        }

        private static AudioClip GetOrCreateChirpClip(float startFrequency, float endFrequency, float durationSeconds)
        {
            float clampedDuration = Mathf.Clamp(durationSeconds, 0.02f, 1f);
            float clampedStart = Mathf.Clamp(startFrequency, 40f, 4000f);
            float clampedEnd = Mathf.Clamp(endFrequency, 40f, 4000f);
            string cacheKey =
                Mathf.RoundToInt(clampedStart).ToString() + "_" +
                Mathf.RoundToInt(clampedEnd).ToString() + "_" +
                Mathf.RoundToInt(clampedDuration * 1000f).ToString();

            if (ClipCache.TryGetValue(cacheKey, out AudioClip existingClip))
            {
                return existingClip;
            }

            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(clampedDuration * SampleRate));
            float[] samples = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount > 1 ? i / (float)(sampleCount - 1) : 0f;
                float frequency = Mathf.Lerp(clampedStart, clampedEnd, t);
                float amplitude = Mathf.Pow(1f - t, 2.2f);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                samples[i] = Mathf.Sin(phase) * amplitude * 0.45f;
            }

            AudioClip clip = AudioClip.Create(
                "Chirp_" + cacheKey,
                sampleCount,
                1,
                SampleRate,
                false);
            clip.SetData(samples, 0);
            ClipCache[cacheKey] = clip;
            return clip;
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
