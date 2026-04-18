using System.Reflection;
using CampusRPG.Interaction;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class TriggerVolumeTests
    {
        [Test]
        public void TriggerVolume_DisablesAfterRestoreWhenAlreadyConsumed()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject triggerObject = new GameObject("Trigger");

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

                TriggerVolume trigger = triggerObject.AddComponent<TriggerVolume>();
                SetPrivateField(trigger, "action", TriggerVolumeAction.EnterArea);
                SetPrivateField(trigger, "payloadId", Chapter01Ids.Areas.Courtyard);
                SetPrivateField(trigger, "oneShot", true);
                SetPrivateField(trigger, "chapterProgressService", progressService);

                InvokePrivateMethod(trigger, "Awake");
                InvokePrivateMethod(trigger, "OnEnable");

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    currentAreaId = Chapter01Ids.Areas.Courtyard,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance, Chapter01Ids.Areas.Courtyard }
                });

                Assert.IsFalse(triggerObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(triggerObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void TriggerVolume_ReenablesAfterCheckpointRestoreRemovesConsumedState()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject triggerObject = new GameObject("Trigger");

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

                TriggerVolume trigger = triggerObject.AddComponent<TriggerVolume>();
                SetPrivateField(trigger, "action", TriggerVolumeAction.EnterArea);
                SetPrivateField(trigger, "payloadId", Chapter01Ids.Areas.Courtyard);
                SetPrivateField(trigger, "oneShot", true);
                SetPrivateField(trigger, "chapterProgressService", progressService);

                InvokePrivateMethod(trigger, "Awake");
                InvokePrivateMethod(trigger, "OnEnable");

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    currentAreaId = Chapter01Ids.Areas.Courtyard,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance, Chapter01Ids.Areas.Courtyard }
                });

                Assert.IsFalse(triggerObject.activeSelf);

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    currentAreaId = Chapter01Ids.Areas.Entrance,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance }
                });
                trigger.ResetForCheckpointRestore();

                Assert.IsTrue(triggerObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(triggerObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
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
