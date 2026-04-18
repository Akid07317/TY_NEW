using System.Collections.Generic;
using UnityEngine;

namespace CampusRPG.Save
{
    public sealed class CheckpointRuntimeRegistry
    {
        private readonly Dictionary<string, CheckpointRuntimeAnchor> checkpoints = new Dictionary<string, CheckpointRuntimeAnchor>();

        public void Register(CheckpointRuntimeAnchor checkpoint)
        {
            if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.CheckpointId))
            {
                return;
            }

            checkpoints[checkpoint.CheckpointId] = checkpoint;
        }

        public void Unregister(CheckpointRuntimeAnchor checkpoint)
        {
            if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.CheckpointId))
            {
                return;
            }

            if (checkpoints.TryGetValue(checkpoint.CheckpointId, out CheckpointRuntimeAnchor registered) && registered == checkpoint)
            {
                checkpoints.Remove(checkpoint.CheckpointId);
            }
        }

        public CheckpointRuntimeAnchor Find(string checkpointId)
        {
            if (string.IsNullOrWhiteSpace(checkpointId))
            {
                return null;
            }

            if (checkpoints.TryGetValue(checkpointId, out CheckpointRuntimeAnchor checkpoint))
            {
                return checkpoint;
            }

            Rebuild();
            checkpoints.TryGetValue(checkpointId, out checkpoint);
            return checkpoint;
        }

        public void Rebuild()
        {
            checkpoints.Clear();
            CheckpointRuntimeAnchor[] anchors = Object.FindObjectsByType<CheckpointRuntimeAnchor>(FindObjectsInactive.Exclude);

            for (int i = 0; i < anchors.Length; i++)
            {
                Register(anchors[i]);
            }
        }
    }
}
