using System.Reflection;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EncounterSealViewTests
    {
        [Test]
        public void EncounterSealPlanner_BuildsReadableChapter01Messages()
        {
            EncounterSealPlan tutorialPlan = EncounterSealPlanner.Build(Chapter01Ids.Encounters.EntranceTutorial);
            EncounterSealPlan courtyardPlan = EncounterSealPlanner.Build(Chapter01Ids.Encounters.Courtyard);
            EncounterSealPlan interiorPlan = EncounterSealPlanner.Build(Chapter01Ids.Encounters.Interior);
            EncounterSealPlan bossPlan = EncounterSealPlanner.Build(Chapter01Ids.Encounters.Gatekeeper);

            Assert.AreEqual("Training Trial", tutorialPlan.Title);
            Assert.AreEqual("Defeat the tutorial enemies before leaving the entrance.", tutorialPlan.Body);
            Assert.AreEqual("Courtyard Sealed", courtyardPlan.Title);
            Assert.AreEqual("Clear the mixed squad to reopen the school interior.", courtyardPlan.Body);
            Assert.AreEqual("Room Sealed", interiorPlan.Title);
            Assert.AreEqual("Defeat every enemy in the room to break the seal and recover the Gate Sigil.", interiorPlan.Body);
            Assert.IsFalse(bossPlan.IsVisible);
        }

        [Test]
        public void EncounterSealView_ShowsForNonBossEncountersOnly()
        {
            GameObject viewObject = new GameObject("EncounterSealView");
            GameObject tutorialEncounterObject = new GameObject("TutorialEncounter");
            GameObject bossEncounterObject = new GameObject("BossEncounter");

            try
            {
                EncounterSealView view = viewObject.AddComponent<EncounterSealView>();
                SetPrivateField(view, "visibleDurationSeconds", 0.6f);
                InvokeMethod(view, "OnEnable");

                EncounterController tutorialEncounter = tutorialEncounterObject.AddComponent<EncounterController>();
                SetPrivateField(tutorialEncounter, "encounterId", Chapter01Ids.Encounters.EntranceTutorial);
                tutorialEncounter.ActivateEncounter();

                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Training Trial", view.CurrentTitle);

                SetPrivateField(view, "isVisible", false);
                EncounterController bossEncounter = bossEncounterObject.AddComponent<EncounterController>();
                SetPrivateField(bossEncounter, "encounterId", Chapter01Ids.Encounters.Gatekeeper);
                bossEncounter.ActivateEncounter();

                Assert.IsFalse(view.IsVisible);
            }
            finally
            {
                InvokeMethod(viewObject.GetComponent<EncounterSealView>(), "OnDisable");
                Object.DestroyImmediate(tutorialEncounterObject);
                Object.DestroyImmediate(bossEncounterObject);
                Object.DestroyImmediate(viewObject);
            }
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
