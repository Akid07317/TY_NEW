using System.Reflection;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusRPG.Tests.EditMode
{
    public sealed class Chapter01ProgressionSceneFlowTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Chapter01_Combined.unity";
        private const string InteriorEncounterId = "EN_A03_INTERIOR";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void Chapter01_BossClosureFlow_UnlocksFinalDoorsAndChapterCompleteView()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            DoorController bossGateDoor = FindRequiredComponent<DoorController>("Door_A03_To_A04");
            DoorController ritualCoreDoor = FindRequiredComponent<DoorController>("Door_A04_To_RitualCore");
            ChapterCompleteView completeView = FindRequiredComponent<ChapterCompleteView>("ChapterCompleteView");
            BossArenaStatusPresenter arenaStatusPresenter = FindRequiredComponent<BossArenaStatusPresenter>("BossPresentationRig");
            ChapterObjectiveView objectiveView = FindRequiredComponent<ChapterObjectiveView>("ChapterFlow");
            KeyItemAcquisitionView keyItemAcquisitionView = FindRequiredComponent<KeyItemAcquisitionView>("ChapterFlow");
            KeyItemBeaconView ritualCoreBeacon = FindRequiredComponent<KeyItemBeaconView>("Pickup_RitualCore");

            InitializeSceneFlow(progressService, bossGateDoor, ritualCoreDoor, completeView, objectiveView, keyItemAcquisitionView, ritualCoreBeacon);

            Assert.IsTrue(bossGateDoor.gameObject.activeSelf);
            Assert.IsTrue(ritualCoreDoor.gameObject.activeSelf);
            Assert.IsFalse(completeView.IsVisible);
            InvokeMethod(ritualCoreBeacon, "Tick", 0f);
            Assert.IsFalse(ritualCoreBeacon.IsVisible);

            Assert.IsTrue(progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil));
            Assert.IsFalse(bossGateDoor.gameObject.activeSelf);
            Assert.IsTrue(ritualCoreDoor.gameObject.activeSelf);
            Assert.IsFalse(completeView.IsVisible);
            Assert.IsTrue(keyItemAcquisitionView.IsVisible);
            Assert.AreEqual("Gate Sigil Recovered", keyItemAcquisitionView.CurrentTitle);

            Assert.IsTrue(progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Gatekeeper));
            Assert.IsFalse(ritualCoreDoor.gameObject.activeSelf);
            Assert.IsFalse(completeView.IsVisible);
            Assert.AreEqual("Ritual Core Ahead", objectiveView.CurrentHeading);
            Assert.AreEqual("The gatekeeper is down. Walk forward and pick up the Ritual Core to finish the chapter.", objectiveView.CurrentBody);
            Assert.IsFalse(objectiveView.IsHighlightActive);
            InvokeMethod(ritualCoreBeacon, "Tick", 0.1f);
            Assert.IsTrue(ritualCoreBeacon.IsVisible);
            Assert.IsTrue(ritualCoreBeacon.IsRevealPulseVisible);

            InvokeMethod(ritualCoreBeacon, "Tick", 1.2f);
            Assert.IsTrue(ritualCoreBeacon.IsVisible);
            Assert.IsFalse(ritualCoreBeacon.IsRevealPulseVisible);

            InvokeMethod(
                arenaStatusPresenter,
                "ShowMessage",
                "Gatekeeper Down",
                "Walk forward and pick up the Ritual Core to finish the chapter.",
                1.05f);
            Assert.IsTrue(arenaStatusPresenter.IsVisible);
            Assert.AreEqual("Gatekeeper Down", arenaStatusPresenter.CurrentTitle);

            Assert.IsTrue(progressService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore));
            Assert.IsTrue(progressService.HasKeyItem(Chapter01Ids.KeyItems.RitualCore));
            Assert.IsFalse(completeView.IsVisible);
            InvokeMethod(arenaStatusPresenter, "Tick", 0f);
            Assert.IsFalse(arenaStatusPresenter.IsVisible);
            InvokeMethod(completeView, "Tick", 0.4f);
            Assert.IsTrue(completeView.IsVisible);
            Assert.IsTrue(completeView.IsFadeInActive);
            Assert.Greater(completeView.CurrentRevealAlpha, 0f);
            Assert.Less(completeView.CurrentRevealAlpha, 1f);
            Assert.Greater(completeView.CurrentBackdropAlpha, 0f);
            Assert.Less(completeView.CurrentBackdropAlpha, 0.28f);
            InvokeMethod(completeView, "Tick", 0.3f);
            Assert.IsFalse(completeView.IsFadeInActive);
            Assert.AreEqual(1f, completeView.CurrentRevealAlpha, 0.001f);
            Assert.AreEqual(0.28f, completeView.CurrentBackdropAlpha, 0.001f);
            Assert.IsFalse(keyItemAcquisitionView.IsVisible);
            InvokeMethod(ritualCoreBeacon, "Tick", 0f);
            Assert.IsFalse(ritualCoreBeacon.IsVisible);
            Assert.IsFalse(ritualCoreBeacon.IsRevealPulseVisible);
            Assert.AreEqual("Chapter 01 Cleared", completeView.CurrentTitle);
            Assert.AreEqual("Reward: Ritual Core recovered.", completeView.CurrentRewardLine);
            Assert.AreEqual("Save state: Chapter01 auto-save updated.", completeView.CurrentSaveStateLine);
        }

        [Test]
        public void Chapter01_MainRouteFlow_UnlocksDoorsInChapterOrder()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            DoorController entranceDoor = FindRequiredComponent<DoorController>("Door_A01_To_A02");
            DoorController courtyardDoor = FindRequiredComponent<DoorController>("Door_A02_To_A03");
            DoorController bossGateDoor = FindRequiredComponent<DoorController>("Door_A03_To_A04");
            DoorController ritualCoreDoor = FindRequiredComponent<DoorController>("Door_A04_To_RitualCore");
            ChapterCompleteView completeView = FindRequiredComponent<ChapterCompleteView>("ChapterCompleteView");

            InitializeSceneFlow(progressService, entranceDoor, courtyardDoor, bossGateDoor, ritualCoreDoor, completeView);

            Assert.AreEqual(Chapter01Ids.Areas.Entrance, progressService.CurrentAreaId);
            Assert.IsTrue(entranceDoor.gameObject.activeSelf);
            Assert.IsTrue(courtyardDoor.gameObject.activeSelf);
            Assert.IsTrue(bossGateDoor.gameObject.activeSelf);
            Assert.IsTrue(ritualCoreDoor.gameObject.activeSelf);
            Assert.IsFalse(completeView.IsVisible);

            Assert.IsTrue(progressService.MarkEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial));
            Assert.IsFalse(entranceDoor.gameObject.activeSelf);
            Assert.IsTrue(courtyardDoor.gameObject.activeSelf);
            Assert.IsTrue(bossGateDoor.gameObject.activeSelf);

            Assert.IsTrue(progressService.EnterArea(Chapter01Ids.Areas.Courtyard));
            Assert.IsTrue(progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Courtyard));
            Assert.IsFalse(courtyardDoor.gameObject.activeSelf);
            Assert.IsTrue(bossGateDoor.gameObject.activeSelf);

            Assert.IsTrue(progressService.EnterArea(Chapter01Ids.Areas.Interior));
            Assert.IsTrue(progressService.MarkEncounterCleared(InteriorEncounterId));
            Assert.IsTrue(bossGateDoor.gameObject.activeSelf);
            Assert.IsTrue(ritualCoreDoor.gameObject.activeSelf);
            Assert.IsTrue(progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil));
            Assert.IsFalse(bossGateDoor.gameObject.activeSelf);
            Assert.IsTrue(ritualCoreDoor.gameObject.activeSelf);

            Assert.IsTrue(progressService.EnterArea(Chapter01Ids.Areas.Boss));
            Assert.IsTrue(progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Gatekeeper));
            Assert.IsFalse(ritualCoreDoor.gameObject.activeSelf);
            Assert.IsFalse(completeView.IsVisible);

            Assert.IsTrue(progressService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore));
            InvokeMethod(completeView, "Tick", 0.4f);
            Assert.IsTrue(completeView.IsVisible);
            Assert.IsTrue(completeView.IsFadeInActive);
            Assert.Greater(completeView.CurrentBackdropAlpha, 0f);
            Assert.Less(completeView.CurrentBackdropAlpha, 0.28f);
            InvokeMethod(completeView, "Tick", 0.3f);
            Assert.AreEqual(1f, completeView.CurrentRevealAlpha, 0.001f);
            Assert.AreEqual(0.28f, completeView.CurrentBackdropAlpha, 0.001f);
            Assert.AreEqual("Chapter 01 Cleared", completeView.CurrentTitle);
        }

        [Test]
        public void Chapter01_InteriorEncounter_ClosesAndReopensSigilRoomBarriers()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            EncounterController interiorEncounter = FindRequiredComponent<EncounterController>("Encounter_EN_A03_INTERIOR");
            GameObject entryBarrier = FindSceneObject("InteriorEncounterBarrier_Entry");
            GameObject sigilBarrier = FindSceneObject("InteriorEncounterBarrier_Sigil");

            Assert.IsNotNull(entryBarrier);
            Assert.IsNotNull(sigilBarrier);

            InitializeSceneFlow(progressService, interiorEncounter);

            Assert.IsFalse(entryBarrier.activeSelf);
            Assert.IsFalse(sigilBarrier.activeSelf);

            interiorEncounter.ActivateEncounter();
            Assert.IsTrue(entryBarrier.activeSelf);
            Assert.IsTrue(sigilBarrier.activeSelf);

            Assert.IsTrue(progressService.MarkEncounterCleared(InteriorEncounterId));
            Assert.IsFalse(entryBarrier.activeSelf);
            Assert.IsFalse(sigilBarrier.activeSelf);
        }

        [Test]
        public void Chapter01_RestoreFlow_RehydratesDoorsForBossApproachState()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("ChapterFlow");
            DoorController entranceDoor = FindRequiredComponent<DoorController>("Door_A01_To_A02");
            DoorController courtyardDoor = FindRequiredComponent<DoorController>("Door_A02_To_A03");
            DoorController bossGateDoor = FindRequiredComponent<DoorController>("Door_A03_To_A04");
            DoorController ritualCoreDoor = FindRequiredComponent<DoorController>("Door_A04_To_RitualCore");
            ChapterCompleteView completeView = FindRequiredComponent<ChapterCompleteView>("ChapterCompleteView");
            EncounterController interiorEncounter = FindRequiredComponent<EncounterController>("Encounter_EN_A03_INTERIOR");
            GameObject entryBarrier = FindSceneObject("InteriorEncounterBarrier_Entry");
            GameObject sigilBarrier = FindSceneObject("InteriorEncounterBarrier_Sigil");

            Assert.IsNotNull(entryBarrier);
            Assert.IsNotNull(sigilBarrier);

            InitializeSceneFlow(
                progressService,
                entranceDoor,
                courtyardDoor,
                bossGateDoor,
                ritualCoreDoor,
                completeView,
                interiorEncounter);

            progressService.RestoreFromSave(new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                currentAreaId = Chapter01Ids.Areas.Boss,
                visitedAreaIds = new[]
                {
                    Chapter01Ids.Areas.Entrance,
                    Chapter01Ids.Areas.Courtyard,
                    Chapter01Ids.Areas.Interior,
                    Chapter01Ids.Areas.Boss
                },
                clearedEncounterIds = new[]
                {
                    Chapter01Ids.Encounters.EntranceTutorial,
                    Chapter01Ids.Encounters.Courtyard,
                    InteriorEncounterId
                },
                keyItemIds = new[]
                {
                    Chapter01Ids.KeyItems.GateSigil
                },
                chapterCompleted = false
            });

            Assert.AreEqual(Chapter01Ids.Areas.Boss, progressService.CurrentAreaId);
            Assert.IsFalse(entranceDoor.gameObject.activeSelf);
            Assert.IsFalse(courtyardDoor.gameObject.activeSelf);
            Assert.IsFalse(bossGateDoor.gameObject.activeSelf);
            Assert.IsTrue(ritualCoreDoor.gameObject.activeSelf);
            Assert.IsFalse(entryBarrier.activeSelf);
            Assert.IsFalse(sigilBarrier.activeSelf);
            Assert.IsFalse(completeView.IsVisible);
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

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            }

            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, arguments);
        }

        private static void InitializeSceneFlow(ChapterProgressService progressService, params Component[] components)
        {
            InvokeMethod(progressService, "Awake");
            InvokeMethod(progressService, "OnEnable");

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    continue;
                }

                InvokeMethod(components[i], "Awake");
                InvokeMethod(components[i], "OnEnable");
            }
        }
    }
}
