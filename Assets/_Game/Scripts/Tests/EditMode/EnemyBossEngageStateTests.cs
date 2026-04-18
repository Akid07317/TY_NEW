using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EnemyBossEngageStateTests
    {
        [Test]
        public void BossIdleGuardState_UsesEngageBeatBeforeChase()
        {
            GameObject enemyObject = new GameObject("Gatekeeper");
            GameObject targetObject = new GameObject("PlayerTarget");
            EnemyArchetypeSO archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();

            try
            {
                EnemySensing sensing = enemyObject.AddComponent<EnemySensing>();
                EnemyStateMachine stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
                HealthComponent enemyHealth = enemyObject.AddComponent<HealthComponent>();
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject.SetActive(false);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();
                targetObject.transform.position = new Vector3(0f, 0f, 2f);
                targetObject.SetActive(true);

                SetPrivateField(archetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(archetype, "aggroDistance", 6f);
                SetPrivateField(archetype, "engageDurationSeconds", 0.6f);

                SetPrivateField(brain, "archetype", archetype);
                SetPrivateField(brain, "sensing", sensing);
                SetPrivateField(brain, "stateMachine", stateMachine);
                SetPrivateField(brain, "health", enemyHealth);

                stateMachine.Initialize(brain);
                Assert.IsInstanceOf<EnemyIdleGuardState>(stateMachine.CurrentState);

                stateMachine.Tick(0.01f);
                Assert.IsInstanceOf<EnemyEngageState>(stateMachine.CurrentState);

                stateMachine.Tick(0.3f);
                Assert.IsInstanceOf<EnemyEngageState>(stateMachine.CurrentState);

                stateMachine.Tick(0.4f);
                Assert.IsInstanceOf<EnemyChaseState>(stateMachine.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(archetype);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(enemyObject);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
