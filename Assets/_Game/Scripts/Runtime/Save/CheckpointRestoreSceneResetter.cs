using System;
using System.Collections.Generic;

namespace CampusRPG.Save
{
    public static class CheckpointRestoreSceneResetter
    {
        private static readonly HashSet<ICheckpointRestoreParticipant> registeredParticipants = new HashSet<ICheckpointRestoreParticipant>();
        private static readonly Dictionary<ICheckpointRestoreParticipant, long> participantRegistrationOrder = new Dictionary<ICheckpointRestoreParticipant, long>();
        private static long nextRegistrationOrder;

        public static void RegisterParticipant(ICheckpointRestoreParticipant participant)
        {
            if (participant == null)
            {
                return;
            }

            if (!registeredParticipants.Add(participant))
            {
                return;
            }

            participantRegistrationOrder[participant] = nextRegistrationOrder++;
        }

        public static void UnregisterParticipant(ICheckpointRestoreParticipant participant)
        {
            if (participant == null)
            {
                return;
            }

            registeredParticipants.Remove(participant);
            participantRegistrationOrder.Remove(participant);
        }

        public static void ResetEnemies()
        {
            ResetParticipants(CheckpointRestoreGroup.Enemy);
        }

        public static void ResetEncounters()
        {
            ResetParticipants(CheckpointRestoreGroup.Encounter);
        }

        public static void ResetInteractions()
        {
            ResetParticipants(CheckpointRestoreGroup.Interaction);
        }

        private static void ResetParticipants(CheckpointRestoreGroup group)
        {
            if (registeredParticipants.Count == 0)
            {
                return;
            }

            ICheckpointRestoreParticipant[] snapshot = new ICheckpointRestoreParticipant[registeredParticipants.Count];
            registeredParticipants.CopyTo(snapshot);

            List<ICheckpointRestoreParticipant> matchingParticipants = new List<ICheckpointRestoreParticipant>(snapshot.Length);
            List<ICheckpointRestoreParticipant> staleParticipants = null;

            for (int i = 0; i < snapshot.Length; i++)
            {
                ICheckpointRestoreParticipant participant = snapshot[i];
                UnityEngine.Object participantObject = participant as UnityEngine.Object;

                if (participantObject == null)
                {
                    if (staleParticipants == null)
                    {
                        staleParticipants = new List<ICheckpointRestoreParticipant>();
                    }

                    staleParticipants.Add(participant);
                    continue;
                }

                if (participant.RestoreGroup != group)
                {
                    continue;
                }

                matchingParticipants.Add(participant);
            }

            matchingParticipants.Sort(CompareParticipantsByPriority);

            for (int i = 0; i < matchingParticipants.Count; i++)
            {
                matchingParticipants[i].ResetForCheckpointRestore();
            }

            if (staleParticipants == null)
            {
                return;
            }

            for (int i = 0; i < staleParticipants.Count; i++)
            {
                registeredParticipants.Remove(staleParticipants[i]);
                participantRegistrationOrder.Remove(staleParticipants[i]);
            }
        }

        private static int CompareParticipantsByPriority(ICheckpointRestoreParticipant left, ICheckpointRestoreParticipant right)
        {
            int priorityComparison = left.RestorePriority.CompareTo(right.RestorePriority);

            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            long leftOrder = participantRegistrationOrder.TryGetValue(left, out long resolvedLeftOrder)
                ? resolvedLeftOrder
                : long.MaxValue;
            long rightOrder = participantRegistrationOrder.TryGetValue(right, out long resolvedRightOrder)
                ? resolvedRightOrder
                : long.MaxValue;
            return leftOrder.CompareTo(rightOrder);
        }
    }
}
