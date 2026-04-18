using System.Reflection;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Core;
using CampusRPG.Input;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class SceneRuntimeReferenceUtilityTests
    {
        [TearDown]
        public void TearDown()
        {
            SetActiveContext(null);
            SetActiveBootstrap(null);
        }

        [Test]
        public void ResolveInputReader_PrefersActiveContext_AndFallsBackToBootstrap()
        {
            GameObject bootstrapObject = new GameObject("Bootstrap");
            GameObject bootstrapInputObject = new GameObject("BootstrapInput");
            GameObject contextObject = new GameObject("SceneRuntimeContext");
            GameObject contextInputObject = new GameObject("ContextInput");

            try
            {
                GameBootstrap bootstrap = bootstrapObject.AddComponent<GameBootstrap>();
                InputReader bootstrapInput = bootstrapInputObject.AddComponent<InputReader>();
                SceneRuntimeContext context = contextObject.AddComponent<SceneRuntimeContext>();
                InputReader contextInput = contextInputObject.AddComponent<InputReader>();

                SetPrivateField(bootstrap, "inputReader", bootstrapInput);
                SetPrivateField(context, "bootstrap", bootstrap);
                SetPrivateField(context, "inputReader", contextInput);
                SetActiveBootstrap(bootstrap);
                SetActiveContext(context);

                Assert.AreSame(contextInput, SceneRuntimeReferenceUtility.ResolveInputReader(null));

                SetPrivateField<InputReader>(context, "inputReader", null);
                Assert.AreSame(bootstrapInput, SceneRuntimeReferenceUtility.ResolveInputReader(null));
            }
            finally
            {
                Object.DestroyImmediate(contextInputObject);
                Object.DestroyImmediate(contextObject);
                Object.DestroyImmediate(bootstrapInputObject);
                Object.DestroyImmediate(bootstrapObject);
            }
        }

        [Test]
        public void ResolvePlayerAndLockOnSelector_FallBackToSceneObjects_WithoutContext()
        {
            GameObject playerObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                LockOnTargetSelector selector = playerObject.AddComponent<LockOnTargetSelector>();

                Assert.AreSame(player, SceneRuntimeReferenceUtility.ResolvePlayerCharacter(null));
                Assert.AreSame(selector, SceneRuntimeReferenceUtility.ResolveLockOnTargetSelector(null, player));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ResolveCameraTransform_UsesController_BeforeMainCameraFallback()
        {
            GameObject cameraRigObject = new GameObject("CameraRig");
            GameObject mainCameraObject = new GameObject("MainCamera");

            try
            {
                ThirdPersonCameraController cameraController = cameraRigObject.AddComponent<ThirdPersonCameraController>();
                UnityEngine.Camera mainCamera = mainCameraObject.AddComponent<UnityEngine.Camera>();
                mainCamera.tag = "MainCamera";

                Assert.AreSame(
                    cameraController.transform,
                    SceneRuntimeReferenceUtility.ResolveCameraTransform(null, null, cameraController));

                Object.DestroyImmediate(cameraRigObject);
                cameraRigObject = null;

                Assert.AreSame(
                    mainCamera.transform,
                    SceneRuntimeReferenceUtility.ResolveCameraTransform(null));
            }
            finally
            {
                Object.DestroyImmediate(mainCameraObject);
                Object.DestroyImmediate(cameraRigObject);
            }
        }

        [Test]
        public void ResolveSaveServices_UsesActiveContext()
        {
            GameObject contextObject = new GameObject("SceneRuntimeContext");
            GameObject checkpointObject = new GameObject("CheckpointCoordinator");
            GameObject progressObject = new GameObject("ChapterProgress");

            try
            {
                SceneRuntimeContext context = contextObject.AddComponent<SceneRuntimeContext>();
                CheckpointRestoreCoordinator checkpointCoordinator = checkpointObject.AddComponent<CheckpointRestoreCoordinator>();
                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();

                SetPrivateField(context, "checkpointRestoreCoordinator", checkpointCoordinator);
                SetPrivateField(context, "chapterProgressService", progressService);
                SetActiveContext(context);

                Assert.AreSame(
                    checkpointCoordinator,
                    SceneRuntimeReferenceUtility.ResolveCheckpointRestoreCoordinator(null));
                Assert.AreSame(
                    progressService,
                    SceneRuntimeReferenceUtility.ResolveChapterProgressService(null));
            }
            finally
            {
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(checkpointObject);
                Object.DestroyImmediate(contextObject);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void SetActiveBootstrap(GameBootstrap bootstrap)
        {
            PropertyInfo property = typeof(GameBootstrap).GetProperty(
                "Active",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property);
            property.SetValue(null, bootstrap);
        }

        private static void SetActiveContext(SceneRuntimeContext context)
        {
            PropertyInfo property = typeof(SceneRuntimeContext).GetProperty(
                "Active",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property);
            property.SetValue(null, context);
        }
    }
}
