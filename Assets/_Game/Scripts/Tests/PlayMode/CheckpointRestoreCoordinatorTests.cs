using System;
using System.Collections;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Save;
using CampusRPG.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CampusRPG.Tests.PlayMode
{
    public sealed class CheckpointRestoreCoordinatorTests
    {
        [UnityTest]
        public IEnumerator DeathRestore_ReturnsPlayerToActivatedCheckpoint()
        {
            string fileName = $"test_checkpoint_restore_{Guid.NewGuid():N}.json";

            GameObject playerObject = null;
            GameObject flowObject = null;
            GameObject checkpointObject = null;
            GameObject enemyObject = null;
            CheckpointDefinitionSO checkpointDefinition = null;
            EnemyArchetypeSO enemyArchetype = null;

            try
            {
                PlayerCharacter player = CreatePlayer(out playerObject);
                CheckpointRestoreCoordinator coordinator = CreateCoordinator(player, fileName, out flowObject);
                SaveService saveService = flowObject.GetComponent<SaveService>();
                CheckpointRuntimeAnchor anchor = CreateCheckpoint(
                    coordinator,
                    "CP01",
                    new Vector3(8f, 0f, 4f),
                    out checkpointObject,
                    out checkpointDefinition);
                EnemyBrain enemy = CreateEnemy(out enemyObject, out enemyArchetype);

                yield return null;

                player.Health.ReceiveDamage(72f, Vector3.zero, null);
                player.Mana.TrySpend(55f);
                enemy.Health.ReceiveDamage(25f, Vector3.zero, player.gameObject);

                coordinator.ActivateCheckpoint(anchor);

                yield return null;

                Assert.AreEqual(player.Health.MaxValue, player.Health.CurrentValue, 0.01f);
                Assert.AreEqual(player.Mana.MaxValue, player.Mana.CurrentValue, 0.01f);

                player.transform.position = new Vector3(-3f, 0f, -2f);
                player.Health.ReceiveDamage(999f, Vector3.zero, null);

                yield return new WaitForSeconds(0.1f);

                Vector3 expectedPosition = anchor.RespawnPosition;
                Assert.That(Vector3.Distance(expectedPosition, player.transform.position), Is.LessThan(0.15f));
                Assert.AreEqual(player.Health.MaxValue, player.Health.CurrentValue, 0.01f);
                Assert.AreEqual(player.Mana.MaxValue, player.Mana.CurrentValue, 0.01f);
                Assert.AreEqual(enemy.Health.MaxValue, enemy.Health.CurrentValue, 0.01f);

                Assert.IsTrue(saveService.TryLoad(out ChapterSaveData saveData));
                Assert.NotNull(saveData);
                Assert.AreEqual("CP01", saveData.checkpointId);
                Assert.AreEqual(player.Health.MaxValue, saveData.playerHealth, 0.01f);
                Assert.AreEqual(player.Mana.MaxValue, saveData.playerMana, 0.01f);

                saveService.DeleteSave();
            }
            finally
            {
                Cleanup(enemyArchetype, checkpointDefinition, enemyObject, checkpointObject, flowObject, playerObject);
            }
        }

        [UnityTest]
        public IEnumerator ActivateCheckpoint_WhenReactivatingSameCheckpoint_RefreshesAndSavesAgain()
        {
            string fileName = $"test_checkpoint_reactivate_{Guid.NewGuid():N}.json";

            GameObject playerObject = null;
            GameObject flowObject = null;
            GameObject checkpointObject = null;
            CheckpointDefinitionSO checkpointDefinition = null;

            try
            {
                PlayerCharacter player = CreatePlayer(out playerObject);
                CheckpointRestoreCoordinator coordinator = CreateCoordinator(player, fileName, out flowObject);
                SaveService saveService = flowObject.GetComponent<SaveService>();
                CheckpointRuntimeAnchor anchor = CreateCheckpoint(
                    coordinator,
                    "CP01",
                    new Vector3(4f, 0f, 2f),
                    out checkpointObject,
                    out checkpointDefinition);

                yield return null;

                coordinator.ActivateCheckpoint(anchor);
                yield return null;

                player.Health.ReceiveDamage(40f, Vector3.zero, null);
                player.Mana.TrySpend(25f);

                coordinator.ActivateCheckpoint(anchor);
                yield return null;

                Assert.AreEqual(player.Health.MaxValue, player.Health.CurrentValue, 0.01f);
                Assert.AreEqual(player.Mana.MaxValue, player.Mana.CurrentValue, 0.01f);
                Assert.IsTrue(saveService.TryLoad(out ChapterSaveData saveData));
                Assert.NotNull(saveData);
                Assert.AreEqual("CP01", saveData.checkpointId);
                Assert.AreEqual(player.Health.MaxValue, saveData.playerHealth, 0.01f);
                Assert.AreEqual(player.Mana.MaxValue, saveData.playerMana, 0.01f);

                saveService.DeleteSave();
            }
            finally
            {
                Cleanup(checkpointDefinition, checkpointObject, flowObject, playerObject);
            }
        }

        private static PlayerCharacter CreatePlayer(out GameObject playerObject)
        {
            playerObject = new GameObject("Player");
            playerObject.SetActive(false);

            playerObject.AddComponent<CharacterController>();
            playerObject.AddComponent<HealthComponent>();
            playerObject.AddComponent<ManaComponent>();
            playerObject.AddComponent<GaugeComponent>();
            playerObject.AddComponent<AttackExecutor>();
            playerObject.AddComponent<HitboxController>();
            playerObject.AddComponent<PlayerMotor>();
            playerObject.AddComponent<PlayerStateMachine>();
            playerObject.AddComponent<PlayerCombatController>();
            playerObject.AddComponent<SkillController>();
            playerObject.AddComponent<DamageableReceiver>();
            PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();

            playerObject.SetActive(true);
            return player;
        }

        private static CheckpointRestoreCoordinator CreateCoordinator(PlayerCharacter player, string fileName, out GameObject flowObject)
        {
            flowObject = new GameObject("CheckpointFlow");
            CheckpointRestoreCoordinator coordinator = flowObject.AddComponent<CheckpointRestoreCoordinator>();
            SaveService saveService = flowObject.GetComponent<SaveService>();
            CheckpointService checkpointService = flowObject.GetComponent<CheckpointService>();

            SetPrivateField(coordinator, "player", player);
            SetPrivateField(coordinator, "saveService", saveService);
            SetPrivateField(coordinator, "checkpointService", checkpointService);
            SetPrivateField(coordinator, "chapterId", "TestChapter");
            SetPrivateField(coordinator, "defaultCheckpointId", "CP01");
            SetPrivateField(coordinator, "autoLoadFromSaveOnStart", false);
            SetPrivateField(coordinator, "autoSaveOnStart", false);
            SetPrivateField(coordinator, "respawnDelaySeconds", 0.05f);
            SetPrivateField(saveService, "fileName", fileName);

            return coordinator;
        }

        private static CheckpointRuntimeAnchor CreateCheckpoint(
            CheckpointRestoreCoordinator coordinator,
            string checkpointId,
            Vector3 position,
            out GameObject checkpointObject,
            out CheckpointDefinitionSO definition)
        {
            checkpointObject = new GameObject("Checkpoint");
            checkpointObject.SetActive(false);
            checkpointObject.transform.position = position;

            BoxCollider collider = checkpointObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            CheckpointRuntimeAnchor anchor = checkpointObject.AddComponent<CheckpointRuntimeAnchor>();
            definition = ScriptableObject.CreateInstance<CheckpointDefinitionSO>();

            SetPrivateField(definition, "checkpointId", checkpointId);
            SetPrivateField(definition, "respawnOffset", Vector3.zero);
            SetPrivateField(definition, "restoreFullHealth", true);
            SetPrivateField(definition, "restoreFullMana", true);
            SetPrivateField(anchor, "definition", definition);
            SetPrivateField(anchor, "coordinator", coordinator);

            checkpointObject.SetActive(true);
            coordinator.RegisterCheckpoint(anchor);
            return anchor;
        }

        private static EnemyBrain CreateEnemy(out GameObject enemyObject, out EnemyArchetypeSO archetype)
        {
            enemyObject = new GameObject("Enemy");
            enemyObject.SetActive(false);
            enemyObject.AddComponent<HealthComponent>();
            enemyObject.AddComponent<EnemyStateMachine>();
            enemyObject.AddComponent<EnemySensing>();
            enemyObject.AddComponent<EnemyAttackController>();
            enemyObject.AddComponent<DamageableReceiver>();
            EnemyBrain enemy = enemyObject.AddComponent<EnemyBrain>();

            archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            SetPrivateField(archetype, "maxHealth", 60f);
            SetPrivateField(enemy, "archetype", archetype);

            enemyObject.SetActive(true);
            return enemy;
        }

        private static void Cleanup(ScriptableObject definition, params UnityEngine.Object[] objects)
        {
            if (definition != null)
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
