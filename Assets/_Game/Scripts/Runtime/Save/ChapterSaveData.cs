using System;

namespace CampusRPG.Save
{
    [Serializable]
    public sealed class ChapterSaveData
    {
        public string chapterId = Chapter01Ids.Chapter;
        public string checkpointId = string.Empty;
        public string currentAreaId = string.Empty;
        public string[] visitedAreaIds = Array.Empty<string>();
        public string[] keyItemIds = Array.Empty<string>();
        public string[] clearedEncounterIds = Array.Empty<string>();
        public bool chapterCompleted;
        public float playerHealth = 100f;
        public float playerMana = 100f;
    }
}
