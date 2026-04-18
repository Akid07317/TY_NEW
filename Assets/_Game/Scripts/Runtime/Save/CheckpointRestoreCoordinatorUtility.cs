using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Save
{
    public enum CheckpointActivationMode
    {
        None = 0,
        ActivateService = 1,
        RefreshAndSave = 2
    }

    public static class CheckpointRestoreCoordinatorUtility
    {
        public static CheckpointActivationMode ResolveActivationMode(
            CheckpointService checkpointService,
            CheckpointRuntimeAnchor checkpoint)
        {
            if (checkpoint == null)
            {
                return CheckpointActivationMode.None;
            }

            if (checkpointService == null)
            {
                return CheckpointActivationMode.RefreshAndSave;
            }

            return checkpointService.CurrentCheckpointId == checkpoint.CheckpointId
                ? CheckpointActivationMode.RefreshAndSave
                : CheckpointActivationMode.ActivateService;
        }

        public static CheckpointRestoreSnapshot BuildRestoreSnapshot(
            PlayerCharacter player,
            ChapterSaveData saveData,
            CheckpointRuntimeRegistry checkpointRegistry,
            string currentCheckpointId,
            string defaultCheckpointId,
            Vector3 initialSpawnPosition,
            Quaternion initialSpawnRotation,
            Vector3 defaultRespawnOffset,
            bool defaultRestoreFullHealth,
            bool defaultRestoreFullMana)
        {
            string checkpointId = CheckpointRestorePlanner.ResolveCheckpointId(
                saveData,
                currentCheckpointId,
                defaultCheckpointId);

            return CheckpointRestorePlanner.CreateSnapshot(
                player,
                saveData,
                checkpointRegistry != null ? checkpointRegistry.Find(checkpointId) : null,
                currentCheckpointId,
                defaultCheckpointId,
                initialSpawnPosition,
                initialSpawnRotation,
                defaultRespawnOffset,
                defaultRestoreFullHealth,
                defaultRestoreFullMana);
        }

        public static ChapterSaveData BuildSaveData(
            string chapterId,
            PlayerCharacter player,
            string currentCheckpointId,
            string defaultCheckpointId,
            ChapterProgressService chapterProgressService)
        {
            ChapterSaveData saveData = CheckpointRestorePlanner.BuildSaveData(
                chapterId,
                player,
                CheckpointRestorePlanner.ResolveCheckpointId(
                    null,
                    currentCheckpointId,
                    defaultCheckpointId));
            chapterProgressService?.PopulateSaveData(saveData);
            return saveData;
        }

        public static bool MatchesChapter(string chapterId, ChapterSaveData saveData)
        {
            return saveData != null
                && (string.IsNullOrWhiteSpace(saveData.chapterId) || saveData.chapterId == chapterId);
        }
    }
}
