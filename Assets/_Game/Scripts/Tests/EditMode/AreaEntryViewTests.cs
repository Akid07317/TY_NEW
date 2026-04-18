using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class AreaEntryViewTests
    {
        [Test]
        public void AreaEntryPlanner_BuildsReadableChapter01ArrivalMessages()
        {
            ChapterProgressionSO progression = CreateProgression();

            try
            {
                AreaEntryPlan entrancePlan = AreaEntryPlanner.Build(progression, Chapter01Ids.Areas.Entrance);
                Assert.IsTrue(entrancePlan.IsVisible);
                Assert.AreEqual("Entrance Tutorial", entrancePlan.Title);
                Assert.AreEqual("Warm up here. Learn the controls, clear the drill, and lock in CP01.", entrancePlan.Body);

                AreaEntryPlan courtyardPlan = AreaEntryPlanner.Build(progression, Chapter01Ids.Areas.Courtyard);
                Assert.AreEqual("Outdoor Courtyard", courtyardPlan.Title);
                Assert.AreEqual("Mixed enemies ahead. Clear the courtyard to reach the school interior.", courtyardPlan.Body);

                AreaEntryPlan bossPlan = AreaEntryPlanner.Build(progression, Chapter01Ids.Areas.Boss);
                Assert.AreEqual("Boss Arena", bossPlan.Title);
                Assert.AreEqual("Final exam ahead. Use the checkpoint, read the gatekeeper, and secure the Ritual Core.", bossPlan.Body);
            }
            finally
            {
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void AreaEntryView_ShowsCurrentAreaAndTracksAreaTransitions()
        {
            ChapterProgressionSO progression = CreateProgression();
            GameObject progressObject = new GameObject("ChapterFlow");
            GameObject viewObject = new GameObject("AreaEntryView");

            try
            {
                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokeMethod(progressService, "Awake");

                AreaEntryView view = viewObject.AddComponent<AreaEntryView>();
                SetPrivateField(view, "chapterProgressService", progressService);
                SetPrivateField(view, "visibleDurationSeconds", 0.4f);
                InvokeMethod(view, "Awake");
                InvokeMethod(view, "OnEnable");
                InvokeMethod(view, "Update");

                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Entrance Tutorial", view.CurrentTitle);
                Assert.AreEqual("Warm up here. Learn the controls, clear the drill, and lock in CP01.", view.CurrentBody);

                progressService.EnterArea(Chapter01Ids.Areas.Courtyard);
                Assert.AreEqual("Outdoor Courtyard", view.CurrentTitle);
                Assert.AreEqual("Mixed enemies ahead. Clear the courtyard to reach the school interior.", view.CurrentBody);

                progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil);
                Assert.AreEqual("Outdoor Courtyard", view.CurrentTitle);

                progressService.EnterArea(Chapter01Ids.Areas.Boss);
                Assert.AreEqual("Boss Arena", view.CurrentTitle);
                Assert.AreEqual("Final exam ahead. Use the checkpoint, read the gatekeeper, and secure the Ritual Core.", view.CurrentBody);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void AreaEntryView_SuppressesInitialBannerWhenResumeSaveAlreadyExists()
        {
            string fileName = "area_entry_resume_" + System.Guid.NewGuid().ToString("N") + ".json";
            ChapterProgressionSO progression = CreateProgression();
            GameObject progressObject = new GameObject("ChapterFlow");
            GameObject saveObject = new GameObject("SaveService");
            GameObject viewObject = new GameObject("AreaEntryView");

            try
            {
                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokeMethod(progressService, "Awake");
                progressService.EnterArea(Chapter01Ids.Areas.Courtyard);

                SaveService saveService = saveObject.AddComponent<SaveService>();
                SetPrivateField(saveService, "fileName", fileName);
                saveService.Save(new ChapterSaveData
                {
                    chapterId = Chapter01Ids.Chapter,
                    checkpointId = Chapter01Ids.Checkpoints.Courtyard,
                    currentAreaId = Chapter01Ids.Areas.Courtyard,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance, Chapter01Ids.Areas.Courtyard }
                });

                AreaEntryView view = viewObject.AddComponent<AreaEntryView>();
                SetPrivateField(view, "chapterProgressService", progressService);
                SetPrivateField(view, "saveService", saveService);
                SetPrivateField(view, "visibleDurationSeconds", 0.4f);
                InvokeMethod(view, "Awake");
                InvokeMethod(view, "OnEnable");
                InvokeMethod(view, "Update");

                Assert.IsFalse(view.IsVisible);
                Assert.AreEqual(string.Empty, view.CurrentTitle);
                Assert.AreEqual(string.Empty, view.CurrentBody);

                progressService.EnterArea(Chapter01Ids.Areas.Boss);
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Boss Arena", view.CurrentTitle);
                Assert.AreEqual("Final exam ahead. Use the checkpoint, read the gatekeeper, and secure the Ritual Core.", view.CurrentBody);
            }
            finally
            {
                CleanupSaveFile(fileName);
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(saveObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        private static ChapterProgressionSO CreateProgression()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            SetPrivateField(
                progression,
                "areas",
                new[]
                {
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance Tutorial"),
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Courtyard, "Outdoor Courtyard"),
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Interior, "School Interior"),
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Boss, "Boss Arena")
                });
            return progression;
        }

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, arguments);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void CleanupSaveFile(string fileName)
        {
            string fullPath = System.IO.Path.Combine(Application.persistentDataPath, "Save", fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
