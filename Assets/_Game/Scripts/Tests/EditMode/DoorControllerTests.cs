using System.Reflection;
using CampusRPG.Interaction;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class DoorControllerTests
    {
        [Test]
        public void DoorController_OpensWhenRequiredKeyItemIsRegistered()
        {
            ChapterProgressionSO progression = ScriptableObject.CreateInstance<ChapterProgressionSO>();
            GameObject progressObject = new GameObject("ChapterProgress");
            GameObject doorObject = new GameObject("Door");
            GameObject blockerObject = new GameObject("Blocker");

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

                DoorController doorController = doorObject.AddComponent<DoorController>();
                SetPrivateField(doorController, "chapterProgressService", progressService);
                SetPrivateField(doorController, "requiredKeyItemId", Chapter01Ids.KeyItems.GateSigil);
                SetPrivateField(doorController, "blockersToDisableWhenOpen", new[] { blockerObject });

                InvokePrivateMethod(doorController, "Awake");
                InvokePrivateMethod(doorController, "OnEnable");

                Assert.IsTrue(blockerObject.activeSelf);

                progressService.RegisterKeyItem(Chapter01Ids.KeyItems.GateSigil);

                Assert.IsFalse(blockerObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(blockerObject);
                Object.DestroyImmediate(doorObject);
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
