using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusRPG.Tests.EditMode
{
    public sealed class MainMenuViewTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";

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
        public void MainMenuPlanner_WithoutSave_ShowsFreshStart()
        {
            MainMenuPlan plan = MainMenuPlanner.Build(null);

            Assert.AreEqual("No auto-save found. Start from CP01 / Entrance Tutorial.", plan.StatusLine);
            Assert.AreEqual("Start Fresh", plan.ObjectiveHeading);
            Assert.AreEqual("Finish the tutorial encounter, activate CP01, then push deeper into the sealed campus.", plan.ObjectiveBody);
            Assert.AreEqual("Start Chapter 01", plan.PrimaryActionLabel);
            Assert.IsFalse(plan.ShowSecondaryAction);
            Assert.AreEqual(string.Empty, plan.SecondaryActionLabel);
            Assert.AreEqual("Enter: Start Chapter 01   Esc: Quit", plan.ShortcutHintLine);
        }

        [Test]
        public void MainMenuPlanner_WithCheckpointSave_ShowsContinueAndRestart()
        {
            ChapterSaveData saveData = new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                checkpointId = Chapter01Ids.Checkpoints.Courtyard,
                currentAreaId = Chapter01Ids.Areas.Courtyard
            };

            MainMenuPlan plan = MainMenuPlanner.Build(saveData);

            Assert.AreEqual("Latest auto-save: CP02 / Outdoor Courtyard. Continue from the saved checkpoint, or restart from CP01.", plan.StatusLine);
            Assert.AreEqual("Outdoor Courtyard", plan.ObjectiveHeading);
            Assert.AreEqual("Win the courtyard skirmish, then push into the school interior.", plan.ObjectiveBody);
            Assert.AreEqual("Continue Chapter 01", plan.PrimaryActionLabel);
            Assert.IsTrue(plan.ShowSecondaryAction);
            Assert.AreEqual("Restart Chapter 01", plan.SecondaryActionLabel);
            Assert.AreEqual("Enter: Continue Chapter 01   R: Restart Chapter 01   Esc: Quit", plan.ShortcutHintLine);
        }

        [Test]
        public void MainMenuPlanner_WithGateSigilSave_ShowsBossObjectivePreview()
        {
            ChapterSaveData saveData = new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                checkpointId = Chapter01Ids.Checkpoints.Interior,
                currentAreaId = Chapter01Ids.Areas.Interior,
                keyItemIds = new[] { Chapter01Ids.KeyItems.GateSigil }
            };

            MainMenuPlan plan = MainMenuPlanner.Build(saveData);

            Assert.AreEqual("Boss Gate Open", plan.ObjectiveHeading);
            Assert.AreEqual("The Gate Sigil unlocked the boss route. Push forward and challenge the gatekeeper.", plan.ObjectiveBody);
        }

        [Test]
        public void MainMenuPlanner_WithCompletedSave_ShowsReviewAndRestart()
        {
            ChapterSaveData saveData = new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                chapterCompleted = true,
                checkpointId = Chapter01Ids.Checkpoints.Interior,
                currentAreaId = Chapter01Ids.Areas.Boss
            };

            MainMenuPlan plan = MainMenuPlanner.Build(saveData);

            Assert.AreEqual("Latest auto-save: Chapter complete. Load back in to review the ending card, or restart from CP01.", plan.StatusLine);
            Assert.AreEqual("Chapter Complete", plan.ObjectiveHeading);
            Assert.AreEqual("The Ritual Core is already secured. Load back in to review the ending card, or restart from CP01.", plan.ObjectiveBody);
            Assert.AreEqual("Review Chapter Complete", plan.PrimaryActionLabel);
            Assert.IsTrue(plan.ShowSecondaryAction);
            Assert.AreEqual("Restart Chapter 01", plan.SecondaryActionLabel);
            Assert.AreEqual("Enter: Review Chapter Complete   R: Restart Chapter 01   Esc: Quit", plan.ShortcutHintLine);
        }

        [Test]
        public void MainMenuScene_WiresViewToSaveServiceAndChapterScene()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            GameObject cameraObject = GameObject.Find("Main Camera");
            Assert.IsNotNull(cameraObject);

            SaveService saveService = cameraObject.GetComponent<SaveService>();
            MainMenuView view = cameraObject.GetComponent<MainMenuView>();

            Assert.IsNotNull(saveService);
            Assert.IsNotNull(view);
            Assert.AreSame(saveService, GetPrivateField<SaveService>(view, "saveService"));
            Assert.AreEqual("Chapter01_Combined", GetPrivateField<string>(view, "chapterSceneName"));
            Assert.AreEqual("Campus Chapter 01", GetPrivateField<string>(view, "menuTitle"));
        }

        private static TValue GetPrivateField<TValue>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TValue)field.GetValue(instance);
        }
    }
}
