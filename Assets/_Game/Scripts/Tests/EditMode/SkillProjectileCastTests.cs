using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class SkillProjectileCastTests
    {
        [Test]
        public void CommitCast_LaunchesProjectile_WithoutInstantDamage_AndIgnoresFriendlyPlayers()
        {
            GameObject playerObject = null;
            GameObject friendlyPlayerObject = null;
            GameObject enemyObject = null;
            GameObject projectilePrefab = null;
            SkillDefinitionSO skillDefinition = null;

            try
            {
                playerObject = new GameObject("Player");
                playerObject.transform.position = Vector3.zero;
                playerObject.transform.rotation = Quaternion.identity;
                playerObject.AddComponent<BoxCollider>();
                HealthComponent playerHealth = playerObject.AddComponent<HealthComponent>();
                DamageableReceiver playerReceiver = playerObject.AddComponent<DamageableReceiver>();
                playerObject.AddComponent<PlayerCharacter>();
                SkillController skillController = playerObject.AddComponent<SkillController>();

                friendlyPlayerObject = new GameObject("FriendlyPlayer");
                friendlyPlayerObject.transform.position = new Vector3(0f, 0f, 1.2f);
                friendlyPlayerObject.AddComponent<BoxCollider>();
                HealthComponent friendlyHealth = friendlyPlayerObject.AddComponent<HealthComponent>();
                DamageableReceiver friendlyReceiver = friendlyPlayerObject.AddComponent<DamageableReceiver>();
                friendlyPlayerObject.AddComponent<PlayerCharacter>();

                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = new Vector3(0f, 0f, 2.5f);
                enemyObject.AddComponent<BoxCollider>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                DamageableReceiver enemyReceiver = enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();
                SetPrivateField(skillDefinition, "damageMultiplier", 1.5f);
                SetPrivateField(skillDefinition, "impactRadius", 0.25f);
                SetPrivateField(skillDefinition, "targetMode", SkillTargetMode.Forward);
                SetPrivateField(skillDefinition, "projectilePrefab", projectilePrefab);
                SetPrivateField(skillDefinition, "projectileSpeed", 18f);
                SetPrivateField(skillDefinition, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(skillDefinition, "projectileSpawnOffset", 0.25f);
                InvokeMethod(playerReceiver, "Awake");
                InvokeMethod(friendlyReceiver, "Awake");
                InvokeMethod(enemyReceiver, "Awake");
                InvokeMethod(skillController, "Awake");
                Physics.SyncTransforms();

                float playerStartHealth = playerHealth.CurrentValue;
                float friendlyStartHealth = friendlyHealth.CurrentValue;
                float enemyStartHealth = enemyHealth.CurrentValue;

                skillController.CommitCast(skillDefinition);
                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);

                ProjectileController projectileInstance = FindProjectileInstance(prefabProjectile);
                Assert.IsNotNull(projectileInstance);

                projectileInstance.Tick(0.05f);
                Assert.AreEqual(playerStartHealth, playerHealth.CurrentValue, 0.01f);
                Assert.AreEqual(friendlyStartHealth, friendlyHealth.CurrentValue, 0.01f);
                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);

                projectileInstance.Tick(0.15f);
                Assert.AreEqual(playerStartHealth, playerHealth.CurrentValue, 0.01f);
                Assert.AreEqual(friendlyStartHealth, friendlyHealth.CurrentValue, 0.01f);
                Assert.AreEqual(70f, enemyHealth.CurrentValue, 0.01f);
            }
            finally
            {
                if (skillDefinition != null)
                {
                    Object.DestroyImmediate(skillDefinition);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }

                if (friendlyPlayerObject != null)
                {
                    Object.DestroyImmediate(friendlyPlayerObject);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void CommitCast_UsesArcTrajectoryOverride_FromSkillDefinition()
        {
            GameObject playerObject = null;
            GameObject enemyObject = null;
            GameObject projectilePrefab = null;
            SkillDefinitionSO skillDefinition = null;

            try
            {
                playerObject = new GameObject("Player");
                playerObject.transform.position = Vector3.zero;
                playerObject.transform.rotation = Quaternion.identity;
                playerObject.AddComponent<BoxCollider>();
                playerObject.AddComponent<HealthComponent>();
                DamageableReceiver playerReceiver = playerObject.AddComponent<DamageableReceiver>();
                playerObject.AddComponent<PlayerCharacter>();
                SkillController skillController = playerObject.AddComponent<SkillController>();

                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = new Vector3(0f, 0f, 5f);
                enemyObject.AddComponent<BoxCollider>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                DamageableReceiver enemyReceiver = enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();
                SetPrivateField(skillDefinition, "damageMultiplier", 1.5f);
                SetPrivateField(skillDefinition, "impactRadius", 0.25f);
                SetPrivateField(skillDefinition, "targetMode", SkillTargetMode.Forward);
                SetPrivateField(skillDefinition, "projectilePrefab", projectilePrefab);
                SetPrivateField(skillDefinition, "projectileSpeed", 18f);
                SetPrivateField(skillDefinition, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(skillDefinition, "projectileSpawnOffset", 0.25f);
                SetPrivateField(skillDefinition, "projectileTrajectoryMode", ProjectileTrajectoryMode.Arc);
                SetPrivateField(skillDefinition, "projectileArcHeight", 1.2f);
                InvokeMethod(playerReceiver, "Awake");
                InvokeMethod(enemyReceiver, "Awake");
                InvokeMethod(skillController, "Awake");
                Physics.SyncTransforms();

                float enemyStartHealth = enemyHealth.CurrentValue;

                skillController.CommitCast(skillDefinition);

                ProjectileController projectileInstance = FindProjectileInstance(prefabProjectile);
                Assert.IsNotNull(projectileInstance);

                projectileInstance.Tick(0.2f);
                Assert.Greater(projectileInstance.transform.position.y, 0.45f);
                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);

                projectileInstance.Tick(0.1f);
                Assert.AreEqual(70f, enemyHealth.CurrentValue, 0.01f);
            }
            finally
            {
                if (skillDefinition != null)
                {
                    Object.DestroyImmediate(skillDefinition);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void CommitCast_StopsProjectile_OnWorldCollider_BeforeTarget()
        {
            GameObject playerObject = null;
            GameObject enemyObject = null;
            GameObject wallObject = null;
            GameObject projectilePrefab = null;
            GameObject impactEffectPrefab = null;
            SkillDefinitionSO skillDefinition = null;

            try
            {
                playerObject = new GameObject("Player");
                playerObject.transform.position = Vector3.zero;
                playerObject.transform.rotation = Quaternion.identity;
                playerObject.AddComponent<BoxCollider>();
                playerObject.AddComponent<HealthComponent>();
                DamageableReceiver playerReceiver = playerObject.AddComponent<DamageableReceiver>();
                playerObject.AddComponent<PlayerCharacter>();
                SkillController skillController = playerObject.AddComponent<SkillController>();

                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = new Vector3(0f, 0f, 4f);
                enemyObject.AddComponent<BoxCollider>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                DamageableReceiver enemyReceiver = enemyObject.AddComponent<DamageableReceiver>();
                enemyObject.AddComponent<EnemyBrain>();

                wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = "Wall";
                wallObject.transform.position = new Vector3(0f, 0f, 1.5f);
                wallObject.transform.localScale = new Vector3(2f, 2f, 0.2f);

                impactEffectPrefab = new GameObject("ImpactEffectPrefab");
                TransientVisualEffect prefabImpactEffect = impactEffectPrefab.AddComponent<TransientVisualEffect>();

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();
                SetPrivateField(prefabProjectile, "impactEffectPrefab", impactEffectPrefab);

                skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();
                SetPrivateField(skillDefinition, "damageMultiplier", 1.5f);
                SetPrivateField(skillDefinition, "impactRadius", 0.25f);
                SetPrivateField(skillDefinition, "targetMode", SkillTargetMode.Forward);
                SetPrivateField(skillDefinition, "projectilePrefab", projectilePrefab);
                SetPrivateField(skillDefinition, "projectileSpeed", 18f);
                SetPrivateField(skillDefinition, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(skillDefinition, "projectileSpawnOffset", 0.25f);
                InvokeMethod(playerReceiver, "Awake");
                InvokeMethod(enemyReceiver, "Awake");
                InvokeMethod(skillController, "Awake");
                Physics.SyncTransforms();

                float enemyStartHealth = enemyHealth.CurrentValue;

                skillController.CommitCast(skillDefinition);

                ProjectileController projectileInstance = FindProjectileInstance(prefabProjectile);
                Assert.IsNotNull(projectileInstance);

                projectileInstance.Tick(0.2f);
                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);
                Assert.IsNotNull(FindImpactEffectInstance(prefabImpactEffect));

                projectileInstance.Tick(0.2f);
                Assert.AreEqual(enemyStartHealth, enemyHealth.CurrentValue, 0.01f);
            }
            finally
            {
                if (skillDefinition != null)
                {
                    Object.DestroyImmediate(skillDefinition);
                }

                if (impactEffectPrefab != null)
                {
                    Object.DestroyImmediate(impactEffectPrefab);
                }

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                if (wallObject != null)
                {
                    Object.DestroyImmediate(wallObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
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
