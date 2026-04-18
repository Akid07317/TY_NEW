using System;
using System.Collections.Generic;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.Save
{
    [DefaultExecutionOrder(-850)]
    [DisallowMultipleComponent]
    public sealed class ChapterProgressService : MonoBehaviour
    {
        [SerializeField] private ChapterProgressionSO progression;
        [SerializeField] private CheckpointRestoreCoordinator checkpointRestoreCoordinator;
        [SerializeField] private string currentAreaId = string.Empty;
        [SerializeField] private bool chapterCompleted;

        private readonly HashSet<string> visitedAreaIds = new HashSet<string>();
        private readonly HashSet<string> keyItemIds = new HashSet<string>();
        private readonly HashSet<string> clearedEncounterIds = new HashSet<string>();
        private bool suppressAutoSave;

        public event Action ProgressChanged;
        public event Action<string> AreaEntered;
        public event Action<string> KeyItemAcquired;
        public event Action<string> EncounterCleared;
        public event Action ChapterCompleted;

        public ChapterProgressionSO Progression => progression;

        public string CurrentAreaId => currentAreaId;

        public bool IsChapterCompleted => chapterCompleted;

        private void Awake()
        {
            ResolveReferences();
            ApplySnapshot(CreateSnapshot());
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        public bool EnterArea(string areaId)
        {
            if (!ChapterProgressStateUtility.TryEnterArea(visitedAreaIds, areaId, ref currentAreaId))
            {
                return false;
            }

            AreaEntered?.Invoke(areaId);
            NotifyProgressChanged();
            return true;
        }

        public bool RegisterKeyItem(string keyItemId)
        {
            if (!ChapterProgressStateUtility.TryRegisterId(keyItemIds, keyItemId))
            {
                return false;
            }

            KeyItemAcquired?.Invoke(keyItemId);
            NotifyProgressChanged();
            return true;
        }

        public bool MarkEncounterCleared(string encounterId)
        {
            if (!ChapterProgressStateUtility.TryRegisterId(clearedEncounterIds, encounterId))
            {
                return false;
            }

            EncounterCleared?.Invoke(encounterId);
            NotifyProgressChanged();
            return true;
        }

        public bool CompleteChapter(string completionKeyItemId = "")
        {
            ChapterProgressCompletionResult completionResult = ChapterProgressStateUtility.CompleteChapter(
                keyItemIds,
                completionKeyItemId,
                ref chapterCompleted);

            if (!completionResult.Changed)
            {
                return false;
            }

            if (completionResult.KeyItemRegistered)
            {
                KeyItemAcquired?.Invoke(completionKeyItemId);
            }

            if (completionResult.ChapterCompletedNow)
            {
                ChapterCompleted?.Invoke();
            }

            NotifyProgressChanged();
            return true;
        }

        public bool HasKeyItem(string keyItemId)
        {
            return ChapterProgressStateUtility.Contains(keyItemIds, keyItemId);
        }

        public bool IsEncounterCleared(string encounterId)
        {
            return ChapterProgressStateUtility.Contains(clearedEncounterIds, encounterId);
        }

        public bool HasVisitedArea(string areaId)
        {
            return ChapterProgressStateUtility.Contains(visitedAreaIds, areaId);
        }

        public bool MeetsRequirements(string requiredAreaId, string requiredEncounterId, string requiredKeyItemId)
        {
            return ChapterProgressStateUtility.MeetsRequirements(
                visitedAreaIds,
                clearedEncounterIds,
                keyItemIds,
                requiredAreaId,
                requiredEncounterId,
                requiredKeyItemId);
        }

        public void PopulateSaveData(ChapterSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            CreateSnapshot().PopulateSaveData(saveData, progression != null ? progression.ChapterId : string.Empty);
        }

        public void RestoreFromSave(ChapterSaveData saveData)
        {
            suppressAutoSave = true;
            try
            {
                ApplySnapshot(ChapterProgressPersistence.RestoreSnapshot(progression, saveData));
            }
            finally
            {
                suppressAutoSave = false;
            }

            ProgressChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            checkpointRestoreCoordinator = SceneRuntimeReferenceUtility.ResolveCheckpointRestoreCoordinator(checkpointRestoreCoordinator);
        }

        private void ApplySnapshot(ChapterProgressSnapshot snapshot)
        {
            ChapterProgressStateUtility.ApplySnapshot(
                snapshot,
                ref currentAreaId,
                ref chapterCompleted,
                visitedAreaIds,
                keyItemIds,
                clearedEncounterIds);
        }

        private void NotifyProgressChanged()
        {
            ProgressChanged?.Invoke();

            if (!suppressAutoSave && checkpointRestoreCoordinator != null)
            {
                checkpointRestoreCoordinator.SaveCurrentProgress();
            }
        }

        private ChapterProgressSnapshot CreateSnapshot()
        {
            return ChapterProgressPersistence.CreateSnapshot(
                progression,
                currentAreaId,
                visitedAreaIds,
                keyItemIds,
                clearedEncounterIds,
                chapterCompleted);
        }
    }
}
