using System;
using UnityEngine;

namespace CampusRPG.Save
{
    [CreateAssetMenu(fileName = "SO_ChapterProgression", menuName = "CampusRPG/Save/Chapter Progression")]
    public sealed class ChapterProgressionSO : ScriptableObject
    {
        [SerializeField] private string chapterId = Chapter01Ids.Chapter;
        [SerializeField] private ChapterAreaProgressionEntry[] areas = Array.Empty<ChapterAreaProgressionEntry>();
        [SerializeField] private string bossGateRequiredKeyItemId = Chapter01Ids.KeyItems.GateSigil;
        [SerializeField] private string chapterCompletionKeyItemId = Chapter01Ids.KeyItems.RitualCore;

        public string ChapterId => chapterId;

        public string BossGateRequiredKeyItemId => bossGateRequiredKeyItemId;

        public string ChapterCompletionKeyItemId => chapterCompletionKeyItemId;

        public ChapterAreaProgressionEntry[] Areas => areas ?? Array.Empty<ChapterAreaProgressionEntry>();

        public string GetFirstAreaId()
        {
            return Areas.Length > 0 ? Areas[0].AreaId : string.Empty;
        }

        public int GetAreaIndex(string areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
            {
                return -1;
            }

            ChapterAreaProgressionEntry[] configuredAreas = Areas;

            for (int i = 0; i < configuredAreas.Length; i++)
            {
                if (configuredAreas[i] != null && configuredAreas[i].AreaId == areaId)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool ContainsArea(string areaId)
        {
            return GetAreaIndex(areaId) >= 0;
        }
    }

    [Serializable]
    public sealed class ChapterAreaProgressionEntry
    {
        [SerializeField] private string areaId;
        [SerializeField] private string displayName;

        public ChapterAreaProgressionEntry(string areaId, string displayName)
        {
            this.areaId = areaId;
            this.displayName = displayName;
        }

        public string AreaId => areaId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? areaId : displayName;
    }
}
