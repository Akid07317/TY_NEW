using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ChapterResumeContextViewTests
    {
        [Test]
        public void ResumePlanner_WithoutSave_IsHidden()
        {
            ChapterResumeContextPlan plan = ChapterResumeContextPlanner.Build(null);

            Assert.IsFalse(plan.IsVisible);
            Assert.AreEqual(string.Empty, plan.Title);
            Assert.AreEqual(string.Empty, plan.Body);
        }

        [Test]
        public void ResumePlanner_WithCourtyardSave_ShowsResumeHint()
        {
            ChapterSaveData saveData = new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                checkpointId = Chapter01Ids.Checkpoints.Courtyard,
                currentAreaId = Chapter01Ids.Areas.Courtyard
            };

            ChapterResumeContextPlan plan = ChapterResumeContextPlanner.Build(saveData);

            Assert.IsTrue(plan.IsVisible);
            Assert.AreEqual("Resume: CP02 / Outdoor Courtyard", plan.Title);
            Assert.AreEqual("Mixed enemies are still ahead. Clear the courtyard, then push into the school interior.", plan.Body);
        }

        [Test]
        public void ResumePlanner_WithGateSigilSave_ShowsBossRouteHint()
        {
            ChapterSaveData saveData = new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                checkpointId = Chapter01Ids.Checkpoints.Interior,
                currentAreaId = Chapter01Ids.Areas.Interior,
                keyItemIds = new[] { Chapter01Ids.KeyItems.GateSigil }
            };

            ChapterResumeContextPlan plan = ChapterResumeContextPlanner.Build(saveData);

            Assert.IsTrue(plan.IsVisible);
            Assert.AreEqual("Resume: CP03 / School Interior", plan.Title);
            Assert.AreEqual("The boss route is open. Push through the gate and challenge the Campus Gatekeeper.", plan.Body);
        }

        [Test]
        public void ResumePlanner_WithCompletedSave_ShowsEndingReminder()
        {
            ChapterSaveData saveData = new ChapterSaveData
            {
                chapterId = Chapter01Ids.Chapter,
                chapterCompleted = true,
                checkpointId = Chapter01Ids.Checkpoints.Interior,
                currentAreaId = Chapter01Ids.Areas.Boss
            };

            ChapterResumeContextPlan plan = ChapterResumeContextPlanner.Build(saveData);

            Assert.IsTrue(plan.IsVisible);
            Assert.AreEqual("Resume: Chapter Complete", plan.Title);
            Assert.AreEqual("The Ritual Core is already secured. Walk forward to review the ending card.", plan.Body);
        }
    }
}
