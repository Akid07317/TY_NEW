using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.Interaction
{
    [DisallowMultipleComponent]
    public sealed class KeyItemPickup : MonoBehaviour, ICheckpointRestoreParticipant
    {
        [SerializeField] private string keyItemId = string.Empty;
        [SerializeField] private string requiredEncounterId = string.Empty;
        [SerializeField] private bool completeChapterOnPickup;
        [SerializeField] private ChapterProgressService chapterProgressService;

        private bool isCollected;
        private bool disabledByCollectedState;

        public string KeyItemId => keyItemId;

        public CheckpointRestoreGroup RestoreGroup => CheckpointRestoreGroup.Interaction;

        public int RestorePriority => 0;

        private void Awake()
        {
            ResolveReferences();
            CheckpointRestoreSceneResetter.RegisterParticipant(this);
            SyncCollectedState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CheckpointRestoreSceneResetter.RegisterParticipant(this);

            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged += HandleProgressChanged;
            }

            SyncCollectedState();
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
            TryCollect(other);
        }

        public bool TryCollect(Collider other)
        {
            if (isCollected || other == null || other.GetComponentInParent<PlayerCharacter>() == null)
            {
                return false;
            }

            ResolveReferences();

            if (chapterProgressService == null || string.IsNullOrWhiteSpace(keyItemId))
            {
                return false;
            }

            if (!chapterProgressService.MeetsRequirements(string.Empty, requiredEncounterId, string.Empty))
            {
                return false;
            }

            bool changed = completeChapterOnPickup
                ? chapterProgressService.CompleteChapter(keyItemId)
                : chapterProgressService.RegisterKeyItem(keyItemId);

            if (!changed)
            {
                return false;
            }

            isCollected = true;
            disabledByCollectedState = true;
            gameObject.SetActive(false);
            return true;
        }

        private void HandleProgressChanged()
        {
            SyncCollectedState();
        }

        public void ResetForCheckpointRestore()
        {
            ResolveReferences();
            SyncCollectedState(allowReactivation: true);
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void SyncCollectedState()
        {
            SyncCollectedState(allowReactivation: false);
        }

        private void SyncCollectedState(bool allowReactivation)
        {
            if (chapterProgressService == null || string.IsNullOrWhiteSpace(keyItemId))
            {
                return;
            }

            bool shouldBeCollected = chapterProgressService.HasKeyItem(keyItemId);
            isCollected = shouldBeCollected;

            if (!shouldBeCollected)
            {
                if (allowReactivation && disabledByCollectedState && !gameObject.activeSelf)
                {
                    disabledByCollectedState = false;
                    gameObject.SetActive(true);
                }

                return;
            }

            if (gameObject.activeSelf)
            {
                disabledByCollectedState = true;
                gameObject.SetActive(false);
            }
        }
    }
}
