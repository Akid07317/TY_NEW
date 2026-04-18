using System;
using UnityEngine;

namespace CampusRPG.Save
{
    public sealed class CheckpointService : MonoBehaviour
    {
        [SerializeField] private string currentCheckpointId = string.Empty;

        public event Action<string> CheckpointActivated;

        public string CurrentCheckpointId => currentCheckpointId;

        public void ActivateCheckpoint(string checkpointId)
        {
            if (string.IsNullOrWhiteSpace(checkpointId) || currentCheckpointId == checkpointId)
            {
                return;
            }

            currentCheckpointId = checkpointId;
            CheckpointActivated?.Invoke(currentCheckpointId);
        }

        public void RestoreFromSave(ChapterSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            currentCheckpointId = saveData.checkpointId;
            CheckpointActivated?.Invoke(currentCheckpointId);
        }
    }
}
