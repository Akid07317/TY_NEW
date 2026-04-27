using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;

namespace CampusRPG.UI
{
    public static class CombatDebugHudActionFeedbackUtility
    {
        public static string BuildPlayerActionFeedbackLine(
            PlayerStateMachine stateMachine,
            PlayerCombatController combatController)
        {
            if (stateMachine != null && stateMachine.CurrentHitReactionType == PlayerHitReactionType.GuardBreak)
            {
                return "Action Cue: Guard Break - recover, dodge slow heavies";
            }

            if (combatController != null && combatController.HasCurrentSwordArt)
            {
                return BuildSwordArtLine(combatController.CurrentSwordArt, combatController.CurrentSwordArtAttack, ready: false);
            }

            if (combatController != null && combatController.HasSwordArtPreview)
            {
                return BuildSwordArtLine(combatController.PreviewSwordArt, combatController.PreviewSwordArtAttack, ready: true);
            }

            return BuildEvasiveActionLine(stateMachine);
        }

        public static string BuildBossResponseFeedbackLine(
            EnemyBrain bossEnemy,
            BossTelegraphStyleSO telegraphStyle = null,
            bool compact = false)
        {
            if (!BossPresentationRules.IsBossEligible(bossEnemy)
                || bossEnemy.StateMachine == null
                || bossEnemy.StateMachine.CurrentStateName != nameof(EnemyAttackState))
            {
                return string.Empty;
            }

            AttackDefinitionSO attack = BossAttackPreviewUtility.PreviewCurrentAttack(bossEnemy);

            if (attack == null
                || (attack.EnemyTargetResponse != EnemyTargetResponseType.AntiAir
                    && attack.EnemyTargetResponse != EnemyTargetResponseType.ChaseRoll
                    && !attack.BreaksGuard))
            {
                return string.Empty;
            }

            BossAttackCuePlan cuePlan = BossAttackCuePlanner.Build(
                bossEnemy,
                telegraphStyle,
                "Incoming Attack",
                0.25f);

            if (compact)
            {
                return BuildCompactBossResponseLine(cuePlan);
            }

            return string.IsNullOrWhiteSpace(cuePlan.ResponseHint)
                ? $"Boss Cue: {cuePlan.CueLabel} - {cuePlan.AttackName}"
                : $"Boss Cue: {cuePlan.CueLabel} - {cuePlan.AttackName} ({cuePlan.ResponseHint})";
        }

        public static string BuildActionAudioFeedbackLine(
            ProceduralActionAudioDecision decision,
            float currentTimeSeconds,
            float maxAgeSeconds = 1f)
        {
            if (!decision.IsVisible
                || maxAgeSeconds <= 0f
                || currentTimeSeconds - decision.CurrentTimeSeconds > maxAgeSeconds)
            {
                return string.Empty;
            }

            string cueLabel = ResolveCompactCueLabel(decision.CueId);
            string mixLabel = decision.MixGroup == ProceduralActionAudioMixGroup.None
                ? string.Empty
                : $" {decision.MixGroup}";

            return decision.Kind switch
            {
                ProceduralActionAudioDecisionKind.Played =>
                    $"SFX: {cueLabel} play p{decision.Priority}{mixLabel}",
                ProceduralActionAudioDecisionKind.Cooldown =>
                    $"SFX: {cueLabel} cd {decision.SecondsRemaining:0.00}s",
                ProceduralActionAudioDecisionKind.DominanceBlocked =>
                    $"SFX: {cueLabel} held p{decision.ActiveDominantPriority} {decision.SecondsRemaining:0.00}s",
                ProceduralActionAudioDecisionKind.Muted => $"SFX: {cueLabel} muted",
                ProceduralActionAudioDecisionKind.BatchMode => $"SFX: {cueLabel} batch skip",
                _ => string.Empty
            };
        }

