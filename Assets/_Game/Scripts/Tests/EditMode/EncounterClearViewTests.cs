using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EncounterClearViewTests
    {
        [Test]
        public void EncounterClearPlanner_BuildsReadableChapter01Messages()
        {
            EncounterClearPlan tutorialPlan = EncounterClearPlanner.Build(Chapter01Ids.Encounters.EntranceTutorial);
            EncounterClearPlan courtyardPlan = EncounterClearPlanner.Build(Chapter01Ids.Encounters.Courtyard);
            EncounterClearPlan interiorPlan = EncounterClearPlanner.Build(Chapter01Ids.Encounters.Interior);
            EncounterClearPlan bossPlan = EncounterClearPlanner.Build(Chapter01Ids.Encounters.Gatekeeper);

            Assert.AreEqual("Training Complete", tutorialPlan.Title);
            Assert.AreEqual("The route forward is open. Activate CP01 before you leave the entrance.", tutorialPlan.Body);
            Assert.AreEqual("Courtyard Secured", courtyardPlan.Title);
            Assert.AreEqual("The school interior is open. Push inside and keep the pressure on.", courtyardPlan.Body);
            Assert.AreEqual("Seal Broken", interiorPlan.Title);
            Assert.AreEqual("The room is clear. Recover the Gate Sigil and head for the boss gate.", interiorPlan.Body);
            Assert.IsFalse(bossPlan.IsVisible);
        }

        [Test]
        public void EncounterClearView_ShowsForNonBossEncountersOnly()
        {
            ChapterProgressionSO progression = CreateProgression();
            GameObject flowObject = new GameObject("ChapterFlow");

            try
            {
                ChapterProgressService progressService = flowObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokeMethod(progressService, "Awake");

                EncounterClearView view = flowObject.AddComponent<EncounterClearView>();
                SetPrivateField(view, "visibleDurationSeconds", 0.6f);
                SetPrivateField(view, "chapterProgressService", progressService);
                InvokeMethod(view, "Awake");
                InvokeMethod(view, "OnEnable");

                Assert.IsFalse(view.IsVisible);

                progressService.MarkEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial);
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Training Complete", view.CurrentTitle);

                progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Interior);
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Seal Broken", view.CurrentTitle);

                SetPrivateField(view, "isVisible", false);
                progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Gatekeeper);
                Assert.IsFalse(view.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(flowObject);
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
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance Tutorial")
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
    }
}
