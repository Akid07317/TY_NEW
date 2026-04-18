using System;
using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Input;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class PlayerStateMachineLifecycleTests
    {
        [Test]
        public void PlayerStateMachine_Reenable_RestoresInputAndDeathSubscriptionsWithoutDuplication()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                InputReader inputReader = gameObject.AddComponent<InputReader>();
                HealthComponent health = gameObject.AddComponent<HealthComponent>();

                SetPrivateField(player, "inputReader", inputReader);
                SetPrivateField(player, "health", health);

                stateMachine.Initialize(player);

                Assert.AreEqual(1, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(1, GetEventSubscriberCount(health, "Died"));

                InvokePrivateMethod(stateMachine, "OnDisable");

                Assert.AreEqual(0, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(0, GetEventSubscriberCount(health, "Died"));

                InvokePrivateMethod(stateMachine, "OnEnable");

                Assert.AreEqual(1, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(1, GetEventSubscriberCount(health, "Died"));

                InvokePrivateMethod(stateMachine, "OnEnable");

                Assert.AreEqual(1, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(1, GetEventSubscriberCount(health, "Died"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static int GetEventSubscriberCount(object instance, string eventFieldName)
        {
            FieldInfo field = instance.GetType().GetField(eventFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, eventFieldName);
            MulticastDelegate multicastDelegate = field.GetValue(instance) as MulticastDelegate;
            return multicastDelegate?.GetInvocationList().Length ?? 0;
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
