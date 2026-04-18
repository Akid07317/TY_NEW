using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class ChapterSaveDataTests
    {
        [Test]
        public void ChapterSaveData_RoundTripsThroughJson()
        {
            ChapterSaveData source = new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                checkpointId = Chapter01Ids.Checkpoints.Courtyard,
                currentAreaId = Chapter01Ids.Areas.Interior,
                visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance, Chapter01Ids.Areas.Courtyard, Chapter01Ids.Areas.Interior },
                keyItemIds = new[] { Chapter01Ids.KeyItems.GateSigil },
                clearedEncounterIds = new[] { Chapter01Ids.Encounters.EntranceTutorial, Chapter01Ids.Encounters.Courtyard },
                chapterCompleted = true,
                playerHealth = 76f,
                playerMana = 44f
            };

            string json = JsonUtility.ToJson(source);
            ChapterSaveData restored = JsonUtility.FromJson<ChapterSaveData>(json);

            Assert.NotNull(restored);
            Assert.AreEqual(Chapter01Ids.Chapter, restored.chapterId);
            Assert.AreEqual(Chapter01Ids.Checkpoints.Courtyard, restored.checkpointId);
            Assert.AreEqual(Chapter01Ids.Areas.Interior, restored.currentAreaId);
            Assert.AreEqual(3, restored.visitedAreaIds.Length);
            Assert.AreEqual(1, restored.keyItemIds.Length);
            Assert.AreEqual(2, restored.clearedEncounterIds.Length);
            Assert.IsTrue(restored.chapterCompleted);
            Assert.AreEqual(76f, restored.playerHealth);
            Assert.AreEqual(44f, restored.playerMana);
        }
    }
}
