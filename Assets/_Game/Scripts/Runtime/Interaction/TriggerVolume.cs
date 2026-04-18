using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.Interaction
{
    public enum TriggerVolumeAction
    {
        EnterArea = 0,
        ClearEncounter = 1,
        CompleteChapter = 2
    }

    [DisallowMultipleComponent]
    public sealed class TriggerVolume : MonoBehaviour, ICheckpointRestoreParticipant
    {
        [SerializeField] private TriggerVolumeAction action;
        [SerializeField] private string payloadId = string.Empty;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private ChapterProgressService chapterProgressService;

        private bool hasTriggered;
        private bool disabledByProgressState;

        public CheckpointRestoreGroup RestoreGroup => CheckpointRestoreGroup.Interaction;

        public int RestorePriority => 100;

        private void Awake()
        {
            ResolveReferences();
            CheckpointRestoreSceneResetter.RegisterParticipant(this);
            SyncConsumedState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CheckpointRestoreSceneResetter.RegisterParticipant(this);

            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged += HandleProgressChanged;
            }

            SyncConsumedState();
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
            if (hasTriggered && oneShot)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerCharacter>() == null)
            {
                return;
            }

            ResolveReferences();

            if (chapterProgressService == null)
            {
                return;
            }

            bool triggered = false;

            switch (action)
            {
                case TriggerVolumeAction.EnterArea:
                    triggered = chapterProgressService.EnterArea(payloadId);
                    break;
                case TriggerVolumeAction.ClearEncounter:
                    triggered = chapterProgressService.MarkEncounterCleared(payloadId);
                    break;
                case TriggerVolumeAction.CompleteChapter:
                    triggered = chapterProgressService.CompleteChapter(payloadId);
                    break;
            }

            if (triggered && oneShot)
            {
                hasTriggered = true;
                disabledByProgressState = true;
                gameObject.SetActive(false);
            }
        }

        private void HandleProgressChanged()
        {
            SyncConsumedState();
        }

        public void ResetForCheckpointRestore()
        {
            ResolveReferences();
            SyncConsumedState(allowReactivation: true);
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void SyncConsumedState()
        {
            SyncConsumedState(allowReactivation: false);
        }

        private void SyncConsumedState(bool allowReactivation)
        {
            if (!oneShot || chapterProgressService == null)
            {
                return;
            }

            bool shouldBeConsumed = IsAlreadyConsumed();
            hasTriggered = shouldBeConsumed;

            if (!shouldBeConsumed)
            {
                if (allowReactivation && disabledByProgressState && !gameObject.activeSelf)
                {
                    disabledByProgressState = false;
                    gameObject.SetActive(true);
                }

                return;
            }

            if (gameObject.activeSelf)
            {
                disabledByProgressState = true;
                gameObject.SetActive(false);
            }
        }

        private bool IsAlreadyConsumed()
        {
            switch (action)
            {
                case TriggerVolumeAction.EnterArea:
                    return chapterProgressService.HasVisitedArea(payloadId);
                case TriggerVolumeAction.ClearEncounter:
                    return chapterProgressService.IsEncounterCleared(payloadId);
                case TriggerVolumeAction.CompleteChapter:
                    return chapterProgressService.IsChapterCompleted
                        || (!string.IsNullOrWhiteSpace(payloadId) && chapterProgressService.HasKeyItem(payloadId));
                default:
                    return false;
            }
        }
    }
}
