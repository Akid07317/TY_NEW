using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class ChapterObjectiveViewTests
    {
        [Test]
        public void ChapterObjectivePlanner_BuildsReadableChapter01Goals()
        {
            ChapterProgressionSO progression = CreateProgression();

            try
            {
                ChapterObjectivePlan entrancePlan = ChapterObjectivePlanner.Build(
                    progression,
                    string.Empty,
                    false,
                    false,
                    false);
                Assert.IsTrue(entrancePlan.IsVisible);
                Assert.AreEqual("Entrance Tutorial", entrancePlan.Heading);
                Assert.AreEqual("Finish the tutorial encounter and activate CP01.", entrancePlan.Body);

                ChapterObjectivePlan interiorPlan = ChapterObjectivePlanner.Build(
                    progression,
                    Chapter01Ids.Areas.Interior,
                    false,
                    false,
                    false);
                Assert.AreEqual("School Interior", interiorPlan.Heading);
                Assert.AreEqual("Clear the sealed room and recover the Gate Sigil.", interiorPlan.Body);

                ChapterObjectivePlan gateOpenPlan = ChapterObjectivePlanner.Build(
                    progression,
                    Chapter01Ids.Areas.Interior,
                    true,
                    false,
                    false);
                Assert.AreEqual("Boss Gate Open", gateOpenPlan.Heading);
                Assert.AreEqual("The Gate Sigil unlocked the boss route. Push forward and challenge the gatekeeper.", gateOpenPlan.Body);

                ChapterObjectivePlan bossClearPlan = ChapterObjectivePlanner.Build(
                    progression,
                    Chapter01Ids.Areas.Boss,
                    true,
                    true,
                    false);
                Assert.AreEqual("Ritual Core Ahead", bossClearPlan.Heading);
                Assert.AreEqual("The gatekeeper is down. Walk forward and pick up the Ritual Core to finish the chapter.", bossClearPlan.Body);
                Assert.IsFalse(bossClearPlan.ShouldHighlightOnChange);

                ChapterObjectivePlan completedPlan = ChapterObjectivePlanner.Build(
                    progression,
                    Chapter01Ids.Areas.Boss,
                    true,
                    true,
                    true);
                Assert.IsFalse(completedPlan.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void ChapterObjectiveView_TracksProgressChangesAndCompletion()
        {
            ChapterProgressionSO progression = CreateProgression();
            GameObject progressObject = new GameObject("ChapterFlow");
            GameObject viewObject = new GameObject("ChapterObjectiveView");

            try
            {
                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                ChapterObjectiveView view = viewObject.AddComponent<ChapterObjectiveView>();
                SetPrivateField(view, "chapterProgressService", progressService);
                InvokePrivateMethod(view, "Awake");
                InvokePrivateMethod(view, "OnEnable");

                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Entrance Tutorial", view.CurrentHeading);
                Assert.AreEqual("Finish the tutorial encounter and activate CP01.", view.CurrentBody);
                Assert.IsFalse(view.IsHighlightActive);

                progressService.EnterArea(Chapter01Ids.Areas.Courtyard);
                Assert.AreEqual("Outdoor Courtyard", view.CurrentHeading);
                Assert.AreEqual("Win the courtyard skirmish, then push into the school interior.", view.CurrentBody);
                Assert.IsTrue(view.IsHighlightActive);
                SetPrivateField(view, "highlightRemainingSeconds", 0f);

                progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil);
                Assert.AreEqual("Boss Gate Open", view.CurrentHeading);
                Assert.AreEqual("The Gate Sigil unlocked the boss route. Push forward and challenge the gatekeeper.", view.CurrentBody);
                Assert.IsTrue(view.IsHighlightActive);
                SetPrivateField(view, "highlightRemainingSeconds", 0f);

                progressService.EnterArea(Chapter01Ids.Areas.Boss);
                Assert.AreEqual("Boss Arena", view.CurrentHeading);
                Assert.AreEqual("Defeat the Campus Gatekeeper and secure the Ritual Core.", view.CurrentBody);
                Assert.IsTrue(view.IsHighlightActive);
                SetPrivateField(view, "highlightRemainingSeconds", 0f);

                progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Gatekeeper);
                Assert.AreEqual("Ritual Core Ahead", view.CurrentHeading);
                Assert.AreEqual("The gatekeeper is down. Walk forward and pick up the Ritual Core to finish the chapter.", view.CurrentBody);
                Assert.IsFalse(view.IsHighlightActive);

                progressService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore);
                Assert.IsFalse(view.IsVisible);
                Assert.IsFalse(view.IsHighlightActive);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
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
