using System.IO;
using System.Linq;
using System.Reflection;
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
        private const string MapDefinitionPath = "Assets/_Game/Data/Chapter/SO_Chapter01_MapDefinition.asset";
        private static readonly string[] ImportedSourceRoots =
        {
            "Assets/Kevin Iglesias/",
            "Assets/DoubleL/",
            "Assets/ithappy/",
            "Assets/JC_LP_MedievalCharacters_LITE/",
            "Assets/Free medieval weapons/",
            "Assets/GhostSamurai_Animset/",
            "Assets/MYFG-Weapon Pack Lite/",
            "Assets/Polytope Studio/"
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
            Assert.AreEqual(Chapter01Ids.Encounters.Interior, GetPrivateField<string>(gateSigilPickup, "requiredEncounterId"));
            Assert.IsFalse(GetPrivateField<bool>(gateSigilPickup, "completeChapterOnPickup"));
            Assert.AreEqual(Chapter01Ids.KeyItems.RitualCore, GetPrivateField<string>(ritualCorePickup, "keyItemId"));
            Assert.AreEqual(Chapter01Ids.Encounters.Gatekeeper, GetPrivateField<string>(ritualCorePickup, "requiredEncounterId"));
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
            EnemyBrain[] enemies = Object.FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include);

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
        public void Chapter01_MapZones_FormFiveReadableActionBeats()
        {
            AssertSceneOpen();

            GameObject zonesRoot = FindSceneObject("Chapter01_MapZones");
            BoxCollider entranceZone = FindRequiredComponent<BoxCollider>("Zone01_EntranceTutorial");
            BoxCollider courtyardZone = FindRequiredComponent<BoxCollider>("Zone02_CourtyardArena");
            BoxCollider narrowHallZone = FindRequiredComponent<BoxCollider>("Zone03_InteriorNarrowHall");
            BoxCollider sideRouteZone = FindRequiredComponent<BoxCollider>("Zone04_SideRouteShortcut");
            BoxCollider bossZone = FindRequiredComponent<BoxCollider>("Zone05_BossApproachAndArena");

            Assert.IsNotNull(zonesRoot);
            Assert.AreEqual(5, zonesRoot.transform.childCount);
            Assert.IsTrue(entranceZone.isTrigger);
            Assert.IsTrue(courtyardZone.isTrigger);
            Assert.IsTrue(narrowHallZone.isTrigger);
            Assert.IsTrue(sideRouteZone.isTrigger);
            Assert.IsTrue(bossZone.isTrigger);
            Assert.Less(entranceZone.transform.position.z, courtyardZone.transform.position.z);
            Assert.Less(courtyardZone.transform.position.z, narrowHallZone.transform.position.z);
            Assert.Less(narrowHallZone.transform.position.z, sideRouteZone.transform.position.z);
            Assert.Less(sideRouteZone.transform.position.z, bossZone.transform.position.z);
            Assert.GreaterOrEqual(courtyardZone.size.x, 18f);
            Assert.LessOrEqual(narrowHallZone.size.x, 10f);
            Assert.Less(sideRouteZone.transform.position.x, -2f);
        }

        [Test]
        public void Chapter01_MapDefinitionAsset_DescribesFiveZonesAndRouteGates()
        {
            ChapterMapDefinitionSO mapDefinition = LoadMapDefinition();

            Assert.AreEqual(Chapter01Ids.Chapter, mapDefinition.ChapterId);
            Assert.That(mapDefinition.Zones, Has.Length.EqualTo(5));
            Assert.That(mapDefinition.RouteGates, Has.Length.EqualTo(5));

            AssertMapZone(
                mapDefinition,
                "zone01_entrance_tutorial",
                "Zone01_EntranceTutorial",
                Chapter01Ids.Areas.Entrance,
                Chapter01Ids.Encounters.EntranceTutorial,
                Chapter01Ids.Checkpoints.Start,
                string.Empty,
                false);
            AssertMapZone(
                mapDefinition,
                "zone02_courtyard_arena",
                "Zone02_CourtyardArena",
                Chapter01Ids.Areas.Courtyard,
                Chapter01Ids.Encounters.Courtyard,
                Chapter01Ids.Checkpoints.Courtyard,
                string.Empty,
                false);
            AssertMapZone(
                mapDefinition,
                "zone03_interior_narrow_hall",
                "Zone03_InteriorNarrowHall",
                Chapter01Ids.Areas.Interior,
                Chapter01Ids.Encounters.Interior,
                Chapter01Ids.Checkpoints.Interior,
                Chapter01Ids.KeyItems.GateSigil,
                false);
            AssertMapZone(
                mapDefinition,
                "zone04_side_route_shortcut",
                "Zone04_SideRouteShortcut",
                Chapter01Ids.Areas.Interior,
                string.Empty,
                Chapter01Ids.Checkpoints.Interior,
                Chapter01Ids.KeyItems.SideRouteCache,
                true);
            AssertMapZone(
                mapDefinition,
                "zone05_boss_approach_and_arena",
                "Zone05_BossApproachAndArena",
                Chapter01Ids.Areas.Boss,
                Chapter01Ids.Encounters.Gatekeeper,
                Chapter01Ids.Checkpoints.Interior,
                Chapter01Ids.KeyItems.RitualCore,
                false);

            AssertRouteGate(
                mapDefinition,
                "route_gate_a01_to_a02",
                "zone01_entrance_tutorial",
                "zone02_courtyard_arena",
                Chapter01Ids.Encounters.EntranceTutorial,
                string.Empty,
                false);
            AssertRouteGate(
                mapDefinition,
                "route_gate_a02_to_a03",
                "zone02_courtyard_arena",
                "zone03_interior_narrow_hall",
                Chapter01Ids.Encounters.Courtyard,
                string.Empty,
                false);
            AssertRouteGate(
                mapDefinition,
                "route_gate_a03_side_shortcut",
                "zone03_interior_narrow_hall",
                "zone04_side_route_shortcut",
                Chapter01Ids.Encounters.Interior,
                string.Empty,
                true);
            AssertRouteGate(
                mapDefinition,
                "route_gate_a03_to_a04",
                "zone04_side_route_shortcut",
                "zone05_boss_approach_and_arena",
                string.Empty,
                Chapter01Ids.KeyItems.GateSigil,
                false);
            AssertRouteGate(
                mapDefinition,
                "route_gate_a04_to_ritual_core",
                "zone05_boss_approach_and_arena",
                "zone05_boss_approach_and_arena",
                Chapter01Ids.Encounters.Gatekeeper,
                string.Empty,
                false);
        }

        [Test]
        public void Chapter01_MapZoneMarkers_AreBoundToMapDefinitionData()
        {
            AssertSceneOpen();

            ChapterMapDefinitionSO mapDefinition = LoadMapDefinition();

            for (int i = 0; i < mapDefinition.Zones.Length; i++)
            {
                ChapterMapDefinitionSO.MapZoneDefinition zone = mapDefinition.Zones[i];
                ChapterMapZoneMarker marker = FindRequiredComponent<ChapterMapZoneMarker>(zone.SceneObjectName);
                BoxCollider collider = marker.GetComponent<BoxCollider>();

                Assert.AreSame(mapDefinition, marker.MapDefinition, zone.ZoneId);
                Assert.AreEqual(zone.ZoneId, marker.ZoneId);
                Assert.IsTrue(marker.TryGetDefinition(out ChapterMapDefinitionSO.MapZoneDefinition resolvedZone), zone.ZoneId);
                Assert.AreEqual(zone.SceneObjectName, resolvedZone.SceneObjectName);
                Assert.AreEqual(zone.Center, marker.transform.position);
                Assert.AreEqual(zone.Size, collider.size);
            }
        }

        [Test]
        public void Chapter01_SideRouteShortcut_ConsumesMapDataForRewardAndLoopback()
        {
            AssertSceneOpen();

            ChapterMapDefinitionSO mapDefinition = LoadMapDefinition();

            Assert.IsTrue(
                mapDefinition.TryGetZone("zone04_side_route_shortcut", out ChapterMapDefinitionSO.MapZoneDefinition sideRouteZone));
            Assert.IsTrue(sideRouteZone.OptionalRoute);
            Assert.AreEqual(Chapter01Ids.KeyItems.SideRouteCache, sideRouteZone.RewardKeyItemId);
            Assert.AreEqual(Chapter01Ids.Checkpoints.Interior, sideRouteZone.CheckpointId);

            Assert.IsTrue(
                mapDefinition.TryGetRouteGate("route_gate_a03_side_shortcut", out ChapterMapDefinitionSO.RouteGateDefinition shortcutGate));
            Assert.AreEqual("zone03_interior_narrow_hall", shortcutGate.FromZoneId);
            Assert.AreEqual("zone04_side_route_shortcut", shortcutGate.ToZoneId);
            Assert.AreEqual(Chapter01Ids.Encounters.Interior, shortcutGate.RequiredEncounterId);
            Assert.AreEqual(string.Empty, shortcutGate.RequiredKeyItemId);
            Assert.IsTrue(shortcutGate.OpensShortcut);

            ChapterMapZoneMarker marker = FindRequiredComponent<ChapterMapZoneMarker>("Zone04_SideRouteShortcut");
            Assert.IsTrue(marker.TryGetDefinition(out ChapterMapDefinitionSO.MapZoneDefinition markerZone));
            Assert.AreEqual(sideRouteZone.ZoneId, markerZone.ZoneId);
            Assert.AreEqual(Chapter01Ids.KeyItems.SideRouteCache, markerZone.RewardKeyItemId);
            Assert.AreEqual(true, markerZone.OptionalRoute);

            GameObject gateLeft = FindSceneObject("Zone04_ShortcutReturn_Gate_Left");
            GameObject gateRight = FindSceneObject("Zone04_ShortcutReturn_Gate_Right");
            Assert.IsNotNull(gateLeft);
            Assert.IsNotNull(gateRight);
            Assert.Less(gateLeft.transform.position.x, gateRight.transform.position.x);
            Assert.That(Mathf.Abs(gateLeft.transform.position.z - gateRight.transform.position.z), Is.LessThan(0.25f));
        }

        [Test]
        public void Chapter01_RouteGateDefinition_MatchesDoorRequirements()
        {
            AssertSceneOpen();

            ChapterMapDefinitionSO mapDefinition = LoadMapDefinition();

            AssertRouteGateMatchesDoor(mapDefinition, "route_gate_a01_to_a02", "Door_A01_To_A02");
            AssertRouteGateMatchesDoor(mapDefinition, "route_gate_a02_to_a03", "Door_A02_To_A03");
            AssertRouteGateMatchesDoor(mapDefinition, "route_gate_a03_to_a04", "Door_A03_To_A04");
            AssertRouteGateMatchesDoor(mapDefinition, "route_gate_a04_to_ritual_core", "Door_A04_To_RitualCore");
        }

        [Test]
        public void Chapter01_MainRouteConnectors_KeepFiveZoneRouteWalkable()
        {
            AssertSceneOpen();

            GameObject connectorA01A02 = FindSceneObject("Connector_A01_A02_Floor");
            GameObject connectorA02A03 = FindSceneObject("Connector_A02_A03_Floor");
            GameObject connectorA03A04 = FindSceneObject("Connector_A03_A04_Floor");

            AssertNavigationFloor(connectorA01A02, "Connector_A01_A02_Floor");
            AssertNavigationFloor(connectorA02A03, "Connector_A02_A03_Floor");
            AssertNavigationFloor(connectorA03A04, "Connector_A03_A04_Floor");
            Assert.IsNull(FindSceneObject("Wall_Front"));
            Assert.IsNull(FindSceneObject("Wall_Back"));
            Assert.IsNotNull(FindSceneObject("Wall_Front_Left"));
            Assert.IsNotNull(FindSceneObject("Wall_Front_Right"));
            Assert.Less(connectorA01A02.transform.position.z, connectorA02A03.transform.position.z);
            Assert.Less(connectorA02A03.transform.position.z, connectorA03A04.transform.position.z);
        }

        [Test]
        public void Chapter01_FiveZoneGraybox_AddsSideRouteAndBossAntechamberLandmarks()
        {
            AssertSceneOpen();

            AssertNavigationFloor(FindSceneObject("Zone02_LeftEvadeLane_Floor"), "Zone02_LeftEvadeLane_Floor");
            AssertNavigationFloor(FindSceneObject("Zone02_RightEvadeLane_Floor"), "Zone02_RightEvadeLane_Floor");
            AssertNavigationFloor(FindSceneObject("Zone04_SideRouteShortcut_Floor"), "Zone04_SideRouteShortcut_Floor");
            AssertNavigationFloor(FindSceneObject("Zone05_BossAntechamber_Floor"), "Zone05_BossAntechamber_Floor");
            AssertNavigationFloor(FindSceneObject("Zone05_BossArena_CenterRing"), "Zone05_BossArena_CenterRing");
            Assert.IsNotNull(FindSceneObject("Zone03_CameraPillar_Left_A"));
            Assert.IsNotNull(FindSceneObject("Zone03_CameraPillar_Right_B"));
            Assert.IsNotNull(FindSceneObject("Zone04_ShortcutReturn_Gate_Left"));
            Assert.IsNotNull(FindSceneObject("Zone04_ShortcutReturn_Gate_Right"));
            Assert.IsNotNull(FindSceneObject("Zone05_BossAntechamber_SupplyMarker"));
            Assert.IsNotNull(FindSceneObject("Zone05_BossArenaBoundary_Left"));
            Assert.IsNotNull(FindSceneObject("Zone05_BossArenaBoundary_Right"));
        }

        [Test]
        public void Chapter01_PublicSafeModularGreybox_AddsReadableModulesForFiveZones()
        {
            AssertSceneOpen();

            GameObject root = FindSceneObject("Chapter01_ModularGreybox");
            GameObject entranceRoot = FindSceneObject("Modular_Zone01_Entrance");
            GameObject courtyardRoot = FindSceneObject("Modular_Zone02_Courtyard");
            GameObject interiorRoot = FindSceneObject("Modular_Zone03_Interior");
            GameObject sideRouteRoot = FindSceneObject("Modular_Zone04_SideRoute");
            GameObject bossRoot = FindSceneObject("Modular_Zone05_BossApproach");

            Assert.IsNotNull(root);
            Assert.IsNotNull(entranceRoot);
            Assert.IsNotNull(courtyardRoot);
            Assert.IsNotNull(interiorRoot);
            Assert.IsNotNull(sideRouteRoot);
            Assert.IsNotNull(bossRoot);
            Assert.AreEqual(5, root.transform.childCount);
            Assert.AreSame(root.transform, entranceRoot.transform.parent);
            Assert.AreSame(root.transform, courtyardRoot.transform.parent);
            Assert.AreSame(root.transform, interiorRoot.transform.parent);
            Assert.AreSame(root.transform, sideRouteRoot.transform.parent);
            Assert.AreSame(root.transform, bossRoot.transform.parent);

            GameObject entranceLeft = AssertModularBlock("Modular_Zone01_EntranceArch_LeftPost");
            GameObject entranceRight = AssertModularBlock("Modular_Zone01_EntranceArch_RightPost");
            GameObject entranceBeam = AssertModularBlock("Modular_Zone01_EntranceArch_TopBeam");
            GameObject leftLaneRail = AssertModularBlock("Modular_Zone02_LeftLane_Rail_A");
            GameObject rightLaneRail = AssertModularBlock("Modular_Zone02_RightLane_Rail_A");
            GameObject ceilingBeam = AssertModularBlock("Modular_Zone03_CeilingBeam_A");
            GameObject sideRouteStep = AssertModularBlock("Modular_Zone04_SideRoute_Step_B");
            GameObject cachePlinth = AssertModularBlock("Modular_Zone04_SideRoute_CachePlinth");
            GameObject bossLeft = AssertModularBlock("Modular_Zone05_AntechamberArch_LeftPost");
            GameObject bossRight = AssertModularBlock("Modular_Zone05_AntechamberArch_RightPost");
            GameObject bossBeam = AssertModularBlock("Modular_Zone05_AntechamberArch_TopBeam");

            Assert.Less(entranceLeft.transform.position.x, -4f);
            Assert.Greater(entranceRight.transform.position.x, 4f);
            Assert.Greater(entranceBeam.transform.position.y, entranceLeft.transform.position.y);
            Assert.Less(leftLaneRail.transform.position.x, -7f);
            Assert.Greater(rightLaneRail.transform.position.x, 7f);
            Assert.Greater(ceilingBeam.transform.position.y, 3f);
            Assert.Less(sideRouteStep.transform.position.x, -4f);
            Assert.Less(cachePlinth.transform.position.x, -5f);
            Assert.Less(bossLeft.transform.position.x, -5f);
            Assert.Greater(bossRight.transform.position.x, 5f);
            Assert.Greater(bossBeam.transform.position.y, bossLeft.transform.position.y);
        }

        [Test]
        public void Chapter01_CameraObstacleGauntlet_KeepsNarrowHallCenterClearBetweenPillars()
        {
            AssertSceneOpen();

            Assert.IsNotNull(FindSceneObject("Zone03_CameraPillar_Left_A"));
            Assert.IsNotNull(FindSceneObject("Zone03_CameraPillar_Right_A"));
            Assert.IsNotNull(FindSceneObject("Zone03_CameraPillar_Left_B"));
            Assert.IsNotNull(FindSceneObject("Zone03_CameraPillar_Right_B"));
            Physics.SyncTransforms();

            Vector3 focus = new Vector3(0f, 1.5f, 55.5f);
            Vector3 desired = new Vector3(0f, 1.5f, 50.8f);
            CameraObstacleResolution resolution = CameraObstacleResolver.Resolve(
                focus,
                desired,
                desired,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.IsFalse(resolution.HasStaticObstruction);
            Assert.IsFalse(resolution.UsedNarrowObstacleSidestep);
            Assert.That(resolution.RetractionRatio, Is.GreaterThan(0.95f));
            Assert.That(resolution.Position.x, Is.EqualTo(desired.x).Within(0.05f));
            Assert.That(resolution.Position.z, Is.EqualTo(desired.z).Within(0.05f));
        }

        [Test]
        public void Chapter01_CameraObstacleGauntlet_RetractsAgainstInteriorBackWallWithoutSidestep()
        {
            AssertSceneOpen();

            GameObject backWall = FindSceneObjectUnder(Chapter01Ids.Areas.Interior, "Wall_Back_Left");
            Assert.IsNotNull(backWall);
            Collider backWallCollider = backWall.GetComponent<Collider>();
            Assert.IsNotNull(backWallCollider);
            Physics.SyncTransforms();

            Vector3 focus = new Vector3(-5.8f, 1.5f, 48.8f);
            Vector3 desired = new Vector3(-5.8f, 1.5f, 44.2f);
            CameraObstacleResolution resolution = CameraObstacleResolver.Resolve(
                focus,
                desired,
                desired,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.IsTrue(resolution.HasStaticObstruction);
            Assert.IsFalse(resolution.UsedNarrowObstacleSidestep);
            Assert.Less(resolution.RetractionRatio, 0.8f);
            Assert.Greater(resolution.Position.z, backWallCollider.bounds.max.z);
            AssertPointOutsideCollider(backWallCollider, resolution.Position);
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

        [Test]
        public void SceneFile_ContainsBakedNavMeshData()
        {
            string sceneYaml = File.ReadAllText(ScenePath);
            StringAssert.DoesNotContain("m_NavMeshData: {fileID: 0}", sceneYaml);
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

        private static GameObject FindSceneObjectUnder(string parentName, string objectName)
        {
            GameObject parent = FindSceneObject(parentName);

            if (parent == null)
            {
                return null;
            }

            Transform[] transforms = parent.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                {
                    return transforms[i].gameObject;
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

        private static ChapterMapDefinitionSO LoadMapDefinition()
        {
            ChapterMapDefinitionSO mapDefinition = AssetDatabase.LoadAssetAtPath<ChapterMapDefinitionSO>(MapDefinitionPath);
            Assert.IsNotNull(mapDefinition, MapDefinitionPath);
            return mapDefinition;
        }

        private static void AssertMapZone(
            ChapterMapDefinitionSO mapDefinition,
            string zoneId,
            string sceneObjectName,
            string areaId,
            string primaryEncounterId,
            string checkpointId,
            string rewardKeyItemId,
            bool optionalRoute)
        {
            Assert.IsTrue(mapDefinition.TryGetZone(zoneId, out ChapterMapDefinitionSO.MapZoneDefinition zone), zoneId);
            Assert.AreEqual(sceneObjectName, zone.SceneObjectName);
            Assert.AreEqual(areaId, zone.AreaId);
            Assert.AreEqual(primaryEncounterId, zone.PrimaryEncounterId);
            Assert.AreEqual(checkpointId, zone.CheckpointId);
            Assert.AreEqual(rewardKeyItemId, zone.RewardKeyItemId);
            Assert.AreEqual(optionalRoute, zone.OptionalRoute);
            Assert.IsFalse(string.IsNullOrWhiteSpace(zone.DisplayName), zoneId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(zone.ObjectiveHint), zoneId);
        }

        private static void AssertRouteGate(
            ChapterMapDefinitionSO mapDefinition,
            string gateId,
            string fromZoneId,
            string toZoneId,
            string requiredEncounterId,
            string requiredKeyItemId,
            bool opensShortcut)
        {
            Assert.IsTrue(mapDefinition.TryGetRouteGate(gateId, out ChapterMapDefinitionSO.RouteGateDefinition routeGate), gateId);
            Assert.AreEqual(fromZoneId, routeGate.FromZoneId);
            Assert.AreEqual(toZoneId, routeGate.ToZoneId);
            Assert.AreEqual(requiredEncounterId, routeGate.RequiredEncounterId);
            Assert.AreEqual(requiredKeyItemId, routeGate.RequiredKeyItemId);
            Assert.AreEqual(opensShortcut, routeGate.OpensShortcut);
            Assert.IsFalse(string.IsNullOrWhiteSpace(routeGate.DisplayName), gateId);
        }

        private static void AssertRouteGateMatchesDoor(
            ChapterMapDefinitionSO mapDefinition,
            string gateId,
            string doorName)
        {
            Assert.IsTrue(mapDefinition.TryGetRouteGate(gateId, out ChapterMapDefinitionSO.RouteGateDefinition routeGate), gateId);

            DoorController doorController = FindRequiredComponent<DoorController>(doorName);
            Assert.AreEqual(routeGate.RequiredEncounterId, GetPrivateField<string>(doorController, "requiredEncounterId"), gateId);
            Assert.AreEqual(routeGate.RequiredKeyItemId, GetPrivateField<string>(doorController, "requiredKeyItemId"), gateId);
        }

        private static void AssertNavigationFloor(GameObject gameObject, string objectName)
        {
            Assert.IsNotNull(gameObject, objectName);
            Assert.IsNotNull(gameObject.GetComponent<BoxCollider>(), objectName);
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            Assert.IsTrue((flags & StaticEditorFlags.NavigationStatic) != 0, objectName);
        }

        private static GameObject AssertModularBlock(string objectName)
        {
            GameObject gameObject = FindSceneObject(objectName);
            Assert.IsNotNull(gameObject, objectName);
            Assert.IsNotNull(gameObject.GetComponent<MeshFilter>(), objectName);
            Assert.IsNotNull(gameObject.GetComponent<MeshRenderer>(), objectName);
            Assert.IsNotNull(gameObject.GetComponent<BoxCollider>(), objectName);
            Assert.IsNull(PrefabUtility.GetCorrespondingObjectFromSource(gameObject), objectName);
            Assert.Greater(gameObject.transform.localScale.x, 0f, objectName);
            Assert.Greater(gameObject.transform.localScale.y, 0f, objectName);
            Assert.Greater(gameObject.transform.localScale.z, 0f, objectName);
            return gameObject;
        }

        private static void AssertPointOutsideCollider(Collider collider, Vector3 point)
        {
            Assert.IsNotNull(collider);
            Assert.Greater(Vector3.Distance(collider.ClosestPoint(point), point), 0.0001f);
        }

        private static bool IsImportedSourcePath(string path)
        {
            return ImportedSourceRoots.Any(root => path.StartsWith(root));
        }
    }
}
