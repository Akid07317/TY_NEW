using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CampusRPG.Tests.PlayMode
{
    public sealed class EnemyAttackControllerTests
    {
        [TearDown]
        public void TearDown()
        {
            ProjectileController[] projectiles = Object.FindObjectsByType<ProjectileController>();

            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] != null)
                {
                    Object.DestroyImmediate(projectiles[i].gameObject);
                }
            }
        }

        [Test]
        public void TryAttack_RejectsTargetOutsideRange_AndHitsWhenClose()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;

                targetObject = new GameObject("Target");
                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent health = targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.SetActive(true);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.5f);
                SetPrivateField(archetype, "attackDistance", 2f);

                targetObject.transform.position = new Vector3(0f, 0f, 5f);
                Assert.IsFalse(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(health.MaxValue, health.CurrentValue, 0.01f);

                targetObject.transform.position = new Vector3(0f, 0f, 1.5f);
                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));
                Assert.Less(health.CurrentValue, health.MaxValue);
            }
            finally
            {
                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
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
        public void TryAttack_CyclesConfiguredBossAttacks_WithPerAttackRangeAndDamage()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO closeAttack = null;
            AttackDefinitionSO reachAttack = null;

            try
            {
                enemyObject = new GameObject("Boss");
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;

                targetObject = new GameObject("Target");
                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent health = targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.SetActive(true);

                closeAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                reachAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(closeAttack, "damageMultiplier", 1f);
                SetPrivateField(closeAttack, "range", 1f);
                SetPrivateField(closeAttack, "radius", 0.2f);
                SetPrivateField(closeAttack, "startupSeconds", 0.15f);
                SetPrivateField(reachAttack, "damageMultiplier", 2f);
                SetPrivateField(reachAttack, "range", 2.6f);
                SetPrivateField(reachAttack, "radius", 0.4f);
                SetPrivateField(reachAttack, "startupSeconds", 0.35f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 1f);
                SetPrivateField(archetype, "attacks", new[] { closeAttack, reachAttack });

                targetObject.transform.position = new Vector3(0f, 0f, 1.1f);
                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(90f, health.CurrentValue, 0.01f);

                controller.Tick(0.2f);
                targetObject.transform.position = new Vector3(0f, 0f, 2.8f);
                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(70f, health.CurrentValue, 0.01f);

                controller.Tick(0.2f);
                Assert.AreEqual(1.2f, controller.GetNextAttackRange(archetype), 0.01f);
                Assert.IsFalse(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(70f, health.CurrentValue, 0.01f);
            }
            finally
            {
                if (reachAttack != null)
                {
                    Object.DestroyImmediate(reachAttack);
                }

                if (closeAttack != null)
                {
                    Object.DestroyImmediate(closeAttack);
                }

                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
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
        public void BossPreviewAttackForTarget_PrefersClosestUsableAttackPerDistanceBand()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO closeAttack = null;
            AttackDefinitionSO reachAttack = null;
            AttackDefinitionSO projectileAttack = null;

            try
            {
                enemyObject = new GameObject("Boss");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                enemyObject.AddComponent<HealthComponent>();
                enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 4.8f);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                closeAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                reachAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(closeAttack, "attackId", "Boss_Close");
                SetPrivateField(closeAttack, "range", 1f);
                SetPrivateField(closeAttack, "radius", 0.2f);
                SetPrivateField(reachAttack, "attackId", "Boss_Reach");
                SetPrivateField(reachAttack, "range", 2.8f);
                SetPrivateField(reachAttack, "radius", 0.4f);
                SetPrivateField(projectileAttack, "attackId", "Boss_Projectile");
                SetPrivateField(projectileAttack, "range", 4.8f);
                SetPrivateField(projectileAttack, "radius", 0.35f);
                SetPrivateField(projectileAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(projectileAttack, "projectileSpeed", 18f);
                SetPrivateField(projectileAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(projectileAttack, "projectileSpawnOffset", 0.25f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 1f);
                SetPrivateField(archetype, "attacks", new[] { closeAttack, reachAttack, projectileAttack });

                Assert.AreSame(closeAttack, controller.PreviewNextAttack(archetype));

                targetObject.transform.position = new Vector3(0f, 0f, 1.1f);
                Assert.AreSame(closeAttack, controller.PreviewAttackForTarget(targetObject.transform, archetype));
                Assert.AreEqual(1.2f, controller.GetAttackRangeForTarget(targetObject.transform, archetype), 0.01f);

                targetObject.transform.position = new Vector3(0f, 0f, 3.1f);
                Assert.AreSame(reachAttack, controller.PreviewAttackForTarget(targetObject.transform, archetype));

                targetObject.transform.position = new Vector3(0f, 0f, 4.8f);
                Assert.AreSame(projectileAttack, controller.PreviewAttackForTarget(targetObject.transform, archetype));
                Assert.Greater(controller.GetAttackRangeForTarget(targetObject.transform, archetype), 5f);
                Assert.IsTrue(controller.HasAttackClearShotForTarget(targetObject.transform, archetype));

                float targetStartHealth = targetHealth.CurrentValue;
                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(targetStartHealth, targetHealth.CurrentValue, 0.01f);
                Assert.AreSame(closeAttack, controller.PreviewNextAttack(archetype));
                Assert.AreSame(projectileAttack, controller.PreviewAttackForTarget(targetObject.transform, archetype));
                Assert.IsNotNull(FindProjectileInstance(prefabProjectile));
            }
            finally
            {
                if (projectileAttack != null)
                {
                    Object.DestroyImmediate(projectileAttack);
                }

                if (reachAttack != null)
                {
                    Object.DestroyImmediate(reachAttack);
                }

                if (closeAttack != null)
                {
                    Object.DestroyImmediate(closeAttack);
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
        public void BossPreviewAttackForTarget_AvoidsImmediateRepeat_WhenAlternateAttackIsCloseInFit()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO closeAttack = null;
            AttackDefinitionSO primaryFarAttack = null;
            AttackDefinitionSO alternateFarAttack = null;

            try
            {
                enemyObject = new GameObject("Boss");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                enemyObject.AddComponent<HealthComponent>();
                enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 4.75f);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                projectilePrefab.AddComponent<ProjectileController>();

                closeAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                primaryFarAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                alternateFarAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

                SetPrivateField(closeAttack, "attackId", "Boss_Close");
                SetPrivateField(closeAttack, "range", 1f);
                SetPrivateField(closeAttack, "radius", 0.2f);

                SetPrivateField(primaryFarAttack, "attackId", "Boss_Far_Primary");
                SetPrivateField(primaryFarAttack, "range", 4.8f);
                SetPrivateField(primaryFarAttack, "radius", 0.35f);
                SetPrivateField(primaryFarAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(primaryFarAttack, "projectileSpeed", 18f);
                SetPrivateField(primaryFarAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(primaryFarAttack, "projectileSpawnOffset", 0.25f);

                SetPrivateField(alternateFarAttack, "attackId", "Boss_Far_Alternate");
                SetPrivateField(alternateFarAttack, "range", 4.5f);
                SetPrivateField(alternateFarAttack, "radius", 0.35f);
                SetPrivateField(alternateFarAttack, "projectilePrefab", projectilePrefab);
                SetPrivateField(alternateFarAttack, "projectileSpeed", 18f);
                SetPrivateField(alternateFarAttack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(alternateFarAttack, "projectileSpawnOffset", 0.25f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.1f);
                SetPrivateField(archetype, "attackDistance", 1f);
                SetPrivateField(archetype, "attacks", new[] { closeAttack, primaryFarAttack, alternateFarAttack });
                Physics.SyncTransforms();

                Assert.AreSame(primaryFarAttack, controller.PreviewAttackForTarget(targetObject.transform, archetype));
                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreSame(alternateFarAttack, controller.PreviewAttackForTarget(targetObject.transform, archetype));
            }
            finally
            {
                if (alternateFarAttack != null)
                {
                    Object.DestroyImmediate(alternateFarAttack);
                }

                if (primaryFarAttack != null)
                {
                    Object.DestroyImmediate(primaryFarAttack);
                }

                if (closeAttack != null)
                {
                    Object.DestroyImmediate(closeAttack);
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

        [UnityTest]
        public System.Collections.IEnumerator TryAttack_LaunchesProjectile_ForProjectileAttack_WithoutHittingSource()
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
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();
                EnemyAttackController controller = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 2.5f);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                projectilePrefab.AddComponent<ProjectileController>();

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
                Physics.SyncTransforms();

                float enemyStartHealth = enemyHealth.CurrentValue;
                float targetStartHealth = targetHealth.CurrentValue;

                Assert.IsTrue(controller.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(targetStartHealth, targetHealth.CurrentValue, 0.01f);

                float timeout = Time.time + 1f;

                while (targetHealth.CurrentValue >= targetStartHealth && Time.time < timeout)
                {
                    yield return null;
                }

                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);
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
        public void TryAttack_DoesNotLaunchProjectile_ThroughWall()
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

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
