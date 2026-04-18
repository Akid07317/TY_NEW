using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossIntroPresenterTests
    {
        [Test]
        public void BossIntroPresenter_ShowsOnBossActivationAndResetsAfterReactivation()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossIntroPresenter");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);

                BossIntroPresenter presenter = presenterObject.AddComponent<BossIntroPresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "visibleDurationSeconds", 2f);

                bossObject.SetActive(false);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);

                bossObject.SetActive(true);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(2f, presenter.RemainingVisibleSeconds, 0.001f);

                InvokeMethod(presenter, "Tick", 1.25f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(0.75f, presenter.RemainingVisibleSeconds, 0.001f);

                InvokeMethod(presenter, "Tick", 0.8f);
                Assert.IsFalse(presenter.IsVisible);

                bossObject.SetActive(false);
                InvokeMethod(presenter, "Tick", 0f);
                bossObject.SetActive(true);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(2f, presenter.RemainingVisibleSeconds, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
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
    }
}
