using System.Reflection;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class KeyItemBeaconViewTests
    {
        [Test]
        public void KeyItemBeaconView_ShowsOnlyAfterRequiredEncounterAndHidesAfterPickup()
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
                        new ChapterAreaProgressionEntry(Chapter01Ids.Areas.Boss, "Boss Arena")
                    });

                ChapterProgressService progressService = progressObject.AddComponent<ChapterProgressService>();
                SetPrivateField(progressService, "progression", progression);
                InvokePrivateMethod(progressService, "Awake");

                KeyItemPickup pickup = pickupObject.AddComponent<KeyItemPickup>();
                SetPrivateField(pickup, "keyItemId", Chapter01Ids.KeyItems.RitualCore);
                SetPrivateField(pickup, "completeChapterOnPickup", true);
                SetPrivateField(pickup, "chapterProgressService", progressService);
                InvokePrivateMethod(pickup, "Awake");
                InvokePrivateMethod(pickup, "OnEnable");

                KeyItemBeaconView view = pickupObject.AddComponent<KeyItemBeaconView>();
                SetPrivateField(view, "chapterProgressService", progressService);
                SetPrivateField(view, "requiredEncounterId", Chapter01Ids.Encounters.Gatekeeper);
                InvokePrivateMethod(view, "Awake");

                InvokePrivateMethod(view, "Tick", 0f);
                Assert.IsFalse(view.IsVisible);

                progressService.MarkEncounterCleared(Chapter01Ids.Encounters.Gatekeeper);
                InvokePrivateMethod(view, "Tick", 0f);
                Assert.IsTrue(view.IsVisible);
                Assert.AreNotEqual(Vector3.zero, view.CurrentWorldPosition);
                Assert.IsTrue(view.IsRevealPulseVisible);
                Assert.AreNotEqual(Vector3.zero, view.CurrentRevealPulseBasePosition);

                InvokePrivateMethod(view, "Tick", 1.2f);
                Assert.IsTrue(view.IsVisible);
                Assert.IsFalse(view.IsRevealPulseVisible);

                progressService.CompleteChapter(Chapter01Ids.KeyItems.RitualCore);
                InvokePrivateMethod(view, "Tick", 0f);
                Assert.IsFalse(view.IsVisible);
                Assert.IsFalse(view.IsRevealPulseVisible);
            }
            finally
            {
                Object.DestroyImmediate(pickupObject);
                Object.DestroyImmediate(progressObject);
                Object.DestroyImmediate(progression);
            }
        }

        private static void InvokePrivateMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
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
