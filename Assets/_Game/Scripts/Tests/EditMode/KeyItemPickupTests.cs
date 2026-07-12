using System.Reflection;
using CampusRPG.Character;
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

        [Test]
        public void KeyItemPickup_RejectsGateSigilUntilInteriorEncounterIsCleared()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject playerObject = new GameObject("Player");

            try
            {
                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                KeyItemPickup pickup = pickupObject.AddComponent<KeyItemPickup>();
                SetPrivateField(pickup, "keyItemId", Chapter01Ids.KeyItems.GateSigil);
                SetPrivateField(pickup, "requiredEncounterId", Chapter01Ids.Encounters.Interior);
                SetPrivateField(pickup, "chapterProgressService", progressService);
                InvokePrivateMethod(pickup, "Awake");
                InvokePrivateMethod(pickup, "OnEnable");

                playerObject.AddComponent<PlayerCharacter>();
                Collider playerCollider = playerObject.AddComponent<CapsuleCollider>();

                Assert.IsFalse(pickup.TryCollect(playerCollider));
                Assert.IsFalse(progressService.HasKeyItem(Chapter01Ids.KeyItems.GateSigil));
                Assert.IsTrue(pickupObject.activeSelf);

                Assert.IsTrue(progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Interior));
                Assert.IsTrue(pickup.TryCollect(playerCollider));
                Assert.IsTrue(progressService.HasKeyItem(Chapter01Ids.KeyItems.GateSigil));
                Assert.IsFalse(pickupObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(pickupObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        [Test]
        public void KeyItemPickup_RejectsChapterCompletionUntilGatekeeperIsCleared()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject playerObject = new GameObject("Player");

            try
            {
                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                KeyItemPickup pickup = pickupObject.AddComponent<KeyItemPickup>();
                SetPrivateField(pickup, "keyItemId", Chapter01Ids.KeyItems.RitualCore);
                SetPrivateField(pickup, "requiredEncounterId", Chapter01Ids.Encounters.Gatekeeper);
                SetPrivateField(pickup, "completeChapterOnPickup", true);
                SetPrivateField(pickup, "chapterProgressService", progressService);
                InvokePrivateMethod(pickup, "Awake");
                InvokePrivateMethod(pickup, "OnEnable");

                playerObject.AddComponent<PlayerCharacter>();
                Collider playerCollider = playerObject.AddComponent<CapsuleCollider>();

                Assert.IsFalse(pickup.TryCollect(playerCollider));
                Assert.IsFalse(progressService.HasKeyItem(Chapter01Ids.KeyItems.RitualCore));
                Assert.IsFalse(progressService.IsChapterCompleted);
                Assert.IsTrue(pickupObject.activeSelf);

                Assert.IsTrue(progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Gatekeeper));
                Assert.IsTrue(pickup.TryCollect(playerCollider));
                Assert.IsTrue(progressService.HasKeyItem(Chapter01Ids.KeyItems.RitualCore));
                Assert.IsTrue(progressService.IsChapterCompleted);
                Assert.IsFalse(pickupObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
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
