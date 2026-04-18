using System.Reflection;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class CheckpointRestoreCoordinatorUtilityTests
    {
        [Test]
        public void ResolveActivationMode_UsesRefreshForMissingOrCurrentCheckpoint()
        {
            GameObject flowObject = new GameObject("Flow");
            GameObject checkpointObject = new GameObject("Checkpoint");
            CheckpointDefinitionSO definition = ScriptableObject.CreateInstance<CheckpointDefinitionSO>();

            try
            {
                CheckpointService checkpointService = flowObject.AddComponent<CheckpointService>();
                CheckpointRuntimeAnchor checkpoint = checkpointObject.AddComponent<CheckpointRuntimeAnchor>();

                SetPrivateField(definition, "checkpointId", "CP01");
                SetPrivateField(checkpoint, "definition", definition);
                SetPrivateField(checkpointService, "currentCheckpointId", "CP01");

                Assert.AreEqual(
                    CheckpointActivationMode.RefreshAndSave,
                    CheckpointRestoreCoordinatorUtility.ResolveActivationMode(null, checkpoint));
                Assert.AreEqual(
                    CheckpointActivationMode.RefreshAndSave,
                    CheckpointRestoreCoordinatorUtility.ResolveActivationMode(checkpointService, checkpoint));

                SetPrivateField(checkpointService, "currentCheckpointId", "CP02");

                Assert.AreEqual(
                    CheckpointActivationMode.ActivateService,
                    CheckpointRestoreCoordinatorUtility.ResolveActivationMode(checkpointService, checkpoint));
                Assert.AreEqual(
                    CheckpointActivationMode.None,
                    CheckpointRestoreCoordinatorUtility.ResolveActivationMode(checkpointService, null));
            }
            finally
            {
                Object.DestroyImmediate(flowObject);
                Object.DestroyImmediate(checkpointObject);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void MatchesChapter_AllowsBlankAndExactChapterIds()
        {
            Assert.IsFalse(CheckpointRestoreCoordinatorUtility.MatchesChapter("Chapter01", null));
            Assert.IsTrue(CheckpointRestoreCoordinatorUtility.MatchesChapter(
                "Chapter01",
                new ChapterSaveData { chapterId = string.Empty }));
            Assert.IsTrue(CheckpointRestoreCoordinatorUtility.MatchesChapter(
                "Chapter01",
                new ChapterSaveData { chapterId = "Chapter01" }));
            Assert.IsFalse(CheckpointRestoreCoordinatorUtility.MatchesChapter(
                "Chapter01",
                new ChapterSaveData { chapterId = "OtherChapter" }));
        }

        [Test]
        public void BuildRestoreSnapshot_UsesRegisteredCheckpointData()
        {
            GameObject checkpointObject = new GameObject("Checkpoint");
            CheckpointDefinitionSO definition = ScriptableObject.CreateInstance<CheckpointDefinitionSO>();

            try
            {
                checkpointObject.transform.position = new Vector3(6f, 0f, 3f);
                checkpointObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

                CheckpointRuntimeAnchor checkpoint = checkpointObject.AddComponent<CheckpointRuntimeAnchor>();
                CheckpointRuntimeRegistry checkpointRegistry = new CheckpointRuntimeRegistry();

                SetPrivateField(definition, "checkpointId", "CP01");
                SetPrivateField(definition, "respawnOffset", new Vector3(0f, 0f, 2f));
                SetPrivateField(definition, "restoreFullHealth", true);
                SetPrivateField(definition, "restoreFullMana", true);
                SetPrivateField(checkpoint, "definition", definition);

                checkpointRegistry.Register(checkpoint);

                CheckpointRestoreSnapshot snapshot = CheckpointRestoreCoordinatorUtility.BuildRestoreSnapshot(
                    null,
                    new ChapterSaveData
                    {
                        checkpointId = "CP01",
                        playerHealth = 35f,
                        playerMana = 20f
                    },
                    checkpointRegistry,
                    string.Empty,
                    "Fallback",
                    new Vector3(1f, 0f, 1f),
                    Quaternion.identity,
                    new Vector3(0f, 0f, 1f),
                    false,
                    false);

                Assert.AreEqual("CP01", snapshot.CheckpointId);
                Assert.AreEqual(checkpoint.RespawnPosition, snapshot.RespawnPosition);
                Assert.AreEqual(checkpoint.RespawnRotation, snapshot.RespawnRotation);
                Assert.AreEqual(100f, snapshot.HealthValue, 0.01f);
                Assert.AreEqual(100f, snapshot.ManaValue, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(checkpointObject);
                Object.DestroyImmediate(definition);
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
