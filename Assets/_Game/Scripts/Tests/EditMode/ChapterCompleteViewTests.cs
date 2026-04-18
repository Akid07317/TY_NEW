using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class ChapterCompleteViewTests
    {
        [Test]
        public void ChapterCompleteView_TracksChapterCompletionAndExposeReadableSummary()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject viewObject = new GameObject("ChapterCompleteView");

            try
            {
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance")
                    });

                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                ChapterCompleteView view = viewObject.AddComponent<ChapterCompleteView>();
                SetPrivateField(view, "chapterProgressService", progressService);
                SetPrivateField(view, "completionRevealDelaySeconds", 0.35f);
                SetPrivateField(view, "completionFadeInDurationSeconds", 0.18f);
                SetPrivateField(view, "backdropMaxAlpha", 0.28f);
                InvokePrivateMethod(view, "Awake");
                InvokePrivateMethod(view, "OnEnable");

                Assert.IsFalse(view.IsVisible);
                Assert.AreEqual(string.Empty, view.CurrentTitle);
                Assert.AreEqual(0f, view.CurrentBackdropAlpha, 0.001f);

                progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Gatekeeper);
                progressService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore);
                Assert.IsFalse(view.IsVisible);
                Assert.IsTrue(view.IsRevealPending);
                Assert.AreEqual(0.35f, view.RemainingRevealDelaySeconds, 0.001f);
                Assert.IsFalse(view.IsFadeInActive);
                Assert.AreEqual(0f, view.CurrentRevealAlpha, 0.001f);
                Assert.AreEqual(0f, view.CurrentBackdropAlpha, 0.001f);

                InvokePrivateMethod(view, "Tick", 0.4f);
                Assert.IsTrue(view.IsVisible);
                Assert.IsFalse(view.IsRevealPending);
                Assert.IsTrue(view.IsFadeInActive);
                Assert.Greater(view.CurrentRevealAlpha, 0f);
                Assert.Less(view.CurrentRevealAlpha, 1f);
                Assert.Greater(view.CurrentBackdropAlpha, 0f);
                Assert.Less(view.CurrentBackdropAlpha, 0.28f);

                InvokePrivateMethod(view, "Tick", 0.4f);
                Assert.IsFalse(view.IsFadeInActive);
                Assert.AreEqual(1f, view.CurrentRevealAlpha, 0.001f);
                Assert.AreEqual(0.28f, view.CurrentBackdropAlpha, 0.001f);
                Assert.AreEqual("Chapter 01 Cleared", view.CurrentTitle);
                Assert.AreEqual("The gatekeeper is down and the Ritual Core is secure.", view.CurrentBody);
                Assert.AreEqual("Result: Campus Gatekeeper defeated.", view.CurrentResultLine);
                Assert.AreEqual("Reward: Ritual Core recovered.", view.CurrentRewardLine);
                Assert.AreEqual("Save state: Chapter01 auto-save updated.", view.CurrentSaveStateLine);

                progressService.RestoreFromSave(new ChapterSaveData());
                Assert.IsFalse(view.IsVisible);
                Assert.AreEqual(string.Empty, view.CurrentTitle);
                Assert.AreEqual(0f, view.CurrentRevealAlpha, 0.001f);
                Assert.AreEqual(0f, view.CurrentBackdropAlpha, 0.001f);

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    chapterId = Chapter01Ids.Chapter,
                    currentAreaId = Chapter01Ids.Areas.Entrance,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance },
                    clearedEncounterIds = new[] { Chapter01Ids.Encounters.Gatekeeper },
                    keyItemIds = new[] { Chapter01Ids.KeyItems.RitualCore },
                    chapterCompleted = true
                });
                Assert.IsTrue(view.IsVisible);
                Assert.IsFalse(view.IsRevealPending);
                Assert.IsFalse(view.IsFadeInActive);
                Assert.AreEqual(1f, view.CurrentRevealAlpha, 0.001f);
                Assert.AreEqual(0.28f, view.CurrentBackdropAlpha, 0.001f);
                Assert.AreEqual("Chapter 01 Cleared", view.CurrentTitle);
                Assert.AreEqual("Result: Campus Gatekeeper defeated.", view.CurrentResultLine);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        private static void InvokePrivateMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, arguments);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