        private static string BuildSwordArtLine(
            SwordArtDefinitionSO swordArt,
            AttackDefinitionSO attackDefinition,
            bool ready)
        {
            string displayName = ResolveSwordArtName(swordArt, attackDefinition);
            string statusSuffix = ready ? " Ready" : string.Empty;

            return displayName switch
            {
                "Cross Step" => $"Action Cue: Cross Step{statusSuffix} - roll counter",
                "Falling Star" => $"Action Cue: Falling Star{statusSuffix} - aerial slam",
                "Iron Gate Break" => $"Action Cue: Iron Gate Break{statusSuffix} - guard pressure",
                "Moon Sever" => $"Action Cue: Moon Sever{statusSuffix} - air dodge slash",
                "Rising Cleave" => $"Action Cue: Rising Cleave{statusSuffix} - aerial chase",
                "Sidewind Cut" => $"Action Cue: Sidewind Cut{statusSuffix} - dodge flank",
                _ => string.IsNullOrWhiteSpace(displayName)
                    ? string.Empty
                    : $"Action Cue: {displayName}{statusSuffix} - committed art"
            };
        }

        private static string BuildEvasiveActionLine(PlayerStateMachine stateMachine)
        {
            if (stateMachine == null || stateMachine.CurrentState is not PlayerDodgeState)
            {
                return string.Empty;
            }

            return stateMachine.CurrentEvasiveActionType switch
            {
                PlayerEvasiveActionType.CombatRoll => "Action Cue: Combat Roll - commit, then counter",
                PlayerEvasiveActionType.AirDodge => "Action Cue: Air Dodge - one aerial follow-up",
                _ => "Action Cue: Dodge - short follow-up window"
            };
        }

        private static string ResolveSwordArtName(
            SwordArtDefinitionSO swordArt,
            AttackDefinitionSO attackDefinition)
        {
            if (swordArt != null && !string.IsNullOrWhiteSpace(swordArt.DisplayName))
            {
                return swordArt.DisplayName;
            }

            return attackDefinition != null ? attackDefinition.DisplayName : string.Empty;
        }

        private static string ResolveCompactCueLabel(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return string.Empty;
            }

            return cueId.Trim() switch
            {
                "CombatRoll" => "Roll",
                "AirDodge" => "AirDodge",
                "FallingStar" => "FallingStar",
                "IronGateBreak" => "IronGate",
                "GuardBreakAttack" => "GuardBreak",
                "GuardBreakHit" => "GuardBreak",
                "EnemyGuardBreak" => "EnemyGuardBreak",
                "PursuitSlam" => "PursuitSlam",
                "SkyHook" => "SkyHook",
                _ => cueId.Trim().Replace(" ", string.Empty)
            };
        }

        private static string BuildCompactBossResponseLine(BossAttackCuePlan cuePlan)
        {
            string cueLabel = ResolveCompactBossCueLabel(cuePlan.CueLabel);
            string attackName = ResolveCompactAttackName(cuePlan.AttackName);
            string hint = ResolveCompactResponseHint(cuePlan.ResponseHint);

            if (string.IsNullOrWhiteSpace(hint))
            {
                return $"Boss: {cueLabel} {attackName}";
            }

            return $"Boss: {cueLabel} {attackName} - {hint}";
        }

        private static string ResolveCompactBossCueLabel(string cueLabel)
        {
            return cueLabel switch
            {
                "Anti-Air Incoming" => "AntiAir",
                "Roll Catch Incoming" => "RollCatch",
                "Guard Break Incoming" => "GuardBreak",
                "Line Shot Incoming" => "LineShot",
                "Arc Shot Incoming" => "ArcShot",
                _ => RemoveWhitespaceAndClamp(cueLabel, 12)
            };
        }

        private static string ResolveCompactAttackName(string attackName)
        {
            return attackName switch
            {
                "Pursuit Slam" => "PursuitSlam",
                "Sky Hook" => "SkyHook",
                "Gate Slam" => "GateSlam",
                _ => RemoveWhitespaceAndClamp(attackName, 14)
            };
        }

