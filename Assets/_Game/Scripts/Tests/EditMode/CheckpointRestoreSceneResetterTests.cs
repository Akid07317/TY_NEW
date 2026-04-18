using System.Reflection;
using System.Collections.Generic;
using CampusRPG.Interaction;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CheckpointRestoreSceneResetterTests
    {
        [Test]
        public void ResetInteractions_ReactivatesRegisteredInactivePickupAndTrigger()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject pickupObject = new GameObject("Pickup");
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

                KeyItemPickup pickup = pickupObject.AddComponent<KeyItemPickup>();
                SetPrivateField(pickup, "keyItemId", Chapter01Ids.KeyItems.GateSigil);
                SetPrivateField(pickup, "chapterProgressService", progressService);
                InvokePrivateMethod(pickup, "Awake");
                InvokePrivateMethod(pickup, "OnEnable");

                TriggerVolume trigger = triggerObject.AddComponent<TriggerVolume>();
                SetPrivateField(trigger, "action", TriggerVolumeAction.EnterArea);
                SetPrivateField(trigger, "payloadId", Chapter01Ids.Areas.Courtyard);
                SetPrivateField(trigger, "oneShot", true);
                SetPrivateField(trigger, "chapterProgressService", progressService);
                InvokePrivateMethod(trigger, "Awake");
                InvokePrivateMethod(trigger, "OnEnable");

                progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil);
                progressService.EnterArea(Chapter01Ids.Areas.Courtyard);

                Assert.IsFalse(pickupObject.activeSelf);
                Assert.IsFalse(triggerObject.activeSelf);

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    currentAreaId = Chapter01Ids.Areas.Entrance,
                    visitedAreaIds = new[] { Chapter01Ids.Areas.Entrance }
                });

                Assert.IsFalse(pickupObject.activeSelf);
                Assert.IsFalse(triggerObject.activeSelf);

                CheckpointRestoreSceneResetter.ResetInteractions();

                Assert.IsTrue(pickupObject.activeSelf);
                Assert.IsTrue(triggerObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(triggerObject);
                Object.DestroyImmediate(pickupObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void ResetInteractions_UsesRestorePriorityBeforeRegistrationOrder()
        {
            GameObject root = new GameObject("CheckpointRestoreParticipants");
            List<string> resetOrder = new List<string>();

            try
            {
                DummyCheckpointRestoreParticipant late = root.AddComponent<DummyCheckpointRestoreParticipant>();
                late.Initialize(CheckpointRestoreGroup.Interaction, 100, "late", resetOrder);
                CheckpointRestoreSceneResetter.RegisterParticipant(late);

                DummyCheckpointRestoreParticipant early = root.AddComponent<DummyCheckpointRestoreParticipant>();
                early.Initialize(CheckpointRestoreGroup.Interaction, 0, "early", resetOrder);
                CheckpointRestoreSceneResetter.RegisterParticipant(early);

                DummyCheckpointRestoreParticipant middle = root.AddComponent<DummyCheckpointRestoreParticipant>();
                middle.Initialize(CheckpointRestoreGroup.Interaction, 0, "middle", resetOrder);
                CheckpointRestoreSceneResetter.RegisterParticipant(middle);

                CheckpointRestoreSceneResetter.ResetInteractions();

                CollectionAssert.AreEqual(new[] { "early", "middle", "late" }, resetOrder);
            }
            finally
            {
                DummyCheckpointRestoreParticipant[] participants = root.GetComponents<DummyCheckpointRestoreParticipant>();

                for (int i = 0; i < participants.Length; i++)
                {
                    CheckpointRestoreSceneResetter.UnregisterParticipant(participants[i]);
                }

                Object.DestroyImmediate(root);
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

        private sealed class DummyCheckpointRestoreParticipant : MonoBehaviour, ICheckpointRestoreParticipant
        {
            private List<string> resetOrder;
            private string label;

            public CheckpointRestoreGroup RestoreGroup { get; private set; }

            public int RestorePriority { get; private set; }

            public void Initialize(
                CheckpointRestoreGroup restoreGroup,
                int restorePriority,
                string participantLabel,
                List<string> participantResetOrder)
            {
                RestoreGroup = restoreGroup;
                RestorePriority = restorePriority;
                label = participantLabel;
                resetOrder = participantResetOrder;
            }

            public void ResetForCheckpointRestore()
            {
                resetOrder.Add(label);
            }
        }
    }
}
