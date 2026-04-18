using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossSpawnFlarePresenterTests
    {
        [Test]
        public void BossSpawnFlarePresenter_ShowsOnBossActivationAndReactivation()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            BossTelegraphStyleSO telegraphStyle = ScriptableObject.CreateInstance<BossTelegraphStyleSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossSpawnFlarePresenter");
            GameObject flareTemplate = new GameObject("StyledSpawnFlareTemplate");
            Material flareMaterial = null;

            try
            {
                Color flareColor = new Color(0.95f, 0.67f, 0.28f, 0.81f);
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(telegraphStyle, "spawnFlareColor", flareColor);
                bossObject.transform.position = new Vector3(2f, 0f, 3f);

                GameObject flareCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flareCore.name = "SpawnFlareCore";
                flareCore.transform.SetParent(flareTemplate.transform, false);
                Object.DestroyImmediate(flareCore.GetComponent<Collider>());

                flareMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
                SetPrivateField(telegraphStyle, "spawnFlareVisualPrefab", flareTemplate);
                SetPrivateField(telegraphStyle, "spawnFlareMaterial", flareMaterial);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);

                BossSpawnFlarePresenter presenter = presenterObject.AddComponent<BossSpawnFlarePresenter>();
                presenter.Configure(bossBrain, telegraphStyle);
                SetPrivateField(presenter, "groundOffset", 0.1f);
                SetPrivateField(presenter, "flareDurationSeconds", 0.6f);

                bossObject.SetActive(false);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);

                bossObject.SetActive(true);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(0.6f, presenter.RemainingVisibleSeconds, 0.001f);
                Assert.AreEqual(new Vector3(2f, 0.1f, 3f), presenter.CurrentBasePosition);
                Assert.AreSame(flareTemplate, GetPrivateField<GameObject>(presenter, "currentVisualTemplate"));
                Assert.AreSame(flareMaterial, GetPrivateField<Material>(presenter, "currentMaterialTemplate"));
                AssertColorApproximately(flareColor, GetPrivateField<Material>(presenter, "flareMaterial").color);
                Assert.IsNotNull(GetPrivateField<GameObject>(presenter, "flareVisual").transform.Find("SpawnFlareCore"));

                InvokeMethod(presenter, "Tick", 0.7f);
                Assert.IsFalse(presenter.IsVisible);

                bossObject.SetActive(false);
                InvokeMethod(presenter, "Tick", 0f);
                bossObject.transform.position = new Vector3(-1f, 0f, 4f);
                bossObject.SetActive(true);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(new Vector3(-1f, 0.1f, 4f), presenter.CurrentBasePosition);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(flareTemplate);
                Object.DestroyImmediate(flareMaterial);
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

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
        }
    }
}
