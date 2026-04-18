using System.Reflection;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Core;
using CampusRPG.Input;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatTestSceneWiringTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/CombatTest.unity";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void SceneContainsRequiredGameplayObjects()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            string[] requiredObjectNames =
            {
                "Bootstrap",
                "Main Camera",
                "Ground",
                "PlayerSpawn",
                "EnemySpawn_Melee",
                "EnemySpawn_Mobile",
                "EnemySpawn_Ranged",
                "Player",
                "Enemy_Melee_A",
                "Enemy_Mobile_A",
                "Enemy_Ranged_A",
                "CombatDebugHUD",
                "CheckpointFlow",
                "SceneRuntimeContext",
                "Checkpoint_CP01"
            };

            for (int i = 0; i < requiredObjectNames.Length; i++)
            {
                Assert.IsNotNull(GameObject.Find(requiredObjectNames[i]), requiredObjectNames[i]);
            }
        }

        [Test]
        public void PlayerCameraAndSceneContextReferences_AreWired()
        {
            GameBootstrap bootstrap = FindRequiredComponent<GameBootstrap>("Bootstrap");
            InputReader inputReader = bootstrap.GetComponent<InputReader>();
            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            ThirdPersonCameraController cameraController = FindRequiredComponent<ThirdPersonCameraController>("Main Camera");
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            SceneRuntimeContext sceneContext = FindRequiredComponent<SceneRuntimeContext>("SceneRuntimeContext");
            CombatDebugHUD debugHud = FindRequiredComponent<CombatDebugHUD>("CombatDebugHUD");

            Assert.IsNotNull(inputReader);
            Assert.IsNotNull(lockOnTargetSelector);
            Assert.AreSame(inputReader, bootstrap.InputReader);
            Assert.AreSame(inputReader, player.InputReader);
            Assert.AreSame(cameraController.transform, player.CameraTransform);
            Assert.AreSame(player.transform, cameraController.FollowTarget);
            Assert.AreSame(inputReader, GetPrivateField<InputReader>(lockOnTargetSelector, "inputReader"));
            Assert.AreSame(cameraController, GetPrivateField<ThirdPersonCameraController>(lockOnTargetSelector, "cameraController"));
            Assert.AreSame(cameraController.transform, GetPrivateField<Transform>(lockOnTargetSelector, "cameraTransform"));
            Assert.AreSame(bootstrap, sceneContext.Bootstrap);
            Assert.AreSame(inputReader, sceneContext.InputReader);
            Assert.AreSame(player, sceneContext.PlayerCharacter);
            Assert.AreSame(cameraController, sceneContext.CameraController);
            Assert.AreSame(lockOnTargetSelector, sceneContext.LockOnTargetSelector);
            Assert.IsNotNull(sceneContext.AudioSettings);
            Assert.AreSame(player, GetPrivateField<PlayerCharacter>(debugHud, "playerCharacter"));
            Assert.AreSame(lockOnTargetSelector, GetPrivateField<LockOnTargetSelector>(debugHud, "lockOnTargetSelector"));
        }

        [Test]
        public void CheckpointFlow_IsConnectedToPlayerAndAnchor()
        {
            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            CheckpointRestoreCoordinator checkpointCoordinator = FindRequiredComponent<CheckpointRestoreCoordinator>("CheckpointFlow");
            SaveService saveService = checkpointCoordinator.GetComponent<SaveService>();
            CheckpointService checkpointService = checkpointCoordinator.GetComponent<CheckpointService>();
            CheckpointRuntimeAnchor checkpointAnchor = FindRequiredComponent<CheckpointRuntimeAnchor>("Checkpoint_CP01");
            BoxCollider checkpointCollider = checkpointAnchor.GetComponent<BoxCollider>();

            Assert.IsNotNull(saveService);
            Assert.IsNotNull(checkpointService);
            Assert.IsNotNull(checkpointCollider);
            Assert.IsTrue(checkpointCollider.isTrigger);
            Assert.AreSame(player, GetPrivateField<PlayerCharacter>(checkpointCoordinator, "player"));
            Assert.AreSame(saveService, GetPrivateField<SaveService>(checkpointCoordinator, "saveService"));
            Assert.AreSame(checkpointService, GetPrivateField<CheckpointService>(checkpointCoordinator, "checkpointService"));
            Assert.AreEqual("CombatTest", GetPrivateField<string>(checkpointCoordinator, "chapterId"));
            Assert.AreEqual("CP01", GetPrivateField<string>(checkpointCoordinator, "defaultCheckpointId"));
            Assert.AreSame(checkpointCoordinator, GetPrivateField<CheckpointRestoreCoordinator>(checkpointAnchor, "coordinator"));
            Assert.AreEqual("CP01", checkpointAnchor.CheckpointId);
        }

        private static TComponent FindRequiredComponent<TComponent>(string objectName) where TComponent : Component
        {
            GameObject gameObject = GameObject.Find(objectName);
            Assert.IsNotNull(gameObject, objectName);

            TComponent component = gameObject.GetComponent<TComponent>();
            Assert.IsNotNull(component, typeof(TComponent).Name + " on " + objectName);
            return component;
        }

        private static TField GetPrivateField<TField>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TField)field.GetValue(instance);
        }
    }
}
