namespace CampusRPG.Save
{
    public enum CheckpointRestoreGroup
    {
        Interaction = 0,
        Encounter = 1,
        Enemy = 2
    }

    public interface ICheckpointRestoreParticipant
    {
        CheckpointRestoreGroup RestoreGroup { get; }

        int RestorePriority { get; }

        void ResetForCheckpointRestore();
    }
}
