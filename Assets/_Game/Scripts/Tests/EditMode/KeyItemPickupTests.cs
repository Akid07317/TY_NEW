using System.Reflection;
using CampusRPG.Interaction;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class KeyItemPickupTests
    {
        [Test]
        public void KeyItemPickup_DisablesAfterRestoreWhenKeyAlreadyOwned()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            try
            {
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance")
                    });

                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                KeyItemPickup pickup = pickupObject.AddComponent<KeyItemPickup>();
                SetPrivateField(pickup, "keyItemId", Chapter01Ids.KeyItems.GateSigil);
                SetPrivateField(pickup, "chapterProgressService", progressService);

                InvokePrivateMethod(pickup, "Awake");
                InvokePrivateMethod(pickup, "OnEnable");

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    keyItemIds = new[] { Chapter01Ids.KeyItems.GateSigil }
                });

                Assert.IsFalse(pickupObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(pickupObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void KeyItemPickup_ReenablesAfterCheckpointRestoreRemovesOwnedKey()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            try
            {
                SetPrivateField(
                    progression,
                    "areas",
                    new[]
                    {
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Entrance, "Entrance")
                    });

                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                KeyItemPickup pickup = pickupObject.AddComponent<KeyItemPickup>();
                SetPrivateField(pickup, "keyItemId", Chapter01Ids.KeyItems.GateSigil);
                SetPrivateField(pickup, "chapterProgressService", progressService);

                InvokePrivateMethod(pickup, "Awake");
                InvokePrivateMethod(pickup, "OnEnable");

                progressService.RestoreFromSave(new ChapterSaveData
                {
                    keyItemIds = new[] { Chapter01Ids.KeyItems.GateSigil }
                });

                Assert.IsFalse(pickupObject.activeSelf);

                progressService.RestoreFromSave(new ChapterSaveData());
                pickup.ResetForCheckpointRestore();

                Assert.IsTrue(pickupObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(pickupObject);
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
