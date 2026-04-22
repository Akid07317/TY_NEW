using System.Reflection;
using System.Linq;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
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
    public sealed class Chapter01ProgressionSceneWiringTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Chapter01_Combined.unity";
        private static readonly string[] ImportedSourceRoots =
        {
            "Assets/Kevin Iglesias/",
            "Assets/DoubleL/",
            "Assets/ithappy/",
            "Assets/JC_LP_MedievalCharacters_LITE/"
        };

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
        public void Chapter01_GatingObjects_UseExpectedProgressionIds()
        {
            AssertSceneOpen();

            DoorController bossGateDoor = FindRequiredComponent<DoorController>("Door_A03_To_A04");
            DoorController ritualCoreDoor = FindRequiredComponent<DoorController>("Door_A04_To_RitualCore");
            KeyItemPickup gateSigilPickup = FindRequiredComponent<KeyItemPickup>("Pickup_GateSigil");
            KeyItemPickup ritualCorePickup = FindRequiredComponent<KeyItemPickup>("Pickup_RitualCore");
            KeyItemBeaconView ritualCoreBeacon = FindRequiredComponent<KeyItemBeaconView>("Pickup_RitualCore");

            Assert.AreEqual(Chapter01Ids.KeyItems.GateSigil, GetPrivateField<string>(bossGateDoor, "requiredKeyItemId"));
            Assert.AreEqual(string.Empty, GetPrivateField<string>(bossGateDoor, "requiredEncounterId"));
            Assert.AreEqual(Chapter01Ids.Encounters.Gatekeeper, GetPrivateField<string>(ritualCoreDoor, "requiredEncounterId"));
            Assert.AreEqual(string.Empty, GetPrivateField<string>(ritualCoreDoor, "requiredKeyItemId"));
            Assert.AreEqual(Chapter01Ids.KeyItems.GateSigil, GetPrivateField<string>(gateSigilPickup, "keyItemId"));
            Assert.IsFalse(GetPrivateField<bool>(gateSigilPickup, "completeChapterOnPickup"));
            Assert.AreEqual(Chapter01Ids.KeyItems.RitualCore, GetPrivateField<string>(ritualCorePickup, "keyItemId"));
            Assert.IsTrue(GetPrivateField<bool>(ritualCorePickup, "completeChapterOnPickup"));
            Assert.AreEqual(Chapter01Ids.Encounters.Gatekeeper, GetPrivateField<string>(ritualCoreBeacon, "requiredEncounterId"));
        }

        [Test]
        public void Chapter01_DoorRequirementHintTriggers_UseExpectedProgressionIds()
        {
            AssertSceneOpen();

            DoorRequirementHintTrigger tutorialHintTrigger = FindRequiredComponent<DoorRequirementHintTrigger>("Hint_Door_A01_To_A02");
            DoorRequirementHintTrigger courtyardHintTrigger = FindRequiredComponent<DoorRequirementHintTrigger>("Hint_Door_A02_To_A03");
            DoorRequirementHintTrigger bossGateHintTrigger = FindRequiredComponent<DoorRequirementHintTrigger>("Hint_Door_A03_To_A04");
            DoorRequirementHintTrigger ritualCoreHintTrigger = FindRequiredComponent<DoorRequirementHintTrigger>("Hint_Door_A04_To_RitualCore");

            Assert.AreEqual(Chapter01Ids.Encounters.EntranceTutorial, GetPrivateField<string>(tutorialHintTrigger, "requiredEncounterId"));
            Assert.AreEqual(Chapter01Ids.Encounters.Courtyard, GetPrivateField<string>(courtyardHintTrigger, "requiredEncounterId"));
            Assert.AreEqual(Chapter01Ids.KeyItems.GateSigil, GetPrivateField<string>(bossGateHintTrigger, "requiredKeyItemId"));
            Assert.AreEqual(Chapter01Ids.Encounters.Gatekeeper, GetPrivateField<string>(ritualCoreHintTrigger, "requiredEncounterId"));
        }

        [Test]
        public void Chapter01_BossClosurePresenters_AreBoundToChapterFlow()
        {
            AssertSceneOpen();

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            ChapterCompleteView completeView = FindRequiredComponent<ChapterCompleteView>("ChapterCompleteView");
            BossPresentationRig rig = FindRequiredComponent<BossPresentationRig>("BossPresentationRig");
            BossArenaStatusPresenter arenaStatusPresenter = FindRequiredComponent<BossArenaStatusPresenter>("BossPresentationRig");
            EncounterController gatekeeperEncounter = FindRequiredComponent<EncounterController>("Encounter_EN_A04_GATEKEEPER");
            EnemyBrain gatekeeperBoss = GetPrivateField<EnemyBrain>(rig, "bossEnemy");

            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(completeView, "chapterProgressService"));
            Assert.AreSame(gatekeeperEncounter, GetPrivateField<EncounterController>(rig, "bossEncounter"));
            Assert.AreSame(gatekeeperEncounter, GetPrivateField<EncounterController>(arenaStatusPresenter, "bossEncounter"));
            Assert.IsNotNull(gatekeeperBoss);
            Assert.AreEqual("Boss_Gatekeeper", gatekeeperBoss.gameObject.name);
        }

        [Test]
        public void Chapter01_ObjectiveView_IsBoundToChapterFlow()
        {
            AssertSceneOpen();

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            ChapterObjectiveView objectiveView = FindRequiredComponent<ChapterObjectiveView>("ChapterFlow");

            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(objectiveView, "chapterProgressService"));
        }

        [Test]
        public void Chapter01_AreaEntryView_IsBoundToChapterFlow()
        {
            AssertSceneOpen();

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            AreaEntryView areaEntryView = FindRequiredComponent<AreaEntryView>("ChapterFlow");

            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(areaEntryView, "chapterProgressService"));
        }

        [Test]
        public void Chapter01_ResumeContextView_IsBoundToChapterFlow()
        {
            AssertSceneOpen();

            SaveService saveService = FindRequiredComponent<SaveService>("ChapterFlow");
            ChapterResumeContextView resumeContextView = FindRequiredComponent<ChapterResumeContextView>("ChapterFlow");

            Assert.AreSame(saveService, GetPrivateField<SaveService>(resumeContextView, "saveService"));
        }

        [Test]
        public void Chapter01_PlayerTraversalHooks_AreWiredForLockOnAndMantle()
        {
            AssertSceneOpen();

            GameBootstrap bootstrap = FindRequiredComponent<GameBootstrap>("Bootstrap");
            InputReader inputReader = bootstrap.GetComponent<InputReader>();
            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            PlayerMovementProbe movementProbe = FindRequiredComponent<PlayerMovementProbe>("Player");
            ThirdPersonCameraController cameraController = FindRequiredComponent<ThirdPersonCameraController>("Main Camera");
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            SceneRuntimeContext sceneContext = FindRequiredComponent<SceneRuntimeContext>("SceneRuntimeContext");
            Transform probeOrigin = GetPrivateField<Transform>(movementProbe, "probeOrigin");

            Assert.IsNotNull(inputReader);
            Assert.IsNotNull(lockOnTargetSelector);
            Assert.IsNotNull(player.BaseStats);
            Assert.AreSame(inputReader, player.InputReader);
            Assert.AreSame(cameraController.transform, player.CameraTransform);
            Assert.AreSame(lockOnTargetSelector, player.LockOnTargetSelector);
            Assert.AreSame(movementProbe, player.MovementProbe);
            Assert.IsNotNull(probeOrigin);
            Assert.AreSame(inputReader, GetPrivateField<InputReader>(lockOnTargetSelector, "inputReader"));
            Assert.AreSame(cameraController, GetPrivateField<ThirdPersonCameraController>(lockOnTargetSelector, "cameraController"));
            Assert.AreSame(cameraController.transform, GetPrivateField<Transform>(lockOnTargetSelector, "cameraTransform"));
            Assert.AreSame(player, sceneContext.PlayerCharacter);
            Assert.AreSame(cameraController, sceneContext.CameraController);
            Assert.AreSame(lockOnTargetSelector, sceneContext.LockOnTargetSelector);
        }

        [Test]
        public void Chapter01_CombatActors_UsePublicProxyVisualBaseline()
        {
            AssertSceneOpen();

            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            Animator animator = player.GetComponent<Animator>();
            PlayerCombatAnimationRelay relay = player.GetComponent<PlayerCombatAnimationRelay>();
            EnemyBrain[] enemies = Object.FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.IsNotNull(animator);
            Assert.IsNotNull(relay);
            Assert.IsNull(animator.avatar);
            Assert.IsNull(player.transform.Find("ImportedVisualRoot"));
            Assert.IsNotNull(player.transform.Find("CombatProxyVisualRoot"));
            Assert.IsNull(GetPrivateField<Transform>(relay, "proxyWeaponGrip"));
            Assert.That(enemies, Is.Not.Empty);

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyBrain enemy = enemies[i];
                EnemyVisualPresentationRelay enemyRelay = enemy.GetComponent<EnemyVisualPresentationRelay>();
                Transform visualRoot = enemy.transform.Find("CombatProxyVisualRoot");

                Assert.IsNotNull(enemyRelay, enemy.name);
                Assert.IsNull(enemy.GetComponent<Animator>(), enemy.name);
                Assert.IsNull(enemy.GetComponent<EnemyCombatAnimationRelay>(), enemy.name);
                Assert.IsNotNull(visualRoot, enemy.name);
                Assert.IsNull(enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), enemy.name);
                Assert.IsNull(visualRoot.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), enemy.name);
                Assert.IsNotNull(EnemyVisualPresentationRelay.FindDefaultAccentTransform(visualRoot), enemy.name);
            }
        }

        [Test]
        public void Chapter01_CheckpointActivationView_IsBoundToChapterFlow()
        {
            AssertSceneOpen();

            CheckpointService checkpointService = FindRequiredComponent<CheckpointService>("ChapterFlow");
            CheckpointActivationView checkpointActivationView = FindRequiredComponent<CheckpointActivationView>("ChapterFlow");

            Assert.AreSame(checkpointService, GetPrivateField<CheckpointService>(checkpointActivationView, "checkpointService"));
        }

        [Test]
        public void Chapter01_KeyItemAcquisitionView_IsBoundToChapterFlow()
        {
            AssertSceneOpen();

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            KeyItemAcquisitionView keyItemAcquisitionView = FindRequiredComponent<KeyItemAcquisitionView>("ChapterFlow");

            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(keyItemAcquisitionView, "chapterProgressService"));
        }

        [Test]
        public void Chapter01_EncounterClearView_IsBoundToChapterFlow()
        {
            AssertSceneOpen();

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            EncounterClearView encounterClearView = FindRequiredComponent<EncounterClearView>("ChapterFlow");

            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(encounterClearView, "chapterProgressService"));
        }

        [Test]
        public void Chapter01_EncounterSealView_ExistsOnChapterFlow()
        {
            AssertSceneOpen();

            EncounterSealView encounterSealView = FindRequiredComponent<EncounterSealView>("ChapterFlow");

            Assert.IsNotNull(encounterSealView);
        }

        [Test]
        public void Chapter01_RouteBlockHintView_ExistsOnChapterFlow()
        {
            AssertSceneOpen();

            ChapterRouteBlockHintView routeBlockHintView = FindRequiredComponent<ChapterRouteBlockHintView>("ChapterFlow");

            Assert.IsNotNull(routeBlockHintView);
        }

        [Test]
        public void Chapter01_TutorialHintView_IsBoundToChapterFlowAndBootstrapInput()
        {
            AssertSceneOpen();

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            ChapterTutorialHintView tutorialHintView = FindRequiredComponent<ChapterTutorialHintView>("ChapterFlow");
            InputReader inputReader = FindRequiredComponent<InputReader>("Bootstrap");

            Assert.AreSame(progressService, GetPrivateField<ChapterProgressService>(tutorialHintView, "chapterProgressService"));
            Assert.AreSame(inputReader, GetPrivateField<InputReader>(tutorialHintView, "inputReader"));
        }

        [Test]
        public void Chapter01_InteriorEncounter_UsesMixedEnemyCompositionAndSigilLockRoom()
        {
            AssertSceneOpen();

            EncounterController interiorEncounter = FindRequiredComponent<EncounterController>("Encounter_EN_A03_INTERIOR");
            GameObject entryBarrier = FindSceneObject("InteriorEncounterBarrier_Entry");
            GameObject sigilBarrier = FindSceneObject("InteriorEncounterBarrier_Sigil");
            EnemyBrain meleeEnemy = FindRequiredComponent<EnemyBrain>("Enemy_A03_Melee_A");
            EnemyBrain mobileEnemy = FindRequiredComponent<EnemyBrain>("Enemy_A03_Mobile_A");
            EnemyBrain rangedEnemy = FindRequiredComponent<EnemyBrain>("Enemy_A03_Ranged_A");
            GameObject[] blockers = GetPrivateField<GameObject[]>(interiorEncounter, "blockersToEnableWhileActive");

            Assert.IsNotNull(entryBarrier);
            Assert.IsNotNull(sigilBarrier);
            Assert.That(blockers, Has.Length.EqualTo(2));
            Assert.Contains(entryBarrier, blockers);
            Assert.Contains(sigilBarrier, blockers);
            Assert.AreEqual(EnemyArchetypeType.Melee, meleeEnemy.Archetype.ArchetypeType);
            Assert.AreEqual(EnemyArchetypeType.Mobile, mobileEnemy.Archetype.ArchetypeType);
            Assert.AreEqual(EnemyArchetypeType.Ranged, rangedEnemy.Archetype.ArchetypeType);
        }

        [Test]
        public void Chapter01_InteriorTraversalObstacle_IsMantleableFromMainRoute()
        {
            AssertSceneOpen();

            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            PlayerMovementProbe movementProbe = FindRequiredComponent<PlayerMovementProbe>("Player");
            GameObject obstacle = FindSceneObject("TraversalMantle_InteriorApproach");
            BoxCollider collider = obstacle != null ? obstacle.GetComponent<BoxCollider>() : null;

            Assert.IsNotNull(obstacle);
            Assert.IsNotNull(collider);
            Assert.IsNotNull(player.BaseStats);

            Bounds bounds = collider.bounds;
            Assert.GreaterOrEqual(bounds.size.y, player.BaseStats.MantleMinHeight);
            Assert.LessOrEqual(bounds.size.y, player.BaseStats.MantleMaxHeight);

            Vector3 originalPosition = player.transform.position;
            Quaternion originalRotation = player.transform.rotation;

            try
            {
                Vector3 startPosition = new Vector3(bounds.center.x, originalPosition.y, bounds.min.z - 0.55f);
                player.transform.SetPositionAndRotation(startPosition, Quaternion.identity);
                Physics.SyncTransforms();

                bool foundTarget = movementProbe.TryFindMantleTarget(player.BaseStats, player.transform, out Vector3 mantleTarget);

                Assert.IsTrue(foundTarget);
                Assert.Greater(mantleTarget.y, originalPosition.y + 0.45f);
                Assert.Greater(mantleTarget.z, bounds.min.z);
            }
            finally
            {
                player.transform.SetPositionAndRotation(originalPosition, originalRotation);
                Physics.SyncTransforms();
            }
        }

        [Test]
        public void Chapter01_BaselineSceneDependencies_DoNotDependOnImportedSourceDirectories()
        {
            string[] dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            string[] importedDependencies = dependencies.Where(IsImportedSourcePath).ToArray();

            CollectionAssert.IsEmpty(
                importedDependencies,
                importedDependencies.Length == 0 ? string.Empty : string.Join("\n", importedDependencies));
        }

        private static void AssertSceneOpen()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);
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
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);

                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                    {
                        return transforms[j].gameObject;
                    }
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

        private static bool IsImportedSourcePath(string path)
        {
            return ImportedSourceRoots.Any(root => path.StartsWith(root));
        }
    }
}
