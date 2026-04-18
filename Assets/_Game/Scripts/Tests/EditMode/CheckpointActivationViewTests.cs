using System.Reflection;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class CheckpointActivationViewTests
    {
        [Test]
        public void CheckpointActivationPlanner_BuildsReadableChapter01Messages()
        {
            CheckpointActivationPlan startPlan = CheckpointActivationPlanner.Build(Chapter01Ids.Checkpoints.Start);
            Assert.AreEqual("Checkpoint Activated", startPlan.Title);
            Assert.AreEqual("Respawn updated to the chapter entrance.", startPlan.Body);

            CheckpointActivationPlan courtyardPlan = CheckpointActivationPlanner.Build(Chapter01Ids.Checkpoints.Courtyard);
            Assert.AreEqual("Courtyard Secured", courtyardPlan.Title);

            CheckpointActivationPlan interiorPlan = CheckpointActivationPlanner.Build(Chapter01Ids.Checkpoints.Interior);
            Assert.AreEqual("Interior Secured", interiorPlan.Title);
        }

        [Test]
        public void CheckpointActivationView_ShowsWhenCheckpointServiceFires()
        {
            GameObject flowObject = new GameObject("ChapterFlow");

            try
            {
                CheckpointService checkpointService = flowObject.AddComponent<CheckpointService>();
                CheckpointActivationView view = flowObject.AddComponent<CheckpointActivationView>();
                SetPrivateField(view, "visibleDurationSeconds", 0.4f);

                InvokeMethod(view, "Awake");
                InvokeMethod(view, "OnEnable");

                Assert.IsFalse(view.IsVisible);

                checkpointService.ActivateCheckpoint(Chapter01Ids.Checkpoints.Courtyard);
                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("Courtyard Secured", view.CurrentTitle);
                Assert.AreEqual("Respawn moved forward to the outdoor courtyard.", view.CurrentBody);

                InvokeMethod(view, "Update");
                SetPrivateField(view, "visibleTimer", 0.05f);
                InvokeMethod(view, "Update");
            }
            finally
            {
                Object.DestroyImmediate(flowObject);
            }
        }

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
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
