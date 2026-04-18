using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CheckpointRestorePlannerTests
    {
        [Test]
        public void CreateSnapshot_UsesCheckpointOverridesAndFullRestoreFlags()
        {
            PlayerCharacter player = null;
            GameObject playerObject = null;
            GameObject checkpointObject = null;
            CheckpointDefinitionSO checkpointDefinition = null;

            try
            {
                player = CreatePlayer(out playerObject);
                checkpointObject = new GameObject("Checkpoint");
                checkpointObject.transform.position = new Vector3(10f, 0f, -4f);
                checkpointObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                CheckpointRuntimeAnchor anchor = checkpointObject.AddComponent<CheckpointRuntimeAnchor>();
                checkpointDefinition = ScriptableObject.CreateInstance<CheckpointDefinitionSO>();

                SetPrivateField(checkpointDefinition, "checkpointId", "CP_BOSS");
                SetPrivateField(checkpointDefinition, "respawnOffset", new Vector3(1f, 0f, 2f));
                SetPrivateField(checkpointDefinition, "restoreFullHealth", true);
                SetPrivateField(checkpointDefinition, "restoreFullMana", true);
                SetPrivateField(anchor, "definition", checkpointDefinition);

                player.Health.SetMax(150f, true);
                player.Mana.SetMax(80f, true);
                player.Health.SetCurrent(30f);
                player.Mana.SetCurrent(12f);

                CheckpointRestoreSnapshot snapshot = CheckpointRestorePlanner.CreateSnapshot(
                    player,
                    new ChapterSaveData
                    {
                        checkpointId = "CP_BOSS",
                        playerHealth = 20f,
                        playerMana = 10f
                    },
                    anchor,
                    string.Empty,
                    "CP_START",
                    new Vector3(-2f, 0f, 3f),
                    Quaternion.identity,
                    new Vector3(4f, 0f, 5f),
                    false,
                    false);

                Assert.AreEqual("CP_BOSS", snapshot.CheckpointId);
                Assert.AreEqual(new Vector3(11f, 0f, -2f), snapshot.RespawnPosition);
                Assert.That(
                    Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), snapshot.RespawnRotation),
                    Is.LessThan(0.01f));
                Assert.AreEqual(150f, snapshot.HealthValue, 0.01f);
                Assert.AreEqual(80f, snapshot.ManaValue, 0.01f);
            }
            finally
            {
                if (checkpointDefinition != null)
                {
                    Object.DestroyImmediate(checkpointDefinition);
                }

                if (checkpointObject != null)
                {
                    Object.DestroyImmediate(checkpointObject);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void CreateSnapshot_FallsBackToDefaultsAndSavedValues()
        {
            PlayerCharacter player = null;
            GameObject playerObject = null;

            try
            {
                player = CreatePlayer(out playerObject);
                player.Health.SetMax(120f, true);
                player.Mana.SetMax(70f, true);

                CheckpointRestoreSnapshot snapshot = CheckpointRestorePlanner.CreateSnapshot(
                    player,
                    new ChapterSaveData
                    {
                        playerHealth = 25f,
                        playerMana = 15f
                    },
                    null,
                    string.Empty,
                    "CP_DEFAULT",
                    new Vector3(3f, 0f, 6f),
                    Quaternion.identity,
                    new Vector3(1f, 0f, -2f),
                    false,
                    false);

                Assert.AreEqual("CP_DEFAULT", snapshot.CheckpointId);
                Assert.AreEqual(new Vector3(4f, 0f, 4f), snapshot.RespawnPosition);
                Assert.AreEqual(25f, snapshot.HealthValue, 0.01f);
                Assert.AreEqual(15f, snapshot.ManaValue, 0.01f);
            }
            finally
            {
                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void ResolveCheckpointId_PrefersSaveThenCurrentThenDefault()
        {
            Assert.AreEqual(
                "CP_SAVE",
                CheckpointRestorePlanner.ResolveCheckpointId(
                    new ChapterSaveData { checkpointId = "CP_SAVE" },
                    "CP_CURRENT",
                    "CP_DEFAULT"));
            Assert.AreEqual(
                "CP_CURRENT",
                CheckpointRestorePlanner.ResolveCheckpointId(
                    new ChapterSaveData(),
                    "CP_CURRENT",
                    "CP_DEFAULT"));
            Assert.AreEqual(
                "CP_DEFAULT",
                CheckpointRestorePlanner.ResolveCheckpointId(
                    null,
                    string.Empty,
                    "CP_DEFAULT"));
        }

        private static PlayerCharacter CreatePlayer(out GameObject playerObject)
        {
            playerObject = new GameObject("Player");
            playerObject.AddComponent<DamageableReceiver>();
            HealthComponent health = playerObject.AddComponent<HealthComponent>();
            ManaComponent mana = playerObject.AddComponent<ManaComponent>();
            PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
            SetPrivateField(player, "health", health);
            SetPrivateField(player, "mana", mana);
            return player;
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
