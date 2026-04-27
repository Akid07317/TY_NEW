using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class KeyItemAcquisitionViewTests
    {
        [Test]
        public void KeyItemAcquisitionPlanner_BuildsReadableChapter01Messages()
        {
            KeyItemAcquisitionPlan gateSigilPlan = KeyItemAcquisitionPlanner.Build(Chapter01Ids.KeyItems.GateSigil);
            Assert.IsTrue(gateSigilPlan.IsVisible);
            Assert.AreEqual("Gate Sigil Recovered", gateSigilPlan.Title);
            Assert.AreEqual("The boss gate is open. Push forward into the gatekeeper arena.", gateSigilPlan.Body);

            KeyItemAcquisitionPlan sideRouteCachePlan = KeyItemAcquisitionPlanner.Build(Chapter01Ids.KeyItems.SideRouteCache);
            Assert.IsTrue(sideRouteCachePlan.IsVisible);
            Assert.AreEqual("Side Route Cache Recovered", sideRouteCachePlan.Title);
            Assert.AreEqual("Optional cache secured. Use the shortcut return or push toward the boss gate.", sideRouteCachePlan.Body);

            KeyItemAcquisitionPlan ritualCorePlan = KeyItemAcquisitionPlanner.Build(Chapter01Ids.KeyItems.RitualCore);
            Assert.IsTrue(ritualCorePlan.IsVisible);
            Assert.AreEqual("Ritual Core Recovered", ritualCorePlan.Title);
            Assert.AreEqual("Chapter target secured. This chapter is now marked complete.", ritualCorePlan.Body);

            KeyItemAcquisitionPlan hiddenCompletionPlan = KeyItemAcquisitionPlanner.Build(Chapter01Ids.KeyItems.RitualCore, true);
            Assert.IsFalse(hiddenCompletionPlan.IsVisible);
        }

        [Test]
        public void KeyItemAcquisitionView_ShowsWhenChapterProgressServiceFires()
        {
            ChapterProgressionSO progression = CreateProgression();
            GameObject flowObject = new GameObject("ChapterFlow");

            try
            {
                ChapterProgressService progressService = flowObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokeMethod(progressService, "Awake");

                KeyItemAcquisitionView view = flowObject.AddComponent<KeyItemAcquisitionView>();
                SetPrivateField(view, "visibleDurationSeconds", 0.6f);
                SetPrivateField(view, "chapterProgressService", progressService);
                InvokeMethod(view, "Awake");
                InvokeMethod(view, "OnEnable");

                Assert.IsFalse(view.IsVisible);

                progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil);
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Gate Sigil Recovered", view.CurrentTitle);
                Assert.AreEqual("The boss gate is open. Push forward into the gatekeeper arena.", view.CurrentBody);

                progressService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore);
                Assert.IsFalse(view.IsVisible);
                Assert.AreEqual(string.Empty, view.CurrentTitle);
                Assert.AreEqual(string.Empty, view.CurrentBody);
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
