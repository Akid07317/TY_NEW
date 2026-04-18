using System.Reflection;
using CampusRPG.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusRPG.Tests
{
    public sealed class InputReaderLifecycleTests
    {
        [Test]
        public void InputReader_Reenable_DoesNotAccumulatePerformedCallbacks()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = new InputActionMap("Player");
            map.AddAction("LightAttack", InputActionType.Button);
            asset.AddActionMap(map);

            GameObject gameObject = new GameObject("InputReader");
            gameObject.SetActive(false);

            try
            {
                InputReader reader = gameObject.AddComponent<InputReader>();
                SetPrivateField(reader, "actionsAsset", asset);

                reader.Initialize();
                InputAction runtimeAction = GetPrivateField<InputAction>(reader, "lightAttackAction");
                Assert.NotNull(runtimeAction);
                Assert.AreEqual(0, GetPerformedCallbackCount(runtimeAction));
                Assert.AreEqual(0, GetPerformedCallbackCount(asset.FindAction("Player/LightAttack", false)));

                InvokePrivateMethod(reader, "OnEnable");
                Assert.AreEqual(1, GetPerformedCallbackCount(runtimeAction));

                InvokePrivateMethod(reader, "OnDisable");
                Assert.AreEqual(0, GetPerformedCallbackCount(runtimeAction));

                InvokePrivateMethod(reader, "OnEnable");
                Assert.AreSame(runtimeAction, GetPrivateField<InputAction>(reader, "lightAttackAction"));
                Assert.AreEqual(1, GetPerformedCallbackCount(runtimeAction));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(asset);
            }
        }

        private static int GetPerformedCallbackCount(InputAction action)
        {
            FieldInfo performedField = typeof(InputAction).GetField("m_OnPerformed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(performedField);
            object callbackArray = performedField.GetValue(action);
            PropertyInfo lengthProperty = callbackArray.GetType().GetProperty("length", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(lengthProperty);
            return (int)lengthProperty.GetValue(callbackArray);
        }

        private static TValue GetPrivateField<TValue>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TValue)field.GetValue(instance);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
        }
    }
}
