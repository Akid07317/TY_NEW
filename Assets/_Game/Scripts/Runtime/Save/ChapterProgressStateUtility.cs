using System;
using System.Collections.Generic;

namespace CampusRPG.Save
{
    public readonly struct ChapterProgressCompletionResult
    {
        public ChapterProgressCompletionResult(bool changed, bool keyItemRegistered, bool chapterCompletedNow)
        {
            Changed = changed;
            KeyItemRegistered = keyItemRegistered;
            ChapterCompletedNow = chapterCompletedNow;
        }

        public bool Changed { get; }

        public bool KeyItemRegistered { get; }

        public bool ChapterCompletedNow { get; }
    }

    public static class ChapterProgressStateUtility
    {
        public static bool TryEnterArea(HashSet<string> visitedAreaIds, string areaId, ref string currentAreaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
            {
                return false;
            }

            bool visitedAdded = visitedAreaIds != null && visitedAreaIds.Add(areaId);
            bool areaChanged = !string.Equals(currentAreaId, areaId, StringComparison.Ordinal);
            currentAreaId = areaId;
            return visitedAdded || areaChanged;
        }

        public static bool TryRegisterId(HashSet<string> values, string value)
        {
            return values != null
                && !string.IsNullOrWhiteSpace(value)
                && values.Add(value);
        }

        public static bool Contains(HashSet<string> values, string value)
        {
            return values != null
                && !string.IsNullOrWhiteSpace(value)
                && values.Contains(value);
        }

        public static bool MeetsRequirements(
            HashSet<string> visitedAreaIds,
            HashSet<string> clearedEncounterIds,
            HashSet<string> keyItemIds,
            string requiredAreaId,
            string requiredEncounterId,
            string requiredKeyItemId)
        {
            if (!string.IsNullOrWhiteSpace(requiredAreaId) && !Contains(visitedAreaIds, requiredAreaId))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredEncounterId) && !Contains(clearedEncounterIds, requiredEncounterId))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredKeyItemId) && !Contains(keyItemIds, requiredKeyItemId))
            {
                return false;
            }

            return true;
        }

        public static ChapterProgressCompletionResult CompleteChapter(
            HashSet<string> keyItemIds,
            string completionKeyItemId,
            ref bool chapterCompleted)
        {
            bool keyItemRegistered = TryRegisterId(keyItemIds, completionKeyItemId);
            bool chapterCompletedNow = false;

            if (!chapterCompleted)
            {
                chapterCompleted = true;
                chapterCompletedNow = true;
            }

            return new ChapterProgressCompletionResult(
                keyItemRegistered || chapterCompletedNow,
                keyItemRegistered,
                chapterCompletedNow);
        }

        public static void ApplySnapshot(
            ChapterProgressSnapshot snapshot,
            ref string currentAreaId,
            ref bool chapterCompleted,
            HashSet<string> visitedAreaIds,
            HashSet<string> keyItemIds,
            HashSet<string> clearedEncounterIds)
        {
            currentAreaId = snapshot.CurrentAreaId;
            chapterCompleted = snapshot.ChapterCompleted;
            ReplaceSet(visitedAreaIds, snapshot.VisitedAreaIds);
            ReplaceSet(keyItemIds, snapshot.KeyItemIds);
            ReplaceSet(clearedEncounterIds, snapshot.ClearedEncounterIds);
        }

        private static void ReplaceSet(HashSet<string> target, string[] values)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();

            if (values == null)
            {
                return;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    target.Add(values[i]);
                }
            }
        }
    }
}
