using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EnemyStrafeStateTests
    {
        [Test]
        public void MobileEnemy_EntersStrafeStateAndGeneratesLateralMovementTarget()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;

            try
            {
                enemyObject = new GameObject("MobileEnemy");
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 1.8f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Mobile);
                SetPrivateField(archetype, "attackDistance", 2f);
                SetPrivateField(archetype, "attackCooldown", 0.25f);
                SetPrivateField(archetype, "preferredCombatDistance", 1.4f);
                SetPrivateField(archetype, "strafeDistance", 0.9f);
                SetPrivateField(archetype, "strafeDurationSeconds", 0.2f);

                InvokeMethod(motor, "Awake");
                InvokeMethod(attackController, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);
                Physics.SyncTransforms();

                stateMachine.Initialize(brain);
                stateMachine.SwitchToChase();
                stateMachine.Tick(0.01f);

                Assert.IsInstanceOf<EnemyStrafeState>(stateMachine.CurrentState);

                stateMachine.Tick(0.05f);

                Assert.IsTrue(GetPrivateField<bool>(motor, "isFallbackMoving"));

                Vector3 fallbackTargetPosition = GetPrivateField<Vector3>(motor, "fallbackTargetPosition");
                Assert.Greater(Mathf.Abs(fallbackTargetPosition.x), 0.01f);
                Assert.Less(fallbackTargetPosition.z, targetObject.transform.position.z);

                stateMachine.Tick(0.25f);
                Assert.IsInstanceOf<EnemyAttackState>(stateMachine.CurrentState);
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
        public void MobileEnemy_StrafeDirection_RemainsStableAcrossReentry()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;

            try
            {
                enemyObject = new GameObject("MobileEnemy_A");
                enemyObject.transform.position = new Vector3(1.25f, 0f, -0.75f);
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 1.8f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Mobile);
                SetPrivateField(archetype, "attackDistance", 2f);
                SetPrivateField(archetype, "attackCooldown", 0.25f);
                SetPrivateField(archetype, "preferredCombatDistance", 1.4f);
                SetPrivateField(archetype, "strafeDistance", 0.9f);
                SetPrivateField(archetype, "strafeDurationSeconds", 0.2f);

                InvokeMethod(motor, "Awake");
                InvokeMethod(attackController, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);
                Physics.SyncTransforms();

                stateMachine.Initialize(brain);
                stateMachine.SwitchToStrafe();
                float firstDirection = GetPrivateField<float>(stateMachine.CurrentState, "strafeDirection");

                stateMachine.SwitchToChase();
                stateMachine.SwitchToStrafe();
                float secondDirection = GetPrivateField<float>(stateMachine.CurrentState, "strafeDirection");

                Assert.AreEqual(firstDirection, secondDirection);
                Assert.AreEqual(1f, Mathf.Abs(firstDirection), 0.001f);
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
        public void RangedEnemy_RetreatsWhenTargetIsTooCloseBeforeAttacking()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;

            try
            {
                enemyObject = new GameObject("RangedEnemy");
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 1.2f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Ranged);
                SetPrivateField(archetype, "attackDistance", 4f);
                SetPrivateField(archetype, "attackCooldown", 0.25f);
                SetPrivateField(archetype, "preferredCombatDistance", 2.5f);

                InvokeMethod(motor, "Awake");
                InvokeMethod(attackController, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);
                Physics.SyncTransforms();

                stateMachine.Initialize(brain);
                stateMachine.SwitchToChase();
                stateMachine.Tick(0.01f);

                Assert.IsInstanceOf<EnemyChaseState>(stateMachine.CurrentState);
                Assert.IsTrue(GetPrivateField<bool>(motor, "isFallbackMoving"));

                Vector3 retreatTargetPosition = GetPrivateField<Vector3>(motor, "fallbackTargetPosition");
                Assert.Less(retreatTargetPosition.z, 0f);

                targetObject.transform.position = new Vector3(0f, 0f, 3f);
                stateMachine.Tick(0.01f);

                Assert.IsInstanceOf<EnemyAttackState>(stateMachine.CurrentState);
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
        public void RangedEnemy_InRangeWithoutClearShot_EntersStrafeStateToFindAngle()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject wallObject = null;
            GameObject projectilePrefab = null;
            EnemyArchetypeSO archetype = null;
            AttackDefinitionSO projectileAttack = null;

            try
            {
                enemyObject = new GameObject("RangedEnemy");
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("Target");
                targetObject.transform.position = new Vector3(0f, 0f, 3f);
                targetObject.AddComponent<BoxCollider>();

                wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = "Wall";
                wallObject.transform.position = new Vector3(0f, 0f, 1.5f);
                wallObject.transform.localScale = new Vector3(2f, 2f, 0.4f);

                projectilePrefab = new GameObject("ProjectilePrefab");
                projectilePrefab.AddComponent<ProjectileController>();

                projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(projectileAttack, "range", 4f);
                SetPrivateField(projectileAttack, "radius", 0.25f);
                SetPrivateField(projectileAttack, "projectilePrefab", projectilePrefab);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Ranged);
                SetPrivateField(archetype, "attackDistance", 4f);
                SetPrivateField(archetype, "attackCooldown", 0.25f);
                SetPrivateField(archetype, "preferredCombatDistance", 2.5f);
                SetPrivateField(archetype, "strafeDistance", 1f);
                SetPrivateField(archetype, "strafeDurationSeconds", 0.2f);
                SetPrivateField(archetype, "attacks", new[] { projectileAttack });

                InvokeMethod(motor, "Awake");
                InvokeMethod(attackController, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);
                Physics.SyncTransforms();

                stateMachine.Initialize(brain);
                stateMachine.SwitchToChase();
                stateMachine.Tick(0.01f);

                Assert.IsInstanceOf<EnemyStrafeState>(stateMachine.CurrentState);

                stateMachine.Tick(0.05f);
                Assert.IsTrue(GetPrivateField<bool>(motor, "isFallbackMoving"));

                Vector3 fallbackTargetPosition = GetPrivateField<Vector3>(motor, "fallbackTargetPosition");
                Assert.Greater(Mathf.Abs(fallbackTargetPosition.x), 0.01f);
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
    }
}
