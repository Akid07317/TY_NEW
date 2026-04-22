using System.Reflection;
using System.IO;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Core;
using CampusRPG.Editor;
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
            PlayerMovementProbe movementProbe = player.GetComponent<PlayerMovementProbe>();
            ThirdPersonCameraController cameraController = FindRequiredComponent<ThirdPersonCameraController>("Main Camera");
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            SceneRuntimeContext sceneContext = FindRequiredComponent<SceneRuntimeContext>("SceneRuntimeContext");
            CombatDebugHUD debugHud = FindRequiredComponent<CombatDebugHUD>("CombatDebugHUD");
            Transform probeOrigin = movementProbe != null ? GetPrivateField<Transform>(movementProbe, "probeOrigin") : null;

            Assert.IsNotNull(inputReader);
            Assert.IsNotNull(lockOnTargetSelector);
            Assert.IsNotNull(movementProbe);
            Assert.IsNotNull(probeOrigin);
            Assert.AreSame(inputReader, bootstrap.InputReader);
            Assert.AreSame(inputReader, player.InputReader);
            Assert.AreSame(cameraController.transform, player.CameraTransform);
            Assert.AreSame(lockOnTargetSelector, player.LockOnTargetSelector);
            Assert.AreSame(movementProbe, player.MovementProbe);
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

        [Test]
        public void EnemyVisualPresentationRelays_AreWiredToProxyRoots()
        {
            string[] enemyNames =
            {
                "Enemy_Melee_A",
                "Enemy_Mobile_A",
                "Enemy_Ranged_A"
            };

            for (int i = 0; i < enemyNames.Length; i++)
            {
                EnemyBrain enemyBrain = FindRequiredComponent<EnemyBrain>(enemyNames[i]);
                EnemyStateMachine enemyStateMachine = FindRequiredComponent<EnemyStateMachine>(enemyNames[i]);
                EnemyVisualPresentationRelay relay = FindRequiredComponent<EnemyVisualPresentationRelay>(enemyNames[i]);
                Transform visualRoot = GetPrivateField<Transform>(relay, "visualRoot");
                Transform accentTransform = GetPrivateField<Transform>(relay, "accentTransform");

                Assert.IsNull(enemyBrain.GetComponent<Animator>(), enemyNames[i]);
                Assert.IsNull(enemyBrain.GetComponent<EnemyCombatAnimationRelay>(), enemyNames[i]);
                Assert.AreSame(enemyBrain, GetPrivateField<EnemyBrain>(relay, "enemyBrain"));
                Assert.AreSame(enemyStateMachine, GetPrivateField<EnemyStateMachine>(relay, "stateMachine"));
                Assert.IsNotNull(visualRoot, enemyNames[i]);
                Assert.AreEqual("CombatProxyVisualRoot", visualRoot.name, enemyNames[i]);
                Assert.IsTrue(relay.enabled, enemyNames[i]);
                Assert.IsNull(enemyBrain.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), enemyNames[i]);
                Assert.IsNull(visualRoot.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), enemyNames[i]);
                Assert.IsNotNull(accentTransform, enemyNames[i]);
                Assert.IsTrue(accentTransform.IsChildOf(visualRoot), enemyNames[i]);
            }
        }

        [Test]
        public void SceneFile_ContainsBakedNavMeshData()
        {
            string sceneYaml = File.ReadAllText(ScenePath);
            StringAssert.DoesNotContain("m_NavMeshData: {fileID: 0}", sceneYaml);
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
