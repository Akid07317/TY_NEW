using CampusRPG.Save;

namespace CampusRPG.Interaction
{
    public readonly struct EncounterProgressPlan
    {
        public EncounterProgressPlan(bool shouldApplyClearedState, bool shouldKeepActiveState, bool shouldResetUncleared, bool shouldActivateFromStart)
        {
            ShouldApplyClearedState = shouldApplyClearedState;
            ShouldKeepActiveState = shouldKeepActiveState;
            ShouldResetUncleared = shouldResetUncleared;
            ShouldActivateFromStart = shouldActivateFromStart;
        }

        public bool ShouldApplyClearedState { get; }

        public bool ShouldKeepActiveState { get; }

        public bool ShouldResetUncleared { get; }

        public bool ShouldActivateFromStart { get; }
    }

    public static class EncounterControllerUtility
    {
        public static EncounterProgressPlan BuildProgressPlan(
            bool hasClearedProgress,
            bool isActive,
            bool startActive,
            bool resetUncleared)
        {
            if (hasClearedProgress)
            {
                return new EncounterProgressPlan(
                    shouldApplyClearedState: true,
                    shouldKeepActiveState: false,
                    shouldResetUncleared: false,
                    shouldActivateFromStart: false);
            }

            if (isActive)
            {
                return new EncounterProgressPlan(
                    shouldApplyClearedState: false,
                    shouldKeepActiveState: true,
                    shouldResetUncleared: false,
                    shouldActivateFromStart: false);
            }

            return new EncounterProgressPlan(
                shouldApplyClearedState: false,
                shouldKeepActiveState: false,
                shouldResetUncleared: resetUncleared,
                shouldActivateFromStart: startActive);
        }

        public static bool AreAllMembersDefeated(EnemyEncounterMember[] members)
        {
            if (members == null || members.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] != null && !members[i].IsDefeated)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasClearedProgress(string encounterId, ChapterProgressService chapterProgressService)
        {
            return !string.IsNullOrWhiteSpace(encounterId)
                && chapterProgressService != null
                && chapterProgressService.IsEncounterCleared(encounterId);
        }
    }
}
