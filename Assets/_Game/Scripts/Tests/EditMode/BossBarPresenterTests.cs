using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossBarPresenterTests
    {
        [Test]
        public void BossBarPresenter_ShowsOnlyForActiveLivingBoss()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossBarPresenter");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);

                BossBarPresenter presenter = presenterObject.AddComponent<BossBarPresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);

                bossObject.SetActive(false);
                InvokeMethod(presenter, "Update");
                Assert.IsFalse(presenter.IsVisible);

                bossObject.SetActive(true);
                InvokeMethod(presenter, "Update");
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(1f, presenter.CurrentFillNormalized, 0.001f);

                health.ReceiveDamage(45f, Vector3.zero, presenterObject);
                InvokeMethod(presenter, "Update");
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(0.75f, presenter.CurrentFillNormalized, 0.001f);

                health.ReceiveDamage(135f, Vector3.zero, presenterObject);
                InvokeMethod(presenter, "Update");
                Assert.IsFalse(presenter.IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(bossArchetype);
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
