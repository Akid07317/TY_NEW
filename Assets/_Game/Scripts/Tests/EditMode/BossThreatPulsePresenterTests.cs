using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossThreatPulsePresenterTests
    {
        [Test]
        public void BossThreatPulsePresenter_PulsesOnBossActivationAndAttackState()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            BossTelegraphStyleSO telegraphStyle = ScriptableObject.CreateInstance<BossTelegraphStyleSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossThreatPulsePresenter");

            try
            {
                Color encounterPulseColor = new Color(0.2f, 0.7f, 0.8f, 0.31f);
                Color attackPulseColor = new Color(0.9f, 0.3f, 0.2f, 0.47f);
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(telegraphStyle, "encounterPulseColor", encounterPulseColor);
                SetPrivateField(telegraphStyle, "attackPulseColor", attackPulseColor);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);

                BossThreatPulsePresenter presenter = presenterObject.AddComponent<BossThreatPulsePresenter>();
                presenter.Configure(bossBrain, telegraphStyle);
                SetPrivateField(presenter, "encounterPulseSeconds", 0.4f);
                SetPrivateField(presenter, "attackPulseSeconds", 0.2f);

                bossObject.SetActive(false);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);
                Assert.AreEqual(BossThreatPulsePresenter.PulseKind.None, presenter.CurrentPulseKind);

                bossObject.SetActive(true);
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyIdleGuardState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossThreatPulsePresenter.PulseKind.Encounter, presenter.CurrentPulseKind);
                Assert.AreEqual(0.4f, presenter.RemainingVisibleSeconds, 0.001f);
                Assert.AreEqual(encounterPulseColor, GetPrivateField<Color>(presenter, "currentPulseColor"));

                InvokeMethod(presenter, "Tick", 0.5f);
                Assert.IsFalse(presenter.IsVisible);
                Assert.AreEqual(BossThreatPulsePresenter.PulseKind.None, presenter.CurrentPulseKind);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossThreatPulsePresenter.PulseKind.Attack, presenter.CurrentPulseKind);
                Assert.AreEqual(0.2f, presenter.RemainingVisibleSeconds, 0.001f);
                Assert.AreEqual(attackPulseColor, GetPrivateField<Color>(presenter, "currentPulseColor"));

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0.25f);
                Assert.IsFalse(presenter.IsVisible);
                Assert.AreEqual(BossThreatPulsePresenter.PulseKind.None, presenter.CurrentPulseKind);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossThreatPulsePresenter.PulseKind.Attack, presenter.CurrentPulseKind);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(telegraphStyle);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            }

            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, arguments);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static TValue GetPrivateField<TValue>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TValue)field.GetValue(instance);
        }
    }
}
