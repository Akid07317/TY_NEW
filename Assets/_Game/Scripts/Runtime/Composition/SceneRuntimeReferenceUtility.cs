using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Core;
using CampusRPG.Input;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.Composition
{
    public static class SceneRuntimeReferenceUtility
    {
        public static SceneRuntimeContext ResolveContext(SceneRuntimeContext context = null)
        {
            return context != null
                ? context
                : SceneRuntimeContext.Active != null
                    ? SceneRuntimeContext.Active
                    : Object.FindAnyObjectByType<SceneRuntimeContext>();
        }

        public static GameBootstrap ResolveBootstrap(GameBootstrap bootstrap = null)
        {
            if (bootstrap != null)
            {
                return bootstrap;
            }

            SceneRuntimeContext context = ResolveContext();

            if (context != null && context.Bootstrap != null)
            {
                return context.Bootstrap;
            }

            return GameBootstrap.Active != null
                ? GameBootstrap.Active
                : Object.FindAnyObjectByType<GameBootstrap>();
        }

        public static InputReader ResolveInputReader(InputReader inputReader, GameBootstrap bootstrap = null)
        {
            if (inputReader != null)
            {
                return inputReader;
            }

            SceneRuntimeContext context = ResolveContext();

            if (context != null && context.InputReader != null)
            {
                return context.InputReader;
            }

            GameBootstrap resolvedBootstrap = ResolveBootstrap(bootstrap);

            if (resolvedBootstrap != null && resolvedBootstrap.InputReader != null)
            {
                return resolvedBootstrap.InputReader;
            }

            return Object.FindAnyObjectByType<InputReader>();
        }

        public static PlayerCharacter ResolvePlayerCharacter(PlayerCharacter playerCharacter = null)
        {
            if (playerCharacter != null)
            {
                return playerCharacter;
            }

            SceneRuntimeContext context = ResolveContext();

            if (context != null && context.PlayerCharacter != null)
            {
                return context.PlayerCharacter;
            }

            return Object.FindAnyObjectByType<PlayerCharacter>();
        }

        public static ThirdPersonCameraController ResolveCameraController(ThirdPersonCameraController cameraController = null)
        {
            if (cameraController != null)
            {
                return cameraController;
            }

            SceneRuntimeContext context = ResolveContext();

            if (context != null && context.CameraController != null)
            {
                return context.CameraController;
            }

            return Object.FindAnyObjectByType<ThirdPersonCameraController>();
        }

        public static LockOnTargetSelector ResolveLockOnTargetSelector(
            LockOnTargetSelector lockOnTargetSelector = null,
            PlayerCharacter playerCharacter = null)
        {
            if (lockOnTargetSelector != null)
            {
                return lockOnTargetSelector;
            }

            SceneRuntimeContext context = ResolveContext();

            if (context != null && context.LockOnTargetSelector != null)
            {
                return context.LockOnTargetSelector;
            }

            PlayerCharacter resolvedPlayer = ResolvePlayerCharacter(playerCharacter);

            if (resolvedPlayer != null)
            {
                LockOnTargetSelector playerSelector = resolvedPlayer.GetComponent<LockOnTargetSelector>();

                if (playerSelector != null)
                {
                    return playerSelector;
                }
            }

            return Object.FindAnyObjectByType<LockOnTargetSelector>();
        }

        public static Transform ResolveCameraTransform(
            Transform cameraTransform,
            PlayerCharacter playerCharacter = null,
            ThirdPersonCameraController cameraController = null)
        {
            if (cameraTransform != null)
            {
                return cameraTransform;
            }

            ThirdPersonCameraController resolvedCameraController = ResolveCameraController(cameraController);

            if (resolvedCameraController != null)
            {
                return resolvedCameraController.transform;
            }

            PlayerCharacter resolvedPlayer = ResolvePlayerCharacter(playerCharacter);

            if (resolvedPlayer != null && resolvedPlayer.CameraTransform != null)
            {
                return resolvedPlayer.CameraTransform;
            }

            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;

            if (mainCamera != null)
            {
                return mainCamera.transform;
            }

            UnityEngine.Camera anyCamera = Object.FindAnyObjectByType<UnityEngine.Camera>();
            return anyCamera != null ? anyCamera.transform : null;
        }

        public static Transform ResolveFollowTarget(Transform followTarget, PlayerCharacter playerCharacter = null)
        {
            if (followTarget != null)
            {
                return followTarget;
            }

            PlayerCharacter resolvedPlayer = ResolvePlayerCharacter(playerCharacter);
            return resolvedPlayer != null ? resolvedPlayer.transform : null;
        }

        public static CheckpointRestoreCoordinator ResolveCheckpointRestoreCoordinator(
            CheckpointRestoreCoordinator coordinator = null)
        {
            if (coordinator != null)
            {
                return coordinator;
            }

            SceneRuntimeContext context = ResolveContext();

            if (context != null && context.CheckpointRestoreCoordinator != null)
            {
                return context.CheckpointRestoreCoordinator;
            }

            return Object.FindAnyObjectByType<CheckpointRestoreCoordinator>();
        }

        public static ChapterProgressService ResolveChapterProgressService(ChapterProgressService chapterProgressService = null)
        {
            if (chapterProgressService != null)
            {
                return chapterProgressService;
            }

            SceneRuntimeContext context = ResolveContext();

            if (context != null && context.ChapterProgressService != null)
            {
                return context.ChapterProgressService;
            }

            return Object.FindAnyObjectByType<ChapterProgressService>();
        }

        public static AudioSettingsSO ResolveAudioSettings(AudioSettingsSO audioSettings = null)
        {
            if (audioSettings != null)
            {
                return audioSettings;
            }

            SceneRuntimeContext context = ResolveContext();
            return context != null ? context.AudioSettings : null;
        }
    }
}
