using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyProjectileAttackTests
    {
        [Test]
        public void TryAttack_LaunchesProjectile_WithoutInstantDamage_AndIgnoresEnemyBodies()
        {
            GameObject enemyObject = null;
            GameObject friendlyObject = null;
            GameObject targetObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO projectileAttack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                DamageableReceiver enemyReceiver = enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                friendlyObject = new GameObject("Friendly");
                friendlyObject.transform.position = new Vector3(0f, 0f, 1.2f);
                friendlyObject.AddComponent<BoxCollider>();
                HealthComponent friendlyHealth = friendlyObject.AddComponent<HealthComponent>();
                DamageableReceiver friendlyReceiver = friendlyObject.AddComponent<DamageableReceiver>();
                friendlyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 2.5f);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                DamageableReceiver targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(projectileAttack, "damageMultiplier", 1.5f);
                SetPrivateField(projectileAttack, "range", 4f);
                SetPrivateField(projectileAttack, "radius", 0.25f);
                SetPrivateField(projectileAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(projectileAttack, "projectileSpeed", 18f);
                SetPrivateField(projectileAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(projectileAttack, "projectileSpawnOffset", 0.25f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 4f);
                SetPrivateField(archetype, "attacks", new[] { projectileAttack });
                InvokeMethod(enemyReceiver, "Awake");
                InvokeMethod(friendlyReceiver, "Awake");
                InvokeMethod(targetReceiver, "Awake");
                InvokeMethod(controller, "Awake");
                Physics.SyncTransforms();

                float enemyStartHealth = enemyHealth.CurrentValue;
                float friendlyStartHealth = friendlyHealth.CurrentValue;
                float targetStartHealth = targetHealth.CurrentValue;

                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(targetStartHealth, targetHealth.CurrentValue, 0.01f);

                ProjectileController projectileInstance = FindProjectileInstance(prefabProjectile);
                Assert.IsNotNull(projectileInstance);

                projectileInstance.Tick(0.05f);
                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);
                Assert.AreEqual(friendlyStartHealth, friendlyHealth.CurrentValue, 0.01f);
                Assert.AreEqual(targetStartHealth, targetHealth.CurrentValue, 0.01f);

                projectileInstance.Tick(0.15f);
                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);
                Assert.AreEqual(friendlyStartHealth, friendlyHealth.CurrentValue, 0.01f);
                Assert.AreEqual(85f, targetHealth.CurrentValue, 0.01f);
            }
            finally
            {
                if (projectileAttack != null)
                {
                    Object.DestroyImmediate(projectileAttack);
                }

                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (friendlyObject != null)
                {
                    Object.DestroyImmediate(friendlyObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void TryAttack_SpawnsImpactEffect_WhenProjectileHitsTarget()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject projectilePrefab = null;
            GameObject impactEffectPrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO projectileAttack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                enemyObject.AddComponent<HealthComponent>();
                DamageableReceiver enemyReceiver = enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 2.5f);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                DamageableReceiver targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                impactEffectPrefab = new GameObject("ImpactEffectPrefab");
                TransientVisualEffect prefabImpactEffect = impactEffectPrefab.AddComponent<TransientVisualEffect>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();
                SetPrivateField(prefabProjectile, "impactEffectPrefab", impactEffectPrefab);

                projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(projectileAttack, "damageMultiplier", 1.5f);
                SetPrivateField(projectileAttack, "range", 4f);
                SetPrivateField(projectileAttack, "radius", 0.25f);
                SetPrivateField(projectileAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(projectileAttack, "projectileSpeed", 18f);
                SetPrivateField(projectileAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(projectileAttack, "projectileSpawnOffset", 0.25f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 4f);
                SetPrivateField(archetype, "attacks", new[] { projectileAttack });
                InvokeMethod(enemyReceiver, "Awake");
                InvokeMethod(targetReceiver, "Awake");
                InvokeMethod(controller, "Awake");
                Physics.SyncTransforms();

                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));

                ProjectileController projectileInstance = FindProjectileInstance(prefabProjectile);
                Assert.IsNotNull(projectileInstance);

                projectileInstance.Tick(0.2f);

                TransientVisualEffect impactEffectInstance = FindImpactEffectInstance(prefabImpactEffect);
                Assert.IsNotNull(impactEffectInstance);
            }
            finally
            {
                if (projectileAttack != null)
                {
                    Object.DestroyImmediate(projectileAttack);
                }

                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
                }

                if (impactEffectPrefab != null)
                {
                    Object.DestroyImmediate(impactEffectPrefab);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void TryAttack_UsesArcTrajectoryOverride_FromAttackDefinition()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO projectileAttack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                enemyObject.AddComponent<HealthComponent>();
                DamageableReceiver enemyReceiver = enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 5f);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                DamageableReceiver targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(projectileAttack, "damageMultiplier", 1.5f);
                SetPrivateField(projectileAttack, "range", 6f);
                SetPrivateField(projectileAttack, "radius", 0.25f);
                SetPrivateField(projectileAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(projectileAttack, "projectileSpeed", 18f);
                SetPrivateField(projectileAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(projectileAttack, "projectileSpawnOffset", 0.25f);
                SetPrivateField(projectileAttack, "projectileTrajectoryMode", ProjectileTrajectoryMode.Arc);
                SetPrivateField(projectileAttack, "projectileArcHeight", 1.2f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 6f);
                SetPrivateField(archetype, "attacks", new[] { projectileAttack });
                InvokeMethod(enemyReceiver, "Awake");
                InvokeMethod(targetReceiver, "Awake");
                InvokeMethod(controller, "Awake");
                Physics.SyncTransforms();

                float targetStartHealth = targetHealth.CurrentValue;

                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));

                ProjectileController projectileInstance = FindProjectileInstance(prefabProjectile);
                Assert.IsNotNull(projectileInstance);

                projectileInstance.Tick(0.2f);
                Assert.Greater(projectileInstance.transform.position.y, 0.45f);
                Assert.AreEqual(targetStartHealth, targetHealth.CurrentValue, 0.01f);

                projectileInstance.Tick(0.1f);
                Assert.AreEqual(85f, targetHealth.CurrentValue, 0.01f);
            }
            finally
            {
                if (projectileAttack != null)
                {
                    Object.DestroyImmediate(projectileAttack);
                }

                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void TryAttack_RejectsProjectileTarget_WhenWorldColliderBlocksShot()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject wallObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO projectileAttack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                enemyObject.AddComponent<HealthComponent>();
                DamageableReceiver enemyReceiver = enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 4f);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                DamageableReceiver targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = "Wall";
                wallObject.transform.position = new Vector3(0f, 0f, 1.5f);
                wallObject.transform.localScale = new Vector3(2f, 2f, 0.2f);

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(projectileAttack, "damageMultiplier", 1.5f);
                SetPrivateField(projectileAttack, "range", 5f);
                SetPrivateField(projectileAttack, "radius", 0.25f);
                SetPrivateField(projectileAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(projectileAttack, "projectileSpeed", 18f);
                SetPrivateField(projectileAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(projectileAttack, "projectileSpawnOffset", 0.25f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 5f);
                SetPrivateField(archetype, "attacks", new[] { projectileAttack });
                InvokeMethod(enemyReceiver, "Awake");
                InvokeMethod(targetReceiver, "Awake");
                InvokeMethod(controller, "Awake");
                Physics.SyncTransforms();

                float targetStartHealth = targetHealth.CurrentValue;

                Assert.IsFalse(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(targetStartHealth, targetHealth.CurrentValue, 0.01f);
                Assert.IsNull(FindProjectileInstance(prefabProjectile));
            }
            finally
            {
                if (projectileAttack != null)
                {
                    Object.DestroyImmediate(projectileAttack);
                }

                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (wallObject != null)
                {
                    Object.DestroyImmediate(wallObject);
                }

                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void TryAttack_RejectsProjectileTarget_WithoutClearShot()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject wallObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO projectileAttack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                enemyObject.AddComponent<HealthComponent>();
                enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 3f);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = "Wall";
                wallObject.transform.position = new Vector3(0f, 0f, 1.5f);
                wallObject.transform.localScale = new Vector3(2f, 2f, 0.4f);

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(projectileAttack, "damageMultiplier", 1.5f);
                SetPrivateField(projectileAttack, "range", 4f);
                SetPrivateField(projectileAttack, "radius", 0.25f);
                SetPrivateField(projectileAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(projectileAttack, "projectileSpeed", 18f);
                SetPrivateField(projectileAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(projectileAttack, "projectileSpawnOffset", 0.25f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 4f);
                SetPrivateField(archetype, "attacks", new[] { projectileAttack });
                InvokeMethod(enemyObject.GetComponent<DamageableReceiver>(), "Awake");
                InvokeMethod(targetObject.GetComponent<DamageableReceiver>(), "Awake");
                InvokeMethod(controller, "Awake");
                Physics.SyncTransforms();

                float targetStartHealth = targetHealth.CurrentValue;

                Assert.IsFalse(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(targetStartHealth, targetHealth.CurrentValue, 0.01f);
                Assert.IsNull(FindProjectileInstance(prefabProjectile));
            }
            finally
            {
                if (projectileAttack != null)
                {
                    Object.DestroyImmediate(projectileAttack);
                }

                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (wallObject != null)
                {
                    Object.DestroyImmediate(wallObject);
                }

                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        private static ProjectileController FindProjectileInstance(ProjectileController prefabProjectile)
        {
            ProjectileController[] projectiles = Object.FindObjectsByType<ProjectileController>();

            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != prefabProjectile)
                {
                    return projectiles[i];
                }
            }

            return null;
        }

        private static TransientVisualEffect FindImpactEffectInstance(TransientVisualEffect prefabEffect)
        {
            TransientVisualEffect[] effects = Object.FindObjectsByType<TransientVisualEffect>();

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != prefabEffect)
                {
                    return effects[i];
                }
            }

            return null;
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void InvokeMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            }

            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
        }
    }
}
