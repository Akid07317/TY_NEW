using System.Collections;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.Save
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SaveService))]
    [RequireComponent(typeof(CheckpointService))]
    public sealed class CheckpointRestoreCoordinator : MonoBehaviour
    {
        [SerializeField] private string chapterId = Chapter01Ids.Chapter;
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private SaveService saveService;
        [SerializeField] private CheckpointService checkpointService;
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private float respawnDelaySeconds = 1f;
        [SerializeField] private bool autoLoadFromSaveOnStart = true;
        [SerializeField] private bool autoSaveOnStart = true;
        [SerializeField] private string defaultCheckpointId = Chapter01Ids.Checkpoints.Start;
        [SerializeField] private Vector3 defaultRespawnOffset = Vector3.zero;
        [SerializeField] private bool defaultRestoreFullHealth = true;
        [SerializeField] private bool defaultRestoreFullMana = true;

        private readonly CheckpointRuntimeRegistry checkpointRegistry = new CheckpointRuntimeRegistry();
        private Coroutine respawnRoutine;
        private bool isApplyingRestore;
        private Vector3 initialSpawnPosition;
        private Quaternion initialSpawnRotation;
        private HealthComponent subscribedPlayerHealth;
        private string CurrentCheckpointId => checkpointService != null ? checkpointService.CurrentCheckpointId : string.Empty;

        private void Awake()
        {
            ResolveReferences();
            CacheInitialSpawn();
            checkpointRegistry.Rebuild();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshPlayerDeathSubscription();

            if (checkpointService != null)
            {
                checkpointService.CheckpointActivated += HandleCheckpointActivated;
            }
        }

        private void Start()
        {
            ResolveReferences();
            CacheInitialSpawn();
            checkpointRegistry.Rebuild();
            RefreshPlayerDeathSubscription();

            if (autoLoadFromSaveOnStart && TryLoadCurrentChapterSave(out ChapterSaveData saveData))
            {
                RestoreFromSave(saveData);
                return;
            }

            if (autoSaveOnStart)
            {
                SaveCurrentProgress();
            }
        }

        private void OnDisable()
        {
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }

            if (checkpointService != null)
            {
                checkpointService.CheckpointActivated -= HandleCheckpointActivated;
            }

            if (subscribedPlayerHealth != null)
            {
                subscribedPlayerHealth.Died -= HandlePlayerDied;
                subscribedPlayerHealth = null;
            }
        }

        public void RegisterCheckpoint(CheckpointRuntimeAnchor checkpoint)
        {
            checkpointRegistry.Register(checkpoint);
        }

        public void UnregisterCheckpoint(CheckpointRuntimeAnchor checkpoint)
        {
            checkpointRegistry.Unregister(checkpoint);
        }

        public void ActivateCheckpoint(CheckpointRuntimeAnchor checkpoint)
        {
            RegisterCheckpoint(checkpoint);

            switch (CheckpointRestoreCoordinatorUtility.ResolveActivationMode(checkpointService, checkpoint))
            {
                case CheckpointActivationMode.ActivateService:
                    checkpointService.ActivateCheckpoint(checkpoint.CheckpointId);
                    return;
                case CheckpointActivationMode.RefreshAndSave:
                    RefreshCheckpointAndSave(checkpoint);
                    return;
            }
        }

        public void RestoreLatestCheckpoint()
        {
            ChapterSaveData saveData = null;
            TryLoadCurrentChapterSave(out saveData);
            RestoreFromSave(saveData);
        }

        public void SaveCurrentProgress()
        {
            if (saveService == null)
            {
                return;
            }

            saveService.Save(CheckpointRestoreCoordinatorUtility.BuildSaveData(
                chapterId,
                player,
                CurrentCheckpointId,
                defaultCheckpointId,
                chapterProgressService));
        }

        private void HandleCheckpointActivated(string checkpointId)
        {
            if (isApplyingRestore)
            {
                return;
            }

            RefreshCheckpointAndSave(checkpointRegistry.Find(checkpointId));
        }

        private void HandlePlayerDied()
        {
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
            }

            respawnRoutine = StartCoroutine(RestoreAfterDelay());
        }

        private IEnumerator RestoreAfterDelay()
        {
            try
            {
                if (respawnDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(respawnDelaySeconds);
                }

                RestoreLatestCheckpoint();
            }
            finally
            {
                respawnRoutine = null;
            }
        }

        private void RestoreFromSave(ChapterSaveData saveData)
        {
            if (player == null)
            {
                return;
            }

            CheckpointRestoreSnapshot snapshot = CheckpointRestoreCoordinatorUtility.BuildRestoreSnapshot(
                player,
                saveData,
                checkpointRegistry,
                CurrentCheckpointId,
                defaultCheckpointId,
                initialSpawnPosition,
                initialSpawnRotation,
                defaultRespawnOffset,
                defaultRestoreFullHealth,
                defaultRestoreFullMana);

            isApplyingRestore = true;

            try
            {
                CheckpointRestoreExecutor.Apply(
                    chapterId,
                    player,
                    checkpointService,
                    chapterProgressService,
                    saveData,
                    snapshot);
            }
            finally
            {
                isApplyingRestore = false;
            }

            SaveCurrentProgress();
        }

        private bool TryLoadCurrentChapterSave(out ChapterSaveData saveData)
        {
            saveData = null;

            if (saveService == null || !saveService.TryLoad(out saveData) || saveData == null)
            {
                return false;
            }

            return CheckpointRestoreCoordinatorUtility.MatchesChapter(chapterId, saveData);
        }

        private void ResolveReferences()
        {
            player = SceneRuntimeReferenceUtility.ResolvePlayerCharacter(player);

            if (saveService == null)
            {
                saveService = GetComponent<SaveService>();
            }

            if (checkpointService == null)
            {
                checkpointService = GetComponent<CheckpointService>();
            }

            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void RefreshPlayerDeathSubscription()
        {
            HealthComponent nextHealth = player != null ? player.Health : null;

            if (subscribedPlayerHealth == nextHealth)
            {
                return;
            }

            if (subscribedPlayerHealth != null)
            {
                subscribedPlayerHealth.Died -= HandlePlayerDied;
            }

            subscribedPlayerHealth = nextHealth;

            if (subscribedPlayerHealth != null)
            {
                subscribedPlayerHealth.Died += HandlePlayerDied;
            }
        }

        private void CacheInitialSpawn()
        {
            if (player == null)
            {
                return;
            }

            initialSpawnPosition = player.transform.position;
            initialSpawnRotation = player.transform.rotation;
        }

        private void RefreshCheckpointAndSave(CheckpointRuntimeAnchor checkpoint)
        {
            if (checkpoint != null)
            {
                CheckpointRestorePlanner.ApplyCheckpointRefresh(player, checkpoint);
            }

            SaveCurrentProgress();
        }

    }
}
