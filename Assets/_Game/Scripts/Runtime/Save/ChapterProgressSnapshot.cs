using System;

namespace CampusRPG.Save
{
    public readonly struct ChapterProgressSnapshot
    {
        public ChapterProgressSnapshot(
            string currentAreaId,
            string[] visitedAreaIds,
            string[] keyItemIds,
            string[] clearedEncounterIds,
            bool chapterCompleted)
        {
            CurrentAreaId = currentAreaId ?? string.Empty;
            VisitedAreaIds = visitedAreaIds ?? Array.Empty<string>();
            KeyItemIds = keyItemIds ?? Array.Empty<string>();
            ClearedEncounterIds = clearedEncounterIds ?? Array.Empty<string>();
            ChapterCompleted = chapterCompleted;
        }

        public string CurrentAreaId { get; }

        public string[] VisitedAreaIds { get; }

        public string[] KeyItemIds { get; }

        public string[] ClearedEncounterIds { get; }

        public bool ChapterCompleted { get; }

        public void PopulateSaveData(ChapterSaveData saveData, string chapterId)
        {
            if (saveData == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(chapterId))
            {
                saveData.chapterId = chapterId;
            }

            saveData.currentAreaId = CurrentAreaId;
            saveData.visitedAreaIds = VisitedAreaIds;
            saveData.keyItemIds = KeyItemIds;
            saveData.clearedEncounterIds = ClearedEncounterIds;
            saveData.chapterCompleted = ChapterCompleted;
        }
    }
}
