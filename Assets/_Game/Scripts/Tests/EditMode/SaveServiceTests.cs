using System.IO;
using System.Text.RegularExpressions;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CampusRPG.Tests
{
    public sealed class SaveServiceTests
    {
        [Test]
        public void SaveService_SaveAndLoad_RoundTripsChapterSaveData()
        {
            string fileName = "save_service_roundtrip_" + System.Guid.NewGuid().ToString("N") + ".json";
            GameObject gameObject = new GameObject("SaveService");

            try
            {
                SaveService service = gameObject.AddComponent<SaveService>();
                SetPrivateField(service, "fileName", fileName);

                ChapterSaveData source = new ChapterSaveData
                {
                    chapterId = Chapter01Ids.Chapter,
                    checkpointId = Chapter01Ids.Checkpoints.Courtyard,
                    currentAreaId = Chapter01Ids.Areas.Interior,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance, Chapter01Ids.Areas.Courtyard },
                    keyItemIds = new[] { Chapter01Ids.KeyItems.GateSigil },
                    clearedEncounterIds = new[] { Chapter01Ids.Encounters.EntranceTutorial },
                    chapterCompleted = true,
                    playerHealth = 64f,
                    playerMana = 21f
                };

                service.Save(source);

                Assert.IsTrue(service.TryLoad(out ChapterSaveData loaded));
                Assert.NotNull(loaded);
                Assert.AreEqual(source.chapterId, loaded.chapterId);
                Assert.AreEqual(source.checkpointId, loaded.checkpointId);
                Assert.AreEqual(source.currentAreaId, loaded.currentAreaId);
                Assert.AreEqual(source.chapterCompleted, loaded.chapterCompleted);
                Assert.AreEqual(source.playerHealth, loaded.playerHealth, 0.001f);
                Assert.AreEqual(source.playerMana, loaded.playerMana, 0.001f);
                Assert.AreEqual(2, loaded.visitedAreaIds.Length);
                Assert.AreEqual(1, loaded.keyItemIds.Length);
                Assert.AreEqual(1, loaded.clearedEncounterIds.Length);
            }
            finally
            {
                CleanupSaveFile(fileName);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SaveService_TryLoad_ReturnsFalseForMalformedJson()
        {
            string fileName = "save_service_invalid_" + System.Guid.NewGuid().ToString("N") + ".json";
            GameObject gameObject = new GameObject("SaveService");

            try
            {
                SaveService service = gameObject.AddComponent<SaveService>();
                SetPrivateField(service, "fileName", fileName);
                EnsureSaveDirectory(service.FullPath);
                File.WriteAllText(service.FullPath, "{ this is not valid json");

                LogAssert.Expect(LogType.Warning, new Regex("SaveService failed to load chapter data"));
                Assert.IsFalse(service.TryLoad(out ChapterSaveData loaded));
                Assert.IsNull(loaded);
            }
            finally
            {
                CleanupSaveFile(fileName);
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            System.Reflection.FieldInfo field = instance.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void EnsureSaveDirectory(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void CleanupSaveFile(string fileName)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, "Save", fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
