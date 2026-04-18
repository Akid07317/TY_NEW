using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Core;
using CampusRPG.Input;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.Composition
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class SceneRuntimeContext : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField] private ThirdPersonCameraController cameraController;
        [SerializeField] private LockOnTargetSelector lockOnTargetSelector;
        [SerializeField] private CheckpointRestoreCoordinator checkpointRestoreCoordinator;
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private AudioSettingsSO audioSettings;

        public static SceneRuntimeContext Active { get; private set; }

        public GameBootstrap Bootstrap => bootstrap;

        public InputReader InputReader => inputReader != null
            ? inputReader
            : bootstrap != null
                ? bootstrap.InputReader
                : null;

        public PlayerCharacter PlayerCharacter => playerCharacter;

        public ThirdPersonCameraController CameraController => cameraController;

        public LockOnTargetSelector LockOnTargetSelector => lockOnTargetSelector;

        public CheckpointRestoreCoordinator CheckpointRestoreCoordinator => checkpointRestoreCoordinator;

        public ChapterProgressService ChapterProgressService => chapterProgressService;

        public AudioSettingsSO AudioSettings => audioSettings;

        private void Awake()
        {
            Active = this;
            SyncDerivedReferences();
        }

        private void OnEnable()
        {
            Active = this;
            SyncDerivedReferences();
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void SyncDerivedReferences()
        {
            bootstrap = SceneRuntimeReferenceUtility.ResolveBootstrap(bootstrap);
            inputReader = SceneRuntimeReferenceUtility.ResolveInputReader(inputReader, bootstrap);
            playerCharacter = SceneRuntimeReferenceUtility.ResolvePlayerCharacter(playerCharacter);
            cameraController = SceneRuntimeReferenceUtility.ResolveCameraController(cameraController);
            lockOnTargetSelector = SceneRuntimeReferenceUtility.ResolveLockOnTargetSelector(lockOnTargetSelector, playerCharacter);
            checkpointRestoreCoordinator = SceneRuntimeReferenceUtility.ResolveCheckpointRestoreCoordinator(checkpointRestoreCoordinator);
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }
    }
}
