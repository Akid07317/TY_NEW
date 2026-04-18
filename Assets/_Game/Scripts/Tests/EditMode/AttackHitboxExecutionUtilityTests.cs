using System.Reflection;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class AttackHitboxExecutionUtilityTests
    {
        [Test]
        public void TryBuildExecutionPlan_UsesConfiguredSphereCenter_AndScaledDamage()
        {
            GameObject attacker = new GameObject("Attacker");
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                attacker.transform.position = new Vector3(2f, 0f, 3f);
                SetPrivateField(attack, "damageMultiplier", 1.5f);
                SetPrivateField(attack, "hitboxShape", AttackHitboxShape.Sphere);
                SetPrivateField(attack, "hitboxLocalCenter", new Vector3(0f, 1f, 2f));
                SetPrivateField(attack, "hitboxRadius", 0.75f);

                Assert.IsTrue(AttackHitboxExecutionUtility.TryBuildExecutionPlan(
                    attack,
                    attacker.transform,
                    10f,
                    out AttackHitboxExecutionPlan plan));

                Assert.AreEqual(AttackHitboxShape.Sphere, plan.Shape);
                Assert.AreEqual(15f, plan.Damage, 0.0001f);
                Assert.AreEqual(0.75f, plan.Radius, 0.0001f);
                AssertVectorApproximately(new Vector3(2f, 1f, 5f), plan.Center);
            }
            finally
            {
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(attacker);
            }
        }

        [Test]
        public void TryBuildExecutionPlan_FallsBackToLegacyForwardSphere_WhenBoxIsInvalid()
        {
            GameObject attacker = new GameObject("Attacker");
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                attacker.transform.position = Vector3.one;
                attacker.transform.forward = Vector3.right;
                SetPrivateField(attack, "damageMultiplier", 2f);
                SetPrivateField(attack, "hitboxShape", AttackHitboxShape.Box);
                SetPrivateField(attack, "hitboxHalfExtents", new Vector3(0f, 1f, 1f));
                SetPrivateField(attack, "range", 1.5f);
                SetPrivateField(attack, "radius", 0.4f);

                Assert.IsTrue(AttackHitboxExecutionUtility.TryBuildExecutionPlan(
                    attack,
                    attacker.transform,
                    6f,
                    out AttackHitboxExecutionPlan plan));

                Assert.AreEqual(AttackHitboxShape.Sphere, plan.Shape);
                Assert.AreEqual(12f, plan.Damage, 0.0001f);
                Assert.AreEqual(0.4f, plan.Radius, 0.0001f);
                AssertVectorApproximately(new Vector3(2.5f, 1f, 1f), plan.Center);
            }
            finally
            {
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(attacker);
            }
        }

        [Test]
        public void TryResolveDamageable_RejectsSourceRoot_AndAcceptsExternalTarget()
        {
            GameObject source = new GameObject("Source");
            BoxCollider sourceCollider = source.AddComponent<BoxCollider>();
            source.AddComponent<TestDamageable>();

            GameObject target = new GameObject("Target");
            BoxCollider targetCollider = target.AddComponent<BoxCollider>();
            target.AddComponent<TestDamageable>();

            try
            {
                Assert.IsFalse(AttackHitboxExecutionUtility.TryResolveDamageable(sourceCollider, source, out IDamageable selfDamageable));
                Assert.IsNull(selfDamageable);

                Assert.IsTrue(AttackHitboxExecutionUtility.TryResolveDamageable(targetCollider, source, out IDamageable externalDamageable));
                Assert.IsNotNull(externalDamageable);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(source);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source)
            {
            }
        }
    }
}
