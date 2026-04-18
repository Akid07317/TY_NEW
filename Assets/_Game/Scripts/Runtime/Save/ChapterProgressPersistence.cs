using System;
using System.Collections.Generic;

namespace CampusRPG.Save
{
    public static class ChapterProgressPersistence
    {
        public static ChapterProgressSnapshot CreateSnapshot(
            ChapterProgressionSO progression,
            string currentAreaId,
            HashSet<string> visitedAreaIds,
            HashSet<string> keyItemIds,
            HashSet<string> clearedEncounterIds,
            bool chapterCompleted)
        {
            string normalizedCurrentAreaId = NormalizeCurrentAreaId(progression, currentAreaId);
            HashSet<string> normalizedVisitedAreaIds = CloneSet(visitedAreaIds);
            EnsureDefaultVisitedAreas(progression, normalizedVisitedAreaIds, normalizedCurrentAreaId);

            return new ChapterProgressSnapshot(
                normalizedCurrentAreaId,
                ToSortedArray(normalizedVisitedAreaIds),
                ToSortedArray(keyItemIds),
                ToSortedArray(clearedEncounterIds),
                chapterCompleted);
        }

        public static ChapterProgressSnapshot RestoreSnapshot(ChapterProgressionSO progression, ChapterSaveData saveData)
        {
            HashSet<string> restoredVisitedAreaIds = RestoreSet(saveData != null ? saveData.visitedAreaIds : null);
            string normalizedCurrentAreaId = NormalizeCurrentAreaId(
                progression,
                saveData != null ? saveData.currentAreaId : string.Empty);

            EnsureDefaultVisitedAreas(progression, restoredVisitedAreaIds, normalizedCurrentAreaId);

            return new ChapterProgressSnapshot(
                normalizedCurrentAreaId,
                ToSortedArray(restoredVisitedAreaIds),
                ToSortedArray(RestoreSet(saveData != null ? saveData.keyItemIds : null)),
                ToSortedArray(RestoreSet(saveData != null ? saveData.clearedEncounterIds : null)),
                saveData != null && saveData.chapterCompleted);
        }

        private static void EnsureDefaultVisitedAreas(
            ChapterProgressionSO progression,
            HashSet<string> visitedAreaIds,
            string currentAreaId)
        {
            if (visitedAreaIds == null)
            {
                return;
            }

            string defaultAreaId = progression != null ? progression.GetFirstAreaId() : string.Empty;

            if (!string.IsNullOrWhiteSpace(defaultAreaId))
            {
                visitedAreaIds.Add(defaultAreaId);
            }

            if (!string.IsNullOrWhiteSpace(currentAreaId))
            {
                visitedAreaIds.Add(currentAreaId);
            }
        }

        private static string NormalizeCurrentAreaId(ChapterProgressionSO progression, string currentAreaId)
        {
            if (!string.IsNullOrWhiteSpace(currentAreaId))
            {
                if (progression == null || progression.ContainsArea(currentAreaId))
                {
                    return currentAreaId;
                }
            }

            return progression != null ? progression.GetFirstAreaId() : string.Empty;
        }

        private static HashSet<string> CloneSet(HashSet<string> values)
        {
            return values != null
                ? new HashSet<string>(values, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }

        private static HashSet<string> RestoreSet(string[] values)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);

            if (values == null)
            {
                return result;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    result.Add(values[i]);
                }
            }

            return result;
        }

        private static string[] ToSortedArray(HashSet<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] result = new string[values.Count];
            values.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }
    }
}
