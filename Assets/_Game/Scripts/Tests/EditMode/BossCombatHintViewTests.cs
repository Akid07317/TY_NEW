using System.Reflection;
using CampusRPG.Combat;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossCombatHintViewTests
    {
        [Test]
        public void BossCombatHintPlanner_BuildsReadableGatekeeperMessage()
        {
            BossCombatHintPlan gatekeeperPlan = BossCombatHintPlanner.Build(Chapter01Ids.Encounters.Gatekeeper);
            BossCombatHintPlan nonBossPlan = BossCombatHintPlanner.Build(Chapter01Ids.Encounters.Courtyard);

            Assert.AreEqual("Gatekeeper Tactics", gatekeeperPlan.Title);
            Assert.AreEqual("Block the close strings, dodge the wide shockwaves, and punish the long recovery windows.", gatekeeperPlan.Body);
            Assert.IsTrue(gatekeeperPlan.IsVisible);
            Assert.IsFalse(nonBossPlan.IsVisible);
        }

        [Test]
        public void BossCombatHintView_ShowsWhenGatekeeperEncounterStarts()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject encounterObject = new GameObject("Encounter");
            GameObject presenterObject = new GameObject("BossCombatHintView");
            GameObject enemy = null;

            try
            {
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Boss, "Boss Arena")
                    });

                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokeMethod(progressService, "Awake");

                EncounterController encounter = encounterObject.AddComponent<EncounterController>();
                SetPrivateField(encounter, "encounterId", Chapter01Ids.Encounters.Gatekeeper);
                SetPrivateField(encounter, "activateOnPlayerEnter", true);
                SetPrivateField(encounter, "startActive", false);
                SetPrivateField(encounter, "chapterProgressService", progressService);

                enemy = CreateEncounterEnemy(encounterObject.transform, "Boss_Gatekeeper");

                InvokeMethod(encounter, "Awake");
                InvokeMethod(encounter, "OnEnable");

                BossCombatHintView view = presenterObject.AddComponent<BossCombatHintView>();
                SetPrivateField(view, "bossEncounter", encounter);
                SetPrivateField(view, "visibleDurationSeconds", 0.6f);

                InvokeMethod(view, "Tick", 0f);
                Assert.IsFalse(view.IsVisible);

                encounter.ActivateEncounter();
                InvokeMethod(view, "Tick", 0f);
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Gatekeeper Tactics", view.CurrentTitle);

                InvokeMethod(view, "Tick", 0.7f);
                Assert.IsFalse(view.IsVisible);

                enemy.GetComponent<HealthComponent>().ReceiveDamage(999f, Vector3.zero, null);
                InvokeMethod(view, "Tick", 0f);
                Assert.IsFalse(view.IsVisible);
            }
            finally
            {
                if (enemy != null)
                {
                    Object.DestroyImmediate(enemy);
                }

                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(encounterObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        private static GameObject CreateEncounterEnemy(Transform parent, string name)
        {
            GameObject enemyObject = new GameObject(name);
            enemyObject.transform.SetParent(parent);
            enemyObject.AddComponent<HealthComponent>();
            EnemyEncounterMember member = enemyObject.AddComponent<EnemyEncounterMember>();
            InvokeMethod(member, "Awake");
            InvokeMethod(member, "OnEnable");
            return enemyObject;
        }

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            }

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
