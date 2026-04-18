using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class ChapterRouteBlockHintViewTests
    {
        [Test]
        public void ChapterRouteBlockHintPlanner_BuildsReadableChapter01RouteMessages()
        {
            ChapterRouteBlockHintPlan tutorialPlan = ChapterRouteBlockHintPlanner.Build(
                new DoorRequirementHintRequest(string.Empty, Chapter01Ids.Encounters.EntranceTutorial, string.Empty));
            ChapterRouteBlockHintPlan courtyardPlan = ChapterRouteBlockHintPlanner.Build(
                new DoorRequirementHintRequest(string.Empty, Chapter01Ids.Encounters.Courtyard, string.Empty));
            ChapterRouteBlockHintPlan bossGatePlan = ChapterRouteBlockHintPlanner.Build(
                new DoorRequirementHintRequest(string.Empty, string.Empty, Chapter01Ids.KeyItems.GateSigil));
            ChapterRouteBlockHintPlan ritualCorePlan = ChapterRouteBlockHintPlanner.Build(
                new DoorRequirementHintRequest(string.Empty, Chapter01Ids.Encounters.Gatekeeper, string.Empty));

            Assert.AreEqual("Training Gate Locked", tutorialPlan.Title);
            Assert.AreEqual("Clear the tutorial enemies before you move into the courtyard.", tutorialPlan.Body);
            Assert.AreEqual("Courtyard Route Locked", courtyardPlan.Title);
            Assert.AreEqual("Win the courtyard skirmish before you push into the school interior.", courtyardPlan.Body);
            Assert.AreEqual("Boss Gate Sealed", bossGatePlan.Title);
            Assert.AreEqual("Recover the Gate Sigil from the interior room to open this route.", bossGatePlan.Body);
            Assert.AreEqual("Ritual Core Sealed", ritualCorePlan.Title);
            Assert.AreEqual("Defeat the Campus Gatekeeper before the Ritual Core route will open.", ritualCorePlan.Body);
        }

        [Test]
        public void DoorRequirementHintTrigger_ShowsHintWhenPlayerReachesBlockedBossGate()
        {
            ChapterProgressionSO progression = CreateProgression();
            GameObject flowObject = new GameObject("ChapterFlow");
            GameObject viewObject = new GameObject("RouteBlockHintView");
            GameObject triggerObject = new GameObject("BossGateHint");
            GameObject playerObject = new GameObject("Player");

            try
            {
                ChapterProgressService progressService = flowObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                ChapterRouteBlockHintView view = viewObject.AddComponent<ChapterRouteBlockHintView>();
                InvokePrivateMethod(view, "OnEnable");

                DoorRequirementHintTrigger trigger = triggerObject.AddComponent<DoorRequirementHintTrigger>();
                SetPrivateField(trigger, "requiredKeyItemId", Chapter01Ids.KeyItems.GateSigil);
                SetPrivateField(trigger, "retriggerCooldownSeconds", 0f);
                SetPrivateField(trigger, "chapterProgressService", progressService);
                InvokePrivateMethod(trigger, "Awake");
                InvokePrivateMethod(trigger, "OnEnable");

                playerObject.AddComponent<PlayerCharacter>();
                Collider playerCollider = playerObject.AddComponent<BoxCollider>();

                InvokePrivateMethod(trigger, "OnTriggerEnter", playerCollider);

                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Boss Gate Sealed", view.CurrentTitle);
                Assert.AreEqual("Recover the Gate Sigil from the interior room to open this route.", view.CurrentBody);
            }
            finally
            {
                CleanupObject(viewObject);
                CleanupObject(triggerObject);
                CleanupObject(playerObject);
                CleanupObject(flowObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void DoorRequirementHintTrigger_DoesNotShowHintWhenRouteIsAlreadyUnlocked()
        {
            ChapterProgressionSO progression = CreateProgression();
            GameObject flowObject = new GameObject("ChapterFlow");
            GameObject viewObject = new GameObject("RouteBlockHintView");
            GameObject triggerObject = new GameObject("BossGateHint");
            GameObject playerObject = new GameObject("Player");

            try
            {
                ChapterProgressService progressService = flowObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");
                progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil);

                ChapterRouteBlockHintView view = viewObject.AddComponent<ChapterRouteBlockHintView>();
                InvokePrivateMethod(view, "OnEnable");

                DoorRequirementHintTrigger trigger = triggerObject.AddComponent<DoorRequirementHintTrigger>();
                SetPrivateField(trigger, "requiredKeyItemId", Chapter01Ids.KeyItems.GateSigil);
                SetPrivateField(trigger, "chapterProgressService", progressService);
                InvokePrivateMethod(trigger, "Awake");
                InvokePrivateMethod(trigger, "OnEnable");

                playerObject.AddComponent<PlayerCharacter>();
                Collider playerCollider = playerObject.AddComponent<BoxCollider>();

                InvokePrivateMethod(trigger, "OnTriggerEnter", playerCollider);

                Assert.IsFalse(view.IsVisible);
            }
            finally
            {
                CleanupObject(viewObject);
                CleanupObject(triggerObject);
                CleanupObject(playerObject);
                CleanupObject(flowObject);
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
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance"),
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Courtyard, "Courtyard"),
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Interior, "Interior"),
                    new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Boss, "Boss")
                });
            return progression;
        }

        private static void CleanupObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    MethodInfo onDisable = behaviours[i].GetType().GetMethod(
                        "OnDisable",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    onDisable?.Invoke(behaviours[i], null);
                }
            }

            Object.DestroyImmediate(gameObject);
        }

        private static void InvokePrivateMethod(object instance, string methodName, params object[] args)
        {
            MethodInfo[] methods = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo method = null;

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = methods[i].GetParameters();

                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool matches = true;

                for (int j = 0; j < parameters.Length; j++)
                {
                    if (args[j] == null)
                    {
                        continue;
                    }

                    if (!parameters[j].ParameterType.IsInstanceOfType(args[j]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    method = methods[i];
                    break;
                }
            }

            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, args);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
