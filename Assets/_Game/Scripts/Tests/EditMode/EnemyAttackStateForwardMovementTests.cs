using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EnemyAttackStateForwardMovementTests
    {
        [Test]
        public void AttackState_UsesForwardMovementToCloseGapBeforeStrike()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO attack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                DamageableReceiver targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.transform.position = new Vector3(0f, 0f, 1.65f);
                targetObject.SetActive(true);

                attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(attack, "startupSeconds", 0.1f);
                SetPrivateField(attack, "forwardMovement", 0.5f);
                SetPrivateField(attack, "range", 1f);
                SetPrivateField(attack, "radius", 0.2f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.2f);
                SetPrivateField(archetype, "attackDistance", 1f);
                SetPrivateField(archetype, "attacks", new[] { attack });

                InvokeMethod(attackController, "Awake");
                InvokeMethod(motor, "Awake");
                InvokeMethod(targetReceiver, "Awake");

                Assert.IsFalse(attackController.TryAttack(targetObject.transform, archetype));

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);
                Physics.SyncTransforms();

                stateMachine.Initialize(brain);
                stateMachine.SwitchToAttack();
                stateMachine.Tick(0.2f);

                Assert.AreEqual(90f, targetHealth.CurrentValue, 0.01f);
                Assert.Greater(enemyObject.transform.position.z, 0.45f);
                Assert.Less(enemyObject.transform.position.z, targetObject.transform.position.z);
            }
            finally
            {
                if (attack != null)
                {
                    Object.DestroyImmediate(attack);
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
        public void AttackState_CommitsForwardMovement_AndConsumesCooldownOnWhiff()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO attack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                DamageableReceiver targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.transform.position = new Vector3(0f, 0f, 1.55f);
                targetObject.SetActive(true);

                attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(attack, "startupSeconds", 0.1f);
                SetPrivateField(attack, "activeSeconds", 0.1f);
                SetPrivateField(attack, "recoverySeconds", 0.3f);
                SetPrivateField(attack, "forwardMovement", 0.45f);
                SetPrivateField(attack, "range", 1f);
                SetPrivateField(attack, "radius", 0.2f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.4f);
                SetPrivateField(archetype, "attackDistance", 1f);
                SetPrivateField(archetype, "attacks", new[] { attack });

                InvokeMethod(attackController, "Awake");
                InvokeMethod(motor, "Awake");
                InvokeMethod(targetReceiver, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);
                Physics.SyncTransforms();

                stateMachine.Initialize(brain);
                stateMachine.SwitchToAttack();
                stateMachine.Tick(0.1f);

                targetObject.transform.position = new Vector3(2f, 0f, 1.55f);
                Physics.SyncTransforms();
                stateMachine.Tick(0.2f);

                Assert.AreEqual(targetHealth.MaxValue, targetHealth.CurrentValue, 0.01f);
                Assert.AreEqual(0f, enemyObject.transform.position.x, 0.01f);
                Assert.Greater(enemyObject.transform.position.z, 0.35f);
                Assert.IsFalse(attackController.CanAttack(archetype.AttackCooldown));
                Assert.IsInstanceOf<EnemyAttackState>(stateMachine.CurrentState);
            }
            finally
            {
                if (attack != null)
                {
                    Object.DestroyImmediate(attack);
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
        public void RangedAttackState_WhenClearShotIsLostDuringStartup_SwitchesToStrafe()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject wallObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO attack = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();
                targetObject.transform.position = new Vector3(0f, 0f, 3f);
                targetObject.SetActive(true);

                wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = "Wall";
                wallObject.transform.position = new Vector3(0f, 0f, 1.5f);
                wallObject.transform.localScale = new Vector3(2f, 2f, 0.4f);

                projectilePrefab = new GameObject("ProjectilePrefab");
                ProjectileController prefabProjectile = projectilePrefab.AddComponent<ProjectileController>();

                attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(attack, "startupSeconds", 0.1f);
                SetPrivateField(attack, "forwardMovement", 0f);
                SetPrivateField(attack, "range", 4f);
                SetPrivateField(attack, "radius", 0.25f);
                SetPrivateField(attack, "projectilePrefab", projectilePrefab);
                SetPrivateField(attack, "projectileSpeed", 18f);
                SetPrivateField(attack, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(attack, "projectileSpawnOffset", 0.25f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Ranged);
                SetPrivateField(archetype, "baseAttack", 10f);
                SetPrivateField(archetype, "attackCooldown", 0.2f);
                SetPrivateField(archetype, "attackDistance", 4f);
                SetPrivateField(archetype, "preferredCombatDistance", 2.5f);
                SetPrivateField(archetype, "strafeDistance", 1f);
                SetPrivateField(archetype, "strafeDurationSeconds", 0.2f);
                SetPrivateField(archetype, "attacks", new[] { attack });

                InvokeMethod(attackController, "Awake");
                InvokeMethod(motor, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);
                Physics.SyncTransforms();

                stateMachine.Initialize(brain);
                stateMachine.SwitchToAttack();
                stateMachine.Tick(0.2f);

                Assert.IsInstanceOf<EnemyStrafeState>(stateMachine.CurrentState);
                Assert.IsNull(FindProjectileInstance(prefabProjectile));
            }
            finally
            {
                if (attack != null)
                {
                    Object.DestroyImmediate(attack);
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
