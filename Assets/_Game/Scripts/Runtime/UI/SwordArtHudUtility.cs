using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    public enum SwordArtHudMode
    {
        Hidden = 0,
        Current = 1,
        CancelWindow = 2,
        Preview = 3,
        Recent = 4
    }

    public readonly struct SwordArtHudPlan
    {
        public SwordArtHudPlan(
            SwordArtHudMode mode,
            string title,
            string status,
            string detail,
            string inputHint,
            float progress01)
        {
            Mode = mode;
            Title = title ?? string.Empty;
            Status = status ?? string.Empty;
            Detail = detail ?? string.Empty;
            InputHint = inputHint ?? string.Empty;
            Progress01 = Mathf.Clamp01(progress01);
        }

        public SwordArtHudMode Mode { get; }

        public bool IsVisible => Mode != SwordArtHudMode.Hidden && !string.IsNullOrWhiteSpace(Title);

        public string Title { get; }

        public string Status { get; }

        public string Detail { get; }

        public string InputHint { get; }

        public float Progress01 { get; }

        public static SwordArtHudPlan Hidden => new SwordArtHudPlan(
            SwordArtHudMode.Hidden,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0f);
    }

    public static class SwordArtHudUtility
    {
        public static SwordArtHudPlan Build(PlayerCombatController combatController)
        {
            if (combatController == null)
            {
                return SwordArtHudPlan.Hidden;
            }

            if (combatController.HasCurrentSwordArt)
            {
                return BuildPlan(
                    SwordArtHudMode.Current,
                    combatController.CurrentSwordArt,
                    combatController.CurrentSwordArtAttack,
                    "EXECUTING",
                    ResolveExecutionProgress(combatController));
            }

            if (combatController.TryGetBufferedSwordArtCancelWindowStatus(
                    out SwordArtDefinitionSO bufferedSwordArt,
                    out AttackDefinitionSO bufferedAttack,
                    out bool isCancelOpen,
                    out float secondsUntilCancelOpen))
            {
                string status = isCancelOpen ? "CHAIN OPEN" : $"CHAIN {secondsUntilCancelOpen:0.00}s";
                return BuildPlan(
                    SwordArtHudMode.CancelWindow,
                    bufferedSwordArt,
                    bufferedAttack,
                    status,
                    isCancelOpen ? 1f : 0f);
            }

            if (combatController.HasSwordArtPreview)
            {
                return BuildPlan(
                    SwordArtHudMode.Preview,
                    combatController.PreviewSwordArt,
                    combatController.PreviewSwordArtAttack,
                    "READY",
                    1f);
            }

            if (combatController.HasRecentSwordArt)
            {
                return BuildPlan(
                    SwordArtHudMode.Recent,
                    combatController.RecentSwordArt,
                    combatController.RecentSwordArtAttack,
                    "RECENT",
                    Mathf.Clamp01(combatController.RecentSwordArtDisplayRemainingSeconds / 1.2f));
            }

            return SwordArtHudPlan.Hidden;
        }

        private static SwordArtHudPlan BuildPlan(
            SwordArtHudMode mode,
            SwordArtDefinitionSO swordArt,
            AttackDefinitionSO attackDefinition,
            string status,
            float progress01)
        {
            string title = ResolveSwordArtName(swordArt, attackDefinition);

            if (string.IsNullOrWhiteSpace(title))
            {
                return SwordArtHudPlan.Hidden;
            }

            return new SwordArtHudPlan(
                mode,
                title,
                status,
                ResolveRoleLine(title),
                ResolveInputHint(title),
                progress01);
        }

        private static float ResolveExecutionProgress(PlayerCombatController combatController)
        {
            if (combatController == null || !combatController.HasCurrentAttackTiming)
            {
                return 0f;
            }

            return combatController.CurrentAttackDurationSeconds > 0f
                ? combatController.CurrentAttackElapsedSeconds / combatController.CurrentAttackDurationSeconds
                : 0f;
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

        private static string ResolveRoleLine(string displayName)
        {
            return displayName switch
            {
                "Cross Step" => "Roll counter",
                "Falling Star" => "Aerial slam",
                "Iron Gate Break" => "Guard pressure",
                "Moon Sever" => "Air dodge slash",
                "Rising Cleave" => "Aerial chase",
                "Sidewind Cut" => "Dodge flank",
                _ => "Committed SwordArt"
            };
        }

        private static string ResolveInputHint(string displayName)
        {
            return displayName switch
            {
                "Cross Step" => "Roll + Light",
                "Falling Star" => "Air + Heavy",
                "Iron Gate Break" => "Guard/Heavy + Heavy",
                "Moon Sever" => "Air Dodge + Light",
                "Rising Cleave" => "Forward/Air + Heavy",
                "Sidewind Cut" => "Dodge Side + Light",
                _ => "Timed input"
            };
        }
    }
}
