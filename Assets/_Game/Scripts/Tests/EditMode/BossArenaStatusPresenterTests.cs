using System.Reflection;
using CampusRPG.Combat;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossArenaStatusPresenterTests
    {
        [Test]
        public void BossArenaStatusPresenter_ShowsOnEncounterSealAndClear()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject encounterObject = new GameObject("Encounter");
            GameObject presenterObject = new GameObject("BossArenaStatusPresenter");
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

                BossArenaStatusPresenter presenter = presenterObject.AddComponent<BossArenaStatusPresenter>();
                SetPrivateField(presenter, "bossEncounter", encounter);
                SetPrivateField(presenter, "sealedVisibleDurationSeconds", 0.6f);
                SetPrivateField(presenter, "clearedVisibleDurationSeconds", 0.25f);

                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);

                encounter.ActivateEncounter();
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Arena Sealed", presenter.CurrentTitle);
                Assert.AreEqual("Gatekeeper has locked the arena behind you.", presenter.CurrentBody);
                Assert.AreEqual(0.6f, presenter.RemainingVisibleSeconds, 0.001f);
                Assert.AreEqual(0f, presenter.CurrentAlpha, 0.001f);

                InvokeMethod(presenter, "Tick", 0.1f);
                Assert.Greater(presenter.CurrentAlpha, 0f);
                Assert.Less(presenter.CurrentAlpha, 1f);

                InvokeMethod(presenter, "Tick", 0.7f);
                Assert.IsFalse(presenter.IsVisible);

                enemy.GetComponent<HealthComponent>().ReceiveDamage(999f, Vector3.zero, null);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Gatekeeper Down", presenter.CurrentTitle);
                Assert.AreEqual("Walk forward and pick up the Ritual Core to finish the chapter.", presenter.CurrentBody);
                Assert.AreEqual(0.25f, presenter.RemainingVisibleSeconds, 0.001f);
                Assert.AreEqual(0f, presenter.CurrentAlpha, 0.001f);

                InvokeMethod(presenter, "Tick", 0.05f);
                Assert.Greater(presenter.CurrentAlpha, 0f);
                Assert.Less(presenter.CurrentAlpha, 1f);

                progressService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);
                Assert.AreEqual(string.Empty, presenter.CurrentTitle);
                Assert.AreEqual(string.Empty, presenter.CurrentBody);
                Assert.AreEqual(0f, presenter.RemainingVisibleSeconds, 0.001f);
                Assert.AreEqual(0f, presenter.CurrentAlpha, 0.001f);

                InvokeMethod(presenter, "Tick", 0.3f);
                Assert.IsFalse(presenter.IsVisible);
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
