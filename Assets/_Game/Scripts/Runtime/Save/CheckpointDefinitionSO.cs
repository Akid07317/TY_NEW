using UnityEngine;

namespace CampusRPG.Save
{
    [CreateAssetMenu(fileName = "SO_CheckpointDefinition", menuName = "CampusRPG/Save/Checkpoint Definition")]
    public sealed class CheckpointDefinitionSO : ScriptableObject
    {
        [SerializeField] private string checkpointId = Chapter01Ids.Checkpoints.Start;
        [SerializeField] private Vector3 respawnOffset = new Vector3(0f, 0f, 1f);
        [SerializeField] private bool restoreFullHealth = true;
        [SerializeField] private bool restoreFullMana = true;

        public string CheckpointId => checkpointId;

        public Vector3 RespawnOffset => respawnOffset;

        public bool RestoreFullHealth => restoreFullHealth;

        public bool RestoreFullMana => restoreFullMana;
    }
}
