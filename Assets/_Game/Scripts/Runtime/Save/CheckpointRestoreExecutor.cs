using CampusRPG.Character;

namespace CampusRPG.Save
{
    public static class CheckpointRestoreExecutor
    {
        public static void Apply(
            string chapterId,
            PlayerCharacter player,
            CheckpointService checkpointService,
            ChapterProgressService chapterProgressService,
            ChapterSaveData saveData,
            CheckpointRestoreSnapshot snapshot)
        {
            if (player == null)
            {
                return;
            }

            if (checkpointService != null)
            {
                checkpointService.RestoreFromSave(snapshot.ToSaveData(chapterId));
            }

            chapterProgressService?.RestoreFromSave(saveData);
            CheckpointRestoreSceneResetter.ResetInteractions();
            player.RestoreFromCheckpoint(
                snapshot.RespawnPosition,
                snapshot.RespawnRotation,
                snapshot.HealthValue,
                snapshot.ManaValue);
            CheckpointRestoreSceneResetter.ResetEncounters();
            CheckpointRestoreSceneResetter.ResetEnemies();
        }
    }
}
