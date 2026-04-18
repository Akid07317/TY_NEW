using System.Reflection;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class ChapterProgressServiceTests
    {
        [Test]
        public void ChapterProgressService_RestoreAndPopulateSaveData_StayConsistent()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject sourceObject = new GameObject("ChapterProgressSource");
            GameObject restoreObject = new GameObject("ChapterProgressRestore");

            try
            {
                SetPrivateField(progression, "chapterId", Chapter01Ids.Chapter);
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance"),
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Courtyard, "Courtyard"),
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Interior, "Interior")
                    });

                ChapterProgressService sourceService = sourceObject.AddComponent<ChapterProgressService>();
                SetPrivateField(sourceService, "progression", progression);
                InvokePrivateMethod(sourceService, "Awake");

                Assert.AreEqual(Chapter01Ids.Areas.Entrance, sourceService.CurrentAreaId);

                sourceService.EnterArea(Chapter01Ids.Areas.Courtyard);
                sourceService.MarkEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial);
                sourceService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil);
                sourceService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore);

                ChapterSaveData saveData = new ChapterSaveData();
                sourceService.PopulateSaveData(saveData);

                Assert.AreEqual(Chapter01Ids.Chapter, saveData.chapterId);
                Assert.AreEqual(Chapter01Ids.Areas.Courtyard, saveData.currentAreaId);
                Assert.AreEqual(2, saveData.visitedAreaIds.Length);
                Assert.AreEqual(2, saveData.keyItemIds.Length);
                Assert.AreEqual(1, saveData.clearedEncounterIds.Length);
                CollectionAssert.AreEqual(
                    new[] { Chapter01Ids.Areas.Entrance, Chapter01Ids.Areas.Courtyard },
                    saveData.visitedAreaIds);
                CollectionAssert.AreEqual(
                    new[] { Chapter01Ids.KeyItems.GateSigil, Chapter01Ids.KeyItems.RitualCore },
                    saveData.keyItemIds);
                CollectionAssert.AreEqual(
                    new[] { Chapter01Ids.Encounters.EntranceTutorial },
                    saveData.clearedEncounterIds);
                Assert.IsTrue(saveData.chapterCompleted);

                ChapterProgressService restoredService = restoreObject.AddComponent<ChapterProgressService>();
                SetPrivateField(restoredService, "progression", progression);
                InvokePrivateMethod(restoredService, "Awake");
                restoredService.RestoreFromSave(saveData);

                Assert.AreEqual(Chapter01Ids.Areas.Courtyard, restoredService.CurrentAreaId);
                Assert.IsTrue(restoredService.HasVisitedArea(Chapter01Ids.Areas.Entrance));
                Assert.IsTrue(restoredService.HasVisitedArea(Chapter01Ids.Areas.Courtyard));
                Assert.IsTrue(restoredService.IsEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial));
                Assert.IsTrue(restoredService.HasKeyItem(Chapter01Ids.KeyItems.GateSigil));
                Assert.IsTrue(restoredService.HasKeyItem(Chapter01Ids.KeyItems.RitualCore));
                Assert.IsTrue(restoredService.IsChapterCompleted);
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(restoreObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void ChapterProgressService_RestoreFromSave_NormalizesInvalidCurrentArea()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject restoreObject = new GameObject("ChapterProgressRestore");

            try
            {
                SetPrivateField(progression, "chapterId", Chapter01Ids.Chapter);
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance"),
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Courtyard, "Courtyard")
                    });

                ChapterProgressService restoredService = restoreObject.AddComponent<ChapterProgressService>();
                SetPrivateField(restoredService, "progression", progression);
                InvokePrivateMethod(restoredService, "Awake");
                restoredService.RestoreFromSave(new ChapterSaveData
                {
                    currentAreaId = "BrokenArea",
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Courtyard }
                });

                Assert.AreEqual(Chapter01Ids.Areas.Entrance, restoredService.CurrentAreaId);
                Assert.IsTrue(restoredService.HasVisitedArea(Chapter01Ids.Areas.Entrance));
                Assert.IsTrue(restoredService.HasVisitedArea(Chapter01Ids.Areas.Courtyard));
            }
            finally
            {
                Object.DestroyImmediate(restoreObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void ChapterProgressService_RestoreFromSave_AddsCurrentAreaToVisitedAreas()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject restoreObject = new GameObject("ChapterProgressRestore");

            try
            {
                SetPrivateField(progression, "chapterId", Chapter01Ids.Chapter);
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance"),
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Courtyard, "Courtyard")
                    });

                ChapterProgressService restoredService = restoreObject.AddComponent<ChapterProgressService>();
                SetPrivateField(restoredService, "progression", progression);
                InvokePrivateMethod(restoredService, "Awake");
                restoredService.RestoreFromSave(new ChapterSaveData
                {
                    currentAreaId = Chapter01Ids.Areas.Courtyard,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance }
                });

                Assert.AreEqual(Chapter01Ids.Areas.Courtyard, restoredService.CurrentAreaId);
                Assert.IsTrue(restoredService.HasVisitedArea(Chapter01Ids.Areas.Entrance));
                Assert.IsTrue(restoredService.HasVisitedArea(Chapter01Ids.Areas.Courtyard));
            }
            finally
            {
                Object.DestroyImmediate(restoreObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void ChapterProgressService_Mutations_StayIdempotentAndMeetRequirements()
        {
            GameObject serviceObject = new GameObject("ChapterProgressService");

            try
            {
                ChapterProgressService service = serviceObject.AddComponent<ChapterProgressService>();
                InvokePrivateMethod(service, "Awake");

                Assert.IsTrue(service.EnterArea("BossHall"));
                Assert.IsFalse(service.EnterArea("BossHall"));
                Assert.IsTrue(service.RegisterKeyItem("GateSigil"));
                Assert.IsFalse(service.RegisterKeyItem("GateSigil"));
                Assert.IsTrue(service.MarkEncounterCleared("BossGuard"));
                Assert.IsFalse(service.MarkEncounterCleared("BossGuard"));
                Assert.IsTrue(service.CompleteChapter("RitualCore"));
                Assert.IsFalse(service.CompleteChapter("RitualCore"));

                Assert.IsTrue(service.HasVisitedArea("BossHall"));
                Assert.IsTrue(service.HasKeyItem("GateSigil"));
                Assert.IsTrue(service.HasKeyItem("RitualCore"));
                Assert.IsTrue(service.IsEncounterCleared("BossGuard"));
                Assert.IsTrue(service.IsChapterCompleted);
                Assert.IsTrue(service.MeetsRequirements("BossHall", "BossGuard", "GateSigil"));
                Assert.IsFalse(service.MeetsRequirements("MissingArea", "BossGuard", "GateSigil"));
            }
            finally
            {
                Object.DestroyImmediate(serviceObject);
            }
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
