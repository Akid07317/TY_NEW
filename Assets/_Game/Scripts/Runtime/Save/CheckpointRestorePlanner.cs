using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Save
{
    public static class CheckpointRestorePlanner
    {
        public static CheckpointRestoreSnapshot CreateSnapshot(
            PlayerCharacter player,
            ChapterSaveData saveData,
            CheckpointRuntimeAnchor checkpoint,
            string currentCheckpointId,
            string defaultCheckpointId,
            Vector3 initialSpawnPosition,
            Quaternion initialSpawnRotation,
            Vector3 defaultRespawnOffset,
            bool defaultRestoreFullHealth,
            bool defaultRestoreFullMana)
        {
            string checkpointId = ResolveCheckpointId(saveData, currentCheckpointId, defaultCheckpointId);
            Vector3 respawnPosition = initialSpawnPosition + defaultRespawnOffset;
            Quaternion respawnRotation = initialSpawnRotation;
            bool restoreFullHealth = defaultRestoreFullHealth;
            bool restoreFullMana = defaultRestoreFullMana;

            if (checkpoint != null)
            {
                respawnPosition = checkpoint.RespawnPosition;
                respawnRotation = checkpoint.RespawnRotation;
                restoreFullHealth = checkpoint.RestoreFullHealth;
                restoreFullMana = checkpoint.RestoreFullMana;
            }

            float healthValue = ResolveRestoreValue(
                player != null && player.Health != null ? player.Health.MaxValue : 100f,
                saveData != null ? saveData.playerHealth : 0f,
                restoreFullHealth);
            float manaValue = ResolveRestoreValue(
                player != null && player.Mana != null ? player.Mana.MaxValue : 100f,
                saveData != null ? saveData.playerMana : 0f,
                restoreFullMana);

            return new CheckpointRestoreSnapshot(
                checkpointId,
                respawnPosition,
                respawnRotation,
                healthValue,
                manaValue);
        }

        public static ChapterSaveData BuildSaveData(string chapterId, PlayerCharacter player, string checkpointId)
        {
            return new ChapterSaveData
            {
                chapterId = chapterId,
                checkpointId = checkpointId,
                playerHealth = player != null && player.Health != null ? player.Health.CurrentValue : 100f,
                playerMana = player != null && player.Mana != null ? player.Mana.CurrentValue : 100f
            };
        }

        public static void ApplyCheckpointRefresh(PlayerCharacter player, CheckpointRuntimeAnchor checkpoint)
        {
            if (player == null || checkpoint == null)
            {
                return;
            }

            if (checkpoint.RestoreFullHealth)
            {
                player.Health?.RestoreFull();
            }

            if (checkpoint.RestoreFullMana)
            {
                player.Mana?.RestoreFull();
            }
        }

        public static string ResolveCheckpointId(ChapterSaveData saveData, string currentCheckpointId, string defaultCheckpointId)
        {
            if (saveData != null && !string.IsNullOrWhiteSpace(saveData.checkpointId))
            {
                return saveData.checkpointId;
            }

            if (!string.IsNullOrWhiteSpace(currentCheckpointId))
            {
                return currentCheckpointId;
            }

            return defaultCheckpointId;
        }

        private static float ResolveRestoreValue(float maxValue, float savedValue, bool restoreFull)
        {
            if (restoreFull)
            {
                return maxValue;
            }

            return Mathf.Clamp(savedValue > 0f ? savedValue : maxValue, 1f, maxValue);
        }
    }
}