        private static string ResolveCompactResponseHint(string responseHint)
        {
            return responseHint switch
            {
                "Land or guard; avoid air hang" => "land/guard",
                "Delay dodge; lane catches rolls" => "delay dodge",
                "Dodge heavy; guard breaks" => "dodge; guard breaks",
                "Sidestep line shot" => "sidestep",
                "Leave marked impact" => "leave mark",
                _ => RemoveWhitespaceAndClamp(responseHint, 16)
            };
        }

        private static string RemoveWhitespaceAndClamp(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    builder.Append(value[i]);
                }
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            return builder.Length > maxLength ? builder.ToString(0, maxLength) : builder.ToString();
        }
    }

    public static class CombatDebugHudAttackTimingUtility
    {
        private const int CompactLineMaxLength = 48;

        public static string BuildAttackTimingLine(PlayerCombatController combatController, bool compact = false)
        {
            if (combatController == null
                || !combatController.HasCurrentAttackTiming
                || combatController.CurrentAttackDefinition == null)
            {
                return string.Empty;
            }

            return BuildAttackTimingLine(
                combatController.CurrentAttackDefinition,
                combatController.CurrentAttackElapsedSeconds,
                combatController.CurrentAttackDurationSeconds,
                compact);
        }

        public static string BuildAttackTimingLine(
            AttackDefinitionSO attackDefinition,
            float elapsedSeconds,
            float durationSeconds,
            bool compact = false)
        {
            return BuildAttackTimingLine(
                attackDefinition,
                elapsedSeconds,
                durationSeconds,
                compact,
                "Attack Phase",
                "Atk");
        }

        public static string BuildTargetAttackTimingLine(
            AttackDefinitionSO attackDefinition,
            EnemyAttackPresentationPhase phase,
            float phaseProgress,
            bool compact = false)
        {
            if (attackDefinition == null || phase == EnemyAttackPresentationPhase.None)
            {
                return string.Empty;
            }

            float startupSeconds = UnityEngine.Mathf.Max(0f, attackDefinition.StartupSeconds);
            float activeSeconds = UnityEngine.Mathf.Max(0f, attackDefinition.ActiveSeconds);
            float recoverySeconds = UnityEngine.Mathf.Max(0f, attackDefinition.RecoverySeconds);
            float progress = UnityEngine.Mathf.Clamp01(phaseProgress);
            float elapsedSeconds = phase switch
            {
                EnemyAttackPresentationPhase.Startup => startupSeconds * progress,
                EnemyAttackPresentationPhase.Advance => startupSeconds + (activeSeconds * progress),
                EnemyAttackPresentationPhase.Recovery => startupSeconds + activeSeconds + (recoverySeconds * progress),
                _ => 0f
            };

            return BuildAttackTimingLine(
                attackDefinition,
                elapsedSeconds,
                startupSeconds + activeSeconds + recoverySeconds,
                compact,
                "Target Attack",
                "Tgt Atk");
        }

        private static string BuildAttackTimingLine(
            AttackDefinitionSO attackDefinition,
            float elapsedSeconds,
            float durationSeconds,
            bool compact,
            string fullLabel,
            string compactLabel)
        {
            if (attackDefinition == null)
            {
                return string.Empty;
            }

            float startupSeconds = UnityEngine.Mathf.Max(0f, attackDefinition.StartupSeconds);
            float activeSeconds = UnityEngine.Mathf.Max(0f, attackDefinition.ActiveSeconds);
            float hitStartSeconds = startupSeconds;
            float hitEndSeconds = hitStartSeconds + activeSeconds;
            float resolvedDurationSeconds = durationSeconds > 0f
                ? durationSeconds
                : hitEndSeconds + UnityEngine.Mathf.Max(0f, attackDefinition.RecoverySeconds);
            float elapsed = UnityEngine.Mathf.Max(0f, elapsedSeconds);
            string phase = ResolvePhase(elapsed, hitStartSeconds, hitEndSeconds, resolvedDurationSeconds);
            string hitWindow = attackDefinition.HitboxActivationMode == AttackHitboxActivationMode.AnimationEvent
                ? "event hit"
                : $"hit {hitStartSeconds:0.00}-{hitEndSeconds:0.00}s";

            if (compact)
            {
                string compactHitWindow = attackDefinition.HitboxActivationMode == AttackHitboxActivationMode.AnimationEvent
                    ? "event"
                    : $"hit {FormatCompactSeconds(hitStartSeconds)}-{FormatCompactSeconds(hitEndSeconds)}";
                string compactPhase = ResolveCompactPhase(phase);
                string compactTail = $"{compactPhase} {elapsed:0.00}/{resolvedDurationSeconds:0.00} {compactHitWindow}";
                string compactAttackName = BuildCompactAttackNameForTimingLine(
                    ResolveCompactAttackName(attackDefinition),
                    compactLabel,
                    compactTail);
                string compactPrefix = $"{compactLabel}: ";

                return string.IsNullOrEmpty(compactAttackName)
                    ? $"{compactPrefix}{compactTail}"
                    : $"{compactPrefix}{compactAttackName} {compactTail}";
            }

            return $"{fullLabel}: {ResolveAttackName(attackDefinition)} {phase} {elapsed:0.00}/{resolvedDurationSeconds:0.00}s ({hitWindow})";
        }

        private static string ResolvePhase(
            float elapsedSeconds,
            float hitStartSeconds,
            float hitEndSeconds,
            float durationSeconds)
        {
            if (elapsedSeconds < hitStartSeconds)
            {
                return "Startup";
            }

            if (elapsedSeconds < hitEndSeconds)
            {
                return "Active";
            }

            return elapsedSeconds < durationSeconds ? "Recovery" : "Done";
        }

        private static string ResolveAttackName(AttackDefinitionSO attackDefinition)
        {
            if (!string.IsNullOrWhiteSpace(attackDefinition.DisplayName))
            {
                return attackDefinition.DisplayName;
            }

            return string.IsNullOrWhiteSpace(attackDefinition.AttackId)
                ? "Attack"
                : attackDefinition.AttackId;
        }

        private static string ResolveCompactPhase(string phase)
        {
            return phase switch
            {
                "Startup" => "Start",
                "Active" => "Act",
                "Recovery" => "Rec",
                _ => phase
            };
        }

        private static string ResolveCompactAttackName(AttackDefinitionSO attackDefinition)
        {
            string attackName = ResolveAttackName(attackDefinition);

            return attackName switch
            {
                "Cross Step" => "CrossStep",
                "Falling Star" => "FallingStar",
                "Iron Gate Break" => "IronGate",
                "Moon Sever" => "MoonSever",
                "Rising Cleave" => "Rising",
                "Sidewind Cut" => "Sidewind",
                _ => RemoveWhitespaceAndClamp(attackName, 32)
            };
        }

        private static string BuildCompactAttackNameForTimingLine(
            string compactAttackName,
            string compactLabel,
            string compactTail)
        {
            string safeName = string.IsNullOrWhiteSpace(compactAttackName) ? "Attack" : compactAttackName;
            int maxNameLength = CompactLineMaxLength - $"{compactLabel}: ".Length - 1 - compactTail.Length;

            if (maxNameLength <= 0)
            {
                return string.Empty;
            }

            if (safeName.Length <= maxNameLength)
            {
                return safeName;
            }

            if (maxNameLength <= 3)
            {
                return safeName.Substring(0, maxNameLength);
            }

            return safeName.Substring(0, maxNameLength - 3) + "...";
        }

        private static string RemoveWhitespaceAndClamp(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Attack";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    builder.Append(value[i]);
                }
            }

            if (builder.Length == 0)
            {
                return "Attack";
            }

            return builder.Length > maxLength ? builder.ToString(0, maxLength) : builder.ToString();
        }

        private static string FormatCompactSeconds(float seconds)
        {
            string formatted = UnityEngine.Mathf.Max(0f, seconds).ToString("0.00");
            return formatted.StartsWith("0.", System.StringComparison.Ordinal) ? formatted.Substring(1) : formatted;
        }
    }
}
