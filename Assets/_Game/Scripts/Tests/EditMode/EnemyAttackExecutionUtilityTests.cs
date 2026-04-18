using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyAttackExecutionUtilityTests
    {
        [Test]
        public void ResolveDamage_AppliesAttackMultiplier()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "damageMultiplier", 1.75f);
                Assert.AreEqual(17.5f, EnemyAttackExecutionUtility.ResolveDamage(10f, attack), 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void TryBuildProjectileLaunchPlan_UsesOriginForward_WhenTargetOverlapsOrigin()
        {
            GameObject originObject = new GameObject("Origin");
            GameObject targetObject = new GameObject("Target");
            GameObject projectilePrefab = new GameObject("ProjectilePrefab");
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                projectilePrefab.AddComponent<ProjectileController>();
                originObject.transform.position = Vector3.zero;
                originObject.transform.forward = Vector3.right;
                targetObject.transform.position = Vector3.zero;

                SetPrivateField(attack, "projectilePrefab", projectilePrefab);
                SetPrivateField(attack, "projectileSpawnOffset", 0.4f);
                SetPrivateField(attack, "projectileSpeed", 18f);
                SetPrivateField(attack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(attack, "radius", 0.3f);

                Assert.IsTrue(EnemyAttackExecutionUtility.TryBuildProjectileLaunchPlan(
                    targetObject.transform,
                    originObject.transform,
                    attack,
                    12f,
                    out EnemyProjectileLaunchPlan plan));
                AssertVectorApproximately(Vector3.right, plan.Direction);
                AssertVectorApproximately(new Vector3(0.4f, 0f, 0f), plan.SpawnPosition);
                Assert.AreEqual(12f, plan.Damage, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(originObject);
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void TryResolveAttackTarget_RejectsProjectileTarget_WithoutClearShot()
        {
            GameObject originObject = new GameObject("Origin");
            GameObject targetObject = new GameObject("Target");
            EnemyArchetypeSO archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject projectilePrefab = new GameObject("ProjectilePrefab");

            try
            {
                originObject.transform.position = Vector3.zero;
                originObject.transform.forward = Vector3.forward;
                targetObject.transform.position = new Vector3(0f, 0f, 2f);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();

                projectilePrefab.AddComponent<ProjectileController>();

                SetPrivateField(archetype, "attackDistance", 3f);
                SetPrivateField(attack, "range", 3f);
                SetPrivateField(attack, "radius", 0.25f);
                SetPrivateField(attack, "projectilePrefab", projectilePrefab);

                Assert.IsFalse(EnemyAttackExecutionUtility.TryResolveAttackTarget(
                    targetObject.transform,
                    originObject.transform,
                    archetype,
                    attack,
                    0.35f,
                    70f,
                    canAttack: true,
                    hasClearShot: false,
                    out IDamageable damageable));
                Assert.IsNull(damageable);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(archetype);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(originObject);
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
    }
}
