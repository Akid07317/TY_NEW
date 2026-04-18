using System.Reflection;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class AttackExecutorHitboxTests
    {
        [Test]
        public void Execute_UsesConfiguredBoxHitbox()
        {
            GameObject attacker = new GameObject("Attacker");
            AttackExecutor executor = attacker.AddComponent<AttackExecutor>();
            GameObject targetInside = CreateTarget("Inside", new Vector3(0f, 0f, 1.1f));
            GameObject targetOutside = CreateTarget("Outside", new Vector3(1.5f, 0f, 1.1f));
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(executor, "attackOrigin", attacker.transform);
                SetPrivateField(attack, "damageMultiplier", 1f);
                SetPrivateField(attack, "hitboxShape", AttackHitboxShape.Box);
                SetPrivateField(attack, "hitboxLocalCenter", new Vector3(0f, 0f, 1.1f));
                SetPrivateField(attack, "hitboxHalfExtents", new Vector3(0.45f, 0.45f, 0.6f));

                int hitCount = executor.Execute(attack, 15f, attacker);

                Assert.AreEqual(1, hitCount);
                Assert.AreEqual(15f, targetInside.GetComponent<TestDamageable>().TotalDamageReceived);
                Assert.AreEqual(0f, targetOutside.GetComponent<TestDamageable>().TotalDamageReceived);
            }
            finally
            {
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(targetInside);
                Object.DestroyImmediate(targetOutside);
                Object.DestroyImmediate(attacker);
            }
        }

        [Test]
        public void Execute_LegacyForwardSphereRemainsCompatible()
        {
            GameObject attacker = new GameObject("Attacker");
            AttackExecutor executor = attacker.AddComponent<AttackExecutor>();
            GameObject target = CreateTarget("Target", new Vector3(0f, 0f, 1.5f));
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(executor, "attackOrigin", attacker.transform);
                SetPrivateField(attack, "damageMultiplier", 1f);
                SetPrivateField(attack, "range", 1.5f);
                SetPrivateField(attack, "radius", 0.5f);
                SetPrivateField(attack, "hitboxShape", AttackHitboxShape.LegacyForwardSphere);

                int hitCount = executor.Execute(attack, 9f, attacker);

                Assert.AreEqual(1, hitCount);
                Assert.AreEqual(9f, target.GetComponent<TestDamageable>().TotalDamageReceived);
            }
            finally
            {
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(attacker);
            }
        }

        private static GameObject CreateTarget(string name, Vector3 position)
        {
            GameObject target = new GameObject(name);
            target.transform.position = position;
            target.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            target.AddComponent<TestDamageable>();
            return target;
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public float TotalDamageReceived { get; private set; }

            public void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source)
            {
                TotalDamageReceived += amount;
            }
        }
    }
}
