using System;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.Interaction
{
    [DisallowMultipleComponent]
    public sealed class EncounterController : MonoBehaviour, ICheckpointRestoreParticipant
    {
        [SerializeField] private string encounterId = string.Empty;
        [SerializeField] private bool activateOnPlayerEnter = true;
        [SerializeField] private bool startActive;
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private EnemyEncounterMember[] members = new EnemyEncounterMember[0];
        [SerializeField] private GameObject[] blockersToEnableWhileActive = new GameObject[0];

        private bool isActive;
        private bool isCleared;

        public static event Action<string> EncounterActivated;

        public string EncounterId => encounterId;

        public CheckpointRestoreGroup RestoreGroup => CheckpointRestoreGroup.Encounter;

        public int RestorePriority => 0;

        public bool IsActive => isActive;

        public bool IsCleared => isCleared;

        private void Awake()
        {
            ResolveReferences();
            CheckpointRestoreSceneResetter.RegisterParticipant(this);
            RefreshMembers();
            ApplyProgressState(true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            CheckpointRestoreSceneResetter.RegisterParticipant(this);

            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged += HandleProgressChanged;
            }

            ApplyProgressState(true);
        }

        private void OnDisable()
        {
            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged -= HandleProgressChanged;
            }
        }

        private void OnDestroy()
        {
            CheckpointRestoreSceneResetter.UnregisterParticipant(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!activateOnPlayerEnter || isActive || isCleared)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerCharacter>() == null)
            {
                return;
            }

            ActivateEncounter();
        }

        public void ActivateEncounter()
        {
            if (isCleared || isActive)
            {
                return;
            }

            isActive = true;
            SetBlockersActive(true);
            ResetMembersForEncounter(true);
            PublishEncounterActivated();

            if (AreAllMembersDefeated())
            {
                CompleteEncounter();
            }
        }

        public void NotifyMemberDefeated(EnemyEncounterMember member)
        {
            if (!isActive || isCleared)
            {
                return;
            }

            if (member == null)
            {
                return;
            }

            if (AreAllMembersDefeated())
            {
                CompleteEncounter();
            }
        }

        public void ResetForCheckpointRestore()
        {
            isActive = false;
            ApplyProgressState(true);
        }

        private void HandleProgressChanged()
        {
            ApplyProgressState(true);
        }

        private void ApplyProgressState(bool resetUncleared)
        {
            RefreshMembers();
            EncounterProgressPlan plan = EncounterControllerUtility.BuildProgressPlan(
                EncounterControllerUtility.HasClearedProgress(encounterId, chapterProgressService),
                isActive,
                startActive,
                resetUncleared);

            if (plan.ShouldApplyClearedState)
            {
                ApplyClearedState();
                return;
            }

            isCleared = false;

            if (plan.ShouldKeepActiveState)
            {
                SetBlockersActive(true);
                return;
            }

            if (plan.ShouldResetUncleared)
            {
                ResetMembersForEncounter(false);
            }

            SetBlockersActive(false);

            if (plan.ShouldActivateFromStart)
            {
                ActivateEncounter();
            }
        }

        private void ApplyClearedState()
        {
            isActive = false;
            isCleared = true;
            SetBlockersActive(false);
            SetMembersClearedState();
        }

        private bool AreAllMembersDefeated()
        {
            return EncounterControllerUtility.AreAllMembersDefeated(members);
        }

        private void CompleteEncounter()
        {
            isActive = false;
            isCleared = true;
            SetBlockersActive(false);
            SetMembersClearedState();

            if (chapterProgressService != null && !string.IsNullOrWhiteSpace(encounterId))
            {
                chapterProgressService.MarkEncounterCleared(encounterId);
            }
        }

        private void PublishEncounterActivated()
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                return;
            }

            EncounterActivated?.Invoke(encounterId);
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void RefreshMembers()
        {
            CacheMembers();
            BindMembers();
        }

        private void CacheMembers()
        {
            if (members != null && members.Length > 0)
            {
                return;
            }

            members = GetComponentsInChildren<EnemyEncounterMember>(true);
        }

        private void BindMembers()
        {
            if (members == null)
            {
                return;
            }

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] != null)
                {
                    members[i].BindEncounter(this);
                }
            }
        }

        private void ResetMembersForEncounter(bool activateObject)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] != null)
                {
                    members[i].ResetForEncounter(activateObject);
                }
            }
        }

        private void SetMembersClearedState()
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] != null)
                {
                    members[i].SetClearedState();
                }
            }
        }

        private void SetBlockersActive(bool active)
        {
            if (blockersToEnableWhileActive == null)
            {
                return;
            }

            for (int i = 0; i < blockersToEnableWhileActive.Length; i++)
            {
                if (blockersToEnableWhileActive[i] != null)
                {
                    blockersToEnableWhileActive[i].SetActive(active);
                }
            }
        }
    }
}
