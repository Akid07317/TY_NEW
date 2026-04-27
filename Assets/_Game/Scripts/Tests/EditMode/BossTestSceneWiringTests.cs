using System.IO;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Composition;
using CampusRPG.Core;
using CampusRPG.Editor;
using CampusRPG.Input;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusRPG.Tests.EditMode
{
    public sealed class BossTestSceneWiringTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/BossTest.unity";
        private const string GatekeeperTelegraphStylePath = "Assets/_Game/Data/Enemies/SO_BossTelegraphStyle_Gatekeeper.asset";

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
        public void SceneContainsIndependentBossFightObjects()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            string[] requiredObjectNames =
            {
                "BossTestRoot",
                "BossTestArena",
                "BossTest_Ground",
                "BossTest_PlayerSpawn",
                "BossTest_BossSpawn",
                "Bootstrap",
                "BossTestFlow",
                "Checkpoint_BossTest_Start",
                "SceneRuntimeContext",
                "CombatDebugHUD",
                "BossPresentationRig",
                "Main Camera",
                "Player",
                "Encounter_BossTest_Gatekeeper",
                "Boss_Gatekeeper",
                "BossArenaBarrier"
            };

            for (int i = 0; i < requiredObjectNames.Length; i++)
            {
                Assert.IsNotNull(FindSceneObject(requiredObjectNames[i]), requiredObjectNames[i]);
            }
        }

        [Test]
        public void PlayerCameraHudAndSceneContext_AreWired()
        {
            GameBootstrap bootstrap = FindRequiredComponent<GameBootstrap>("Bootstrap");
            InputReader inputReader = bootstrap.GetComponent<InputReader>();
            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            PlayerMovementProbe movementProbe = player.GetComponent<PlayerMovementProbe>();
            ThirdPersonCameraController cameraController = FindRequiredComponent<ThirdPersonCameraController>("Main Camera");
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            CombatDebugHUD debugHud = FindRequiredComponent<CombatDebugHUD>("CombatDebugHUD");
            SceneRuntimeContext sceneContext = FindRequiredComponent<SceneRuntimeContext>("SceneRuntimeContext");

            Assert.IsNotNull(inputReader);
            Assert.IsNotNull(movementProbe);
            Assert.AreSame(inputReader, bootstrap.InputReader);
            Assert.AreSame(inputReader, player.InputReader);
            Assert.AreSame(cameraController.transform, player.CameraTransform);
            Assert.AreSame(lockOnTargetSelector, player.LockOnTargetSelector);
            Assert.AreSame(movementProbe, player.MovementProbe);
            Assert.AreSame(player.transform, cameraController.FollowTarget);
            Assert.AreSame(inputReader, GetPrivateField<InputReader>(lockOnTargetSelector, "inputReader"));
            Assert.AreSame(cameraController, GetPrivateField<ThirdPersonCameraController>(lockOnTargetSelector, "cameraController"));
            Assert.AreSame(cameraController.transform, GetPrivateField<Transform>(lockOnTargetSelector, "cameraTransform"));
            Assert.AreSame(player, GetPrivateField<PlayerCharacter>(debugHud, "playerCharacter"));
            Assert.AreSame(lockOnTargetSelector, GetPrivateField<LockOnTargetSelector>(debugHud, "lockOnTargetSelector"));
            Assert.AreSame(bootstrap, sceneContext.Bootstrap);
            Assert.AreSame(inputReader, sceneContext.InputReader);
            Assert.AreSame(player, sceneContext.PlayerCharacter);
            Assert.AreSame(cameraController, sceneContext.CameraController);
            Assert.AreSame(lockOnTargetSelector, sceneContext.LockOnTargetSelector);
            Assert.IsNotNull(sceneContext.AudioSettings);
        }

        [Test]
        public void BossEncounter_UsesGatekeeperContractAndPresentationRig()
        {
            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("BossTestFlow");
            EncounterController encounter = FindRequiredComponent<EncounterController>("Encounter_BossTest_Gatekeeper");
            EnemyBrain boss = FindRequiredComponent<EnemyBrain>("Boss_Gatekeeper");
            EnemyEncounterMember member = FindRequiredComponent<EnemyEncounterMember>("Boss_Gatekeeper");
            BossPresentationRig rig = FindRequiredComponent<BossPresentationRig>("BossPresentationRig");
            BossTelegraphStyleSO expectedStyle = AssetDatabase.LoadAssetAtPath<BossTelegraphStyleSO>(GatekeeperTelegraphStylePath);
            GameObject barrier = FindSceneObject("BossArenaBarrier");

            Assert.IsNotNull(expectedStyle);
            Assert.AreEqual(Chapter01Ids.Encounters.Gatekeeper, encounter.EncounterId);
            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(encounter, "chapterProgressService"));
            Assert.AreSame(encounter, GetPrivateField<EncounterController>(member, "ownerEncounter"));
            Assert.AreSame(boss, GetPrivateField<EnemyBrain>(member, "enemyBrain"));
            Assert.AreEqual(EnemyArchetypeType.Boss, boss.Archetype.ArchetypeType);
            Assert.IsNotNull(boss.Health);
            Assert.IsNotNull(boss.AttackController);
            Assert.IsNotNull(boss.GetComponent<LockOnTarget>());
            Assert.IsNotNull(boss.transform.Find("CombatProxyVisualRoot"));
            Assert.AreSame(boss, GetPrivateField<EnemyBrain>(rig, "bossEnemy"));
            Assert.AreSame(encounter, GetPrivateField<EncounterController>(rig, "bossEncounter"));
            Assert.AreSame(expectedStyle, GetPrivateField<BossTelegraphStyleSO>(rig, "telegraphStyle"));
            CollectionAssert.Contains(GetPrivateField<GameObject[]>(encounter, "blockersToEnableWhileActive"), barrier);
        }

        [Test]
        public void CheckpointFlow_IsBoundToBossTestScene()
        {
            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            CheckpointRestoreCoordinator coordinator = FindRequiredComponent<CheckpointRestoreCoordinator>("BossTestFlow");
            SaveService saveService = coordinator.GetComponent<SaveService>();
            CheckpointService checkpointService = coordinator.GetComponent<CheckpointService>();
            ChapterProgressService progressService = coordinator.GetComponent<ChapterProgressService>();
            CheckpointRuntimeAnchor checkpointAnchor = FindRequiredComponent<CheckpointRuntimeAnchor>("Checkpoint_BossTest_Start");

            Assert.IsNotNull(saveService);
            Assert.IsNotNull(checkpointService);
            Assert.IsNotNull(progressService);
            Assert.AreSame(player, GetPrivateField<PlayerCharacter>(coordinator, "player"));
            Assert.AreSame(saveService, GetPrivateField<SaveService>(coordinator, "saveService"));
            Assert.AreSame(checkpointService, GetPrivateField<CheckpointService>(coordinator, "checkpointService"));
            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(coordinator, "chapterProgressService"));
            Assert.AreEqual(Chapter01Ids.Chapter, GetPrivateField<string>(coordinator, "chapterId"));
            Assert.AreEqual("BossTest_Start", GetPrivateField<string>(coordinator, "defaultCheckpointId"));
            Assert.AreSame(coordinator, GetPrivateField<CheckpointRestoreCoordinator>(checkpointAnchor, "coordinator"));
            Assert.AreEqual("BossTest_Start", checkpointAnchor.CheckpointId);
        }

        [Test]
        public void SceneFile_ContainsBakedNavMeshData()
        {
            string sceneYaml = File.ReadAllText(ScenePath);
            StringAssert.DoesNotContain("m_NavMeshData: {fileID: 0}", sceneYaml);
        }

        private static TComponent FindRequiredComponent<TComponent>(string objectName) where TComponent : Component
        {
            GameObject gameObject = FindSceneObject(objectName);
            Assert.IsNotNull(gameObject, objectName);

            TComponent component = gameObject.GetComponent<TComponent>();
            Assert.IsNotNull(component, typeof(TComponent).Name + " on " + objectName);
            return component;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject gameObject = gameObjects[i];

                if (gameObject.scene == SceneManager.GetActiveScene() && gameObject.name == objectName)
                {
                    return gameObject;
                }
            }

            return null;
        }

        private static TField GetPrivateField<TField>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TField)field.GetValue(instance);
        }
    }
}
