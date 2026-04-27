using System.Collections;
using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Interaction;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CampusRPG.Tests.PlayMode
{
    public sealed class BossTestRuntimeFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator BossTest_GatekeeperEncounter_ActivatesLocksAndClears()
        {
            yield return LoadSceneAndWait("BossTest");

            ChapterProgressService progressService = FindRequiredComponent<ChapterProgressService>("BossTestFlow");
            PlayerCharacter player = FindRequiredComponent<PlayerCharacter>("Player");
            EncounterController bossEncounter = FindRequiredComponent<EncounterController>("Encounter_BossTest_Gatekeeper");
            HealthComponent bossHealth = FindRequiredComponent<HealthComponent>("Boss_Gatekeeper");
            GameObject bossBarrier = FindSceneObject("BossArenaBarrier");

            SetPrivateField<CheckpointRestoreCoordinator>(progressService, "checkpointRestoreCoordinator", null);
            progressService.RestoreFromSave(new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                checkpointId = "BossTest_Start",
                currentAreaId = Chapter01Ids.Areas.Boss,
                visitedAreaIds = new[] { Chapter01Ids.Areas.Boss },
                clearedEncounterIds = new string[0],
                chapterCompleted = false,
                playerHealth = player.Health != null ? player.Health.MaxValue : 100f,
                playerMana = player.Mana != null ? player.Mana.MaxValue : 100f
            });

            yield return null;

            Assert.IsNotNull(bossBarrier);
            Assert.IsFalse(progressService.IsEncounterCleared(Chapter01Ids.Encounters.Gatekeeper));
            Assert.IsFalse(bossEncounter.IsActive);
            Assert.IsFalse(bossEncounter.IsCleared);
            Assert.IsFalse(bossBarrier.activeSelf);
            Assert.IsFalse(bossHealth.gameObject.activeSelf);

            bossEncounter.ActivateEncounter();
            yield return null;

            Assert.IsTrue(bossEncounter.IsActive);
            Assert.IsFalse(bossEncounter.IsCleared);
            Assert.IsTrue(bossBarrier.activeSelf);
            Assert.IsTrue(bossHealth.gameObject.activeSelf);
            Assert.IsFalse(bossHealth.IsDead);

            bossHealth.ReceiveDamage(bossHealth.MaxValue + 250f, bossHealth.transform.position, player.gameObject);
            yield return null;

            Assert.IsFalse(bossEncounter.IsActive);
            Assert.IsTrue(bossEncounter.IsCleared);
            Assert.IsTrue(progressService.IsEncounterCleared(Chapter01Ids.Encounters.Gatekeeper));
            Assert.IsFalse(bossBarrier.activeSelf);
            Assert.IsFalse(bossHealth.gameObject.activeSelf);
        }

        private static IEnumerator LoadSceneAndWait(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.IsNotNull(operation, sceneName);

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static TComponent FindRequiredComponent<TComponent>(string objectName) where TComponent : Component
        {
            GameObject gameObject = FindSceneObject(objectName);
            Assert.IsNotNull(gameObject, objectName);

            TComponent component = gameObject.GetComponent<TComponent>();
            Assert.IsNotNull(component, typeof(TComponent).Name + " on " + objectName);
            return component;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject gameObject = gameObjects[i];

                if (gameObject.scene == activeScene && gameObject.name == objectName)
                {
                    return gameObject;
                }
            }

            return null;
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
