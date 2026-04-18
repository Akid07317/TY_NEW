using System.Reflection;
using CampusRPG.Combat;
using CampusRPG.Interaction;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EncounterControllerTests
    {
        [Test]
        public void EncounterController_ClearsAfterAllMembersAreDefeated()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject encounterObject = new GameObject("Encounter");
            GameObject blockerObject = new GameObject("Blocker");
            GameObject enemyA = null;
            GameObject enemyB = null;

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

                EncounterController encounter = encounterObject.AddComponent<EncounterController>();
                SetPrivateField(encounter, "encounterId", Chapter01Ids.Encounters.EntranceTutorial);
                SetPrivateField(encounter, "activateOnPlayerEnter", false);
                SetPrivateField(encounter, "startActive", false);
                SetPrivateField(encounter, "chapterProgressService", progressService);
                SetPrivateField(encounter, "blockersToEnableWhileActive", new[] { blockerObject });

                enemyA = CreateEncounterEnemy(encounterObject.transform, "Enemy_A");
                enemyB = CreateEncounterEnemy(encounterObject.transform, "Enemy_B");

                InvokePrivateMethod(encounter, "Awake");
                InvokePrivateMethod(encounter, "OnEnable");
                Assert.IsFalse(encounter.IsActive);
                encounter.ActivateEncounter();

                Assert.IsTrue(blockerObject.activeSelf);
                Assert.IsTrue(enemyA.activeSelf);
                Assert.IsTrue(enemyB.activeSelf);

                enemyA.GetComponent<HealthComponent>().ReceiveDamage(999f, Vector3.zero, null);
                Assert.IsFalse(progressService.IsEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial));

                enemyB.GetComponent<HealthComponent>().ReceiveDamage(999f, Vector3.zero, null);

                Assert.IsTrue(progressService.IsEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial));
                Assert.IsTrue(encounter.IsCleared);
                Assert.IsFalse(blockerObject.activeSelf);
                Assert.IsFalse(enemyA.activeSelf);
                Assert.IsFalse(enemyB.activeSelf);
            }
            finally
            {
                if (enemyA != null)
                {
                    Object.DestroyImmediate(enemyA);
                }

                if (enemyB != null)
                {
                    Object.DestroyImmediate(enemyB);
                }

                Object.DestroyImmediate(blockerObject);
                Object.DestroyImmediate(encounterObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void EncounterController_ApplyClearedProgress_DisablesEncounterMembers()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject encounterObject = new GameObject("Encounter");
            GameObject enemy = null;

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

                EncounterController encounter = encounterObject.AddComponent<EncounterController>();
                SetPrivateField(encounter, "encounterId", Chapter01Ids.Encounters.EntranceTutorial);
                SetPrivateField(encounter, "activateOnPlayerEnter", false);
                SetPrivateField(encounter, "startActive", false);
                SetPrivateField(encounter, "chapterProgressService", progressService);

                enemy = CreateEncounterEnemy(encounterObject.transform, "Enemy_A");

                InvokePrivateMethod(encounter, "Awake");
                InvokePrivateMethod(encounter, "OnEnable");
                Assert.IsFalse(encounter.IsActive);
                encounter.ActivateEncounter();

                Assert.IsTrue(enemy.activeSelf);

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    clearedEncounterIds = new[] { Chapter01Ids.Encounters.EntranceTutorial }
                });

                Assert.IsTrue(encounter.IsCleared);
                Assert.IsFalse(enemy.activeSelf);
            }
            finally
            {
                if (enemy != null)
                {
                    Object.DestroyImmediate(enemy);
                }

                Object.DestroyImmediate(encounterObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void EncounterController_ResetForCheckpointRestore_ResetsUnclearedEncounter()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject encounterObject = new GameObject("Encounter");
            GameObject blockerObject = new GameObject("Blocker");
            GameObject enemyA = null;
            GameObject enemyB = null;

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

                EncounterController encounter = encounterObject.AddComponent<EncounterController>();
                SetPrivateField(encounter, "encounterId", Chapter01Ids.Encounters.EntranceTutorial);
                SetPrivateField(encounter, "activateOnPlayerEnter", true);
                SetPrivateField(encounter, "startActive", false);
                SetPrivateField(encounter, "chapterProgressService", progressService);
                SetPrivateField(encounter, "blockersToEnableWhileActive", new[] { blockerObject });

                enemyA = CreateEncounterEnemy(encounterObject.transform, "Enemy_A");
                enemyB = CreateEncounterEnemy(encounterObject.transform, "Enemy_B");

                InvokePrivateMethod(encounter, "Awake");
                InvokePrivateMethod(encounter, "OnEnable");
                encounter.ActivateEncounter();

                enemyA.GetComponent<HealthComponent>().ReceiveDamage(999f, Vector3.zero, null);
                Assert.IsFalse(enemyA.activeSelf);
                Assert.IsTrue(enemyB.activeSelf);
                Assert.IsTrue(blockerObject.activeSelf);

                encounter.ResetForCheckpointRestore();

                Assert.IsFalse(encounter.IsActive);
                Assert.IsFalse(encounter.IsCleared);
                Assert.IsFalse(progressService.IsEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial));
                Assert.IsFalse(blockerObject.activeSelf);
                Assert.IsFalse(enemyA.activeSelf);
                Assert.IsFalse(enemyB.activeSelf);

                encounter.ActivateEncounter();

                Assert.IsTrue(enemyA.activeSelf);
                Assert.IsTrue(enemyB.activeSelf);
            }
            finally
            {
                if (enemyA != null)
                {
                    Object.DestroyImmediate(enemyA);
                }

                if (enemyB != null)
                {
                    Object.DestroyImmediate(enemyB);
                }

                Object.DestroyImmediate(blockerObject);
                Object.DestroyImmediate(encounterObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void EncounterController_ManualEncounter_DoesNotAutoActivateOnEnableOrProgressRefresh()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject encounterObject = new GameObject("Encounter");
            GameObject blockerObject = new GameObject("Blocker");
            GameObject enemy = null;

            try
            {
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance"),
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Courtyard, "Courtyard")
                    });

                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                EncounterController encounter = encounterObject.AddComponent<EncounterController>();
                SetPrivateField(encounter, "encounterId", Chapter01Ids.Encounters.EntranceTutorial);
                SetPrivateField(encounter, "activateOnPlayerEnter", false);
                SetPrivateField(encounter, "startActive", false);
                SetPrivateField(encounter, "chapterProgressService", progressService);
                SetPrivateField(encounter, "blockersToEnableWhileActive", new[] { blockerObject });

                enemy = CreateEncounterEnemy(encounterObject.transform, "Enemy_A");

                InvokePrivateMethod(encounter, "Awake");
                InvokePrivateMethod(encounter, "OnEnable");

                Assert.IsFalse(encounter.IsActive);
                Assert.IsFalse(encounter.IsCleared);
                Assert.IsFalse(blockerObject.activeSelf);
                Assert.IsFalse(enemy.activeSelf);

                progressService.EnterArea(Chapter01Ids.Areas.Courtyard);

                Assert.IsFalse(encounter.IsActive);
                Assert.IsFalse(encounter.IsCleared);
                Assert.IsFalse(blockerObject.activeSelf);
                Assert.IsFalse(enemy.activeSelf);
            }
            finally
            {
                if (enemy != null)
                {
                    Object.DestroyImmediate(enemy);
                }

                Object.DestroyImmediate(blockerObject);
                Object.DestroyImmediate(encounterObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        private static GameObject CreateEncounterEnemy(Transform parent, string name)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.SetParent(parent);
            enemy.AddComponent<HealthComponent>();
            EnemyEncounterMember member = enemy.AddComponent<EnemyEncounterMember>();
            InvokePrivateMethod(member, "Awake");
            InvokePrivateMethod(member, "OnEnable");
            return enemy;
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
