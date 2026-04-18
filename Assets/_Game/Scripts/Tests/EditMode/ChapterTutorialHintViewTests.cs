using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class ChapterTutorialHintViewTests
    {
        [Test]
        public void ChapterTutorialHintPlanner_BuildsReadableEntranceSequence()
        {
            ChapterTutorialHintPlan movementPlan = ChapterTutorialHintPlanner.Build(string.Empty, false, false, false, false, false, false);
            Assert.IsTrue(movementPlan.IsVisible);
            Assert.AreEqual("Get Moving", movementPlan.Title);

            ChapterTutorialHintPlan lockOnPlan = ChapterTutorialHintPlanner.Build(Chapter01Ids.Areas.Entrance, false, false, true, false, false, false);
            Assert.AreEqual("Lock On", lockOnPlan.Title);

            ChapterTutorialHintPlan attackPlan = ChapterTutorialHintPlanner.Build(Chapter01Ids.Areas.Entrance, false, false, true, true, false, false);
            Assert.AreEqual("Open the Fight", attackPlan.Title);

            ChapterTutorialHintPlan defensePlan = ChapterTutorialHintPlanner.Build(Chapter01Ids.Areas.Entrance, false, false, true, true, true, false);
            Assert.AreEqual("Stay Safe", defensePlan.Title);

            ChapterTutorialHintPlan finishPlan = ChapterTutorialHintPlanner.Build(Chapter01Ids.Areas.Entrance, false, false, true, true, true, true);
            Assert.AreEqual("Finish the Drill", finishPlan.Title);

            ChapterTutorialHintPlan hiddenPlan = ChapterTutorialHintPlanner.Build(Chapter01Ids.Areas.Courtyard, false, false, true, true, true, true);
            Assert.IsFalse(hiddenPlan.IsVisible);
        }

        [Test]
        public void ChapterTutorialHintView_TracksTutorialMilestonesAndHidesAfterClear()
        {
            ChapterProgressionSO progression = CreateProgression();
            GameObject progressObject = new GameObject("ChapterFlow");
            GameObject viewObject = new GameObject("ChapterTutorialHintView");

            try
            {
                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokeMethod(progressService, "Awake");
                progressService.EnterArea(Chapter01Ids.Areas.Entrance);

                ChapterTutorialHintView view = viewObject.AddComponent<ChapterTutorialHintView>();
                SetPrivateField(view, "chapterProgressService", progressService);
                InvokeMethod(view, "Awake");
                InvokeMethod(view, "OnEnable");

                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Get Moving", view.CurrentTitle);

                InvokeMethod(view, "MarkMovementInputSeen");
                Assert.AreEqual("Lock On", view.CurrentTitle);

                InvokeMethod(view, "MarkLockOnSeen");
                Assert.AreEqual("Open the Fight", view.CurrentTitle);

                InvokeMethod(view, "MarkAttackSeen");
                Assert.AreEqual("Stay Safe", view.CurrentTitle);

                InvokeMethod(view, "MarkDefenseSeen");
                Assert.AreEqual("Finish the Drill", view.CurrentTitle);

                progressService.MarkEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial);
                Assert.IsFalse(view.IsVisible);
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
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Courtyard, "Outdoor Courtyard")
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
