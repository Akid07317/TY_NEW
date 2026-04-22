using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EnemyAttackStateBossRecoveryTests
    {
        [Test]
        public void BossAttackState_HoldsRecoveryWindowBeforeReturningToChase()
        {
            GameObject enemyObject = new GameObject("Gatekeeper");
            GameObject targetObject = new GameObject("Target");
            EnemyArchetypeSO archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.transform.position = new Vector3(0f, 0f, 1.2f);
                targetObject.SetActive(true);

                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(archetype, "attackDistance", 1.5f);
                SetPrivateField(archetype, "attackCooldown", 0.2f);
                SetPrivateField(archetype, "attacks", new[] { attack });

                SetPrivateField(attack, "startupSeconds", 0.1f);
                SetPrivateField(attack, "recoverySeconds", 0.35f);
                SetPrivateField(attack, "range", 1f);
                SetPrivateField(attack, "radius", 0.3f);

                InvokeMethod(attackController, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                brain.SetTarget(targetObject.transform);

                stateMachine.Initialize(brain);
                stateMachine.SwitchToAttack();

                stateMachine.Tick(0.2f);
                Assert.IsInstanceOf<EnemyAttackState>(stateMachine.CurrentState);

                stateMachine.Tick(0.1f);
                Assert.IsInstanceOf<EnemyAttackState>(stateMachine.CurrentState);

                stateMachine.Tick(0.3f);
                Assert.IsInstanceOf<EnemyChaseState>(stateMachine.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(archetype);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void NonBossAttackState_HoldsShortRecoveryBeforeReturningToChase()
        {
            GameObject enemyObject = new GameObject("MeleeEnemy");
            GameObject targetObject = new GameObject("Target");
            EnemyArchetypeSO archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                EnemyMotor motor = enemyObject.AddComponent<EnemyMotor>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.transform.position = new Vector3(0f, 0f, 1.2f);
                targetObject.SetActive(true);

                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Melee);
                SetPrivateField(archetype, "attackDistance", 1.5f);
                SetPrivateField(archetype, "attackCooldown", 0.2f);
                SetPrivateField(archetype, "attacks", new[] { attack });

                SetPrivateField(attack, "startupSeconds", 0.1f);
                SetPrivateField(attack, "activeSeconds", 0.08f);
                SetPrivateField(attack, "recoverySeconds", 0.24f);
                SetPrivateField(attack, "range", 1f);
                SetPrivateField(attack, "radius", 0.3f);

                InvokeMethod(motor, "Awake");
                InvokeMethod(attackController, "Awake");

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "attackController", attackController);
                SetPrivateField(brain, "health", enemyHealth);
                SetPrivateField(brain, "motor", motor);
                brain.SetTarget(targetObject.transform);

                stateMachine.Initialize(brain);
                stateMachine.SwitchToAttack();

                stateMachine.Tick(0.18f);
                Assert.IsInstanceOf<EnemyAttackState>(stateMachine.CurrentState);

                stateMachine.Tick(0.08f);
                Assert.IsInstanceOf<EnemyAttackState>(stateMachine.CurrentState);

                stateMachine.Tick(0.1f);
                Assert.IsInstanceOf<EnemyChaseState>(stateMachine.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(archetype);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(enemyObject);
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

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
