using UnityEngine;

namespace CampusRPG.Save
{
    public readonly struct CheckpointRestoreSnapshot
    {
        public CheckpointRestoreSnapshot(
            string checkpointId,
            Vector3 respawnPosition,
            Quaternion respawnRotation,
            float healthValue,
            float manaValue)
        {
            CheckpointId = checkpointId ?? string.Empty;
            RespawnPosition = respawnPosition;
            RespawnRotation = respawnRotation;
            HealthValue = healthValue;
            ManaValue = manaValue;
        }

        public string CheckpointId { get; }

        public Vector3 RespawnPosition { get; }

        public Quaternion RespawnRotation { get; }

        public float HealthValue { get; }

        public float ManaValue { get; }

        public ChapterSaveData ToSaveData(string chapterId)
        {
            return new ChapterSaveData
            {
                chapterId = chapterId,
                checkpointId = CheckpointId,
                playerHealth = HealthValue,
                playerMana = ManaValue
            };
        }
    }
}
