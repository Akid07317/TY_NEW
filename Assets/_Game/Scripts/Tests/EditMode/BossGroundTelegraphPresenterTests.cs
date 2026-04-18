using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossGroundTelegraphPresenterTests
    {
        [Test]
        public void BossGroundTelegraphPresenter_ShowsWorldTelegraphForEngageAndAttackStates()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject targetObject = new GameObject("Player_Target");
            GameObject presenterObject = new GameObject("BossGroundTelegraphPresenter");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attackDistance", 1.5f);
                SetPrivateField(bossArchetype, "attacks", new[] { attack });
                SetPrivateField(attack, "range", 2.6f);
                SetPrivateField(attack, "radius", 0.4f);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                bossObject.transform.position = Vector3.zero;
                targetObject.transform.position = new Vector3(0f, 0f, 3f);

                EnemyAttackController controller = bossObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "attackController", controller);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);
                SetPrivateField(bossBrain, "currentTarget", targetObject.transform);

                BossGroundTelegraphPresenter presenter = presenterObject.AddComponent<BossGroundTelegraphPresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "groundOffset", 0.1f);

                bossObject.SetActive(false);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphMode.None, presenter.CurrentMode);

                bossObject.SetActive(true);
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyEngageState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphMode.Engage, presenter.CurrentMode);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphShape.GroundCircle, presenter.CurrentShape);
                Assert.AreEqual(3f, presenter.CurrentRadius, 0.001f);
                Assert.AreEqual(new Vector3(0f, 0.1f, 0f), presenter.CurrentPosition);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphMode.Attack, presenter.CurrentMode);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphShape.GroundCircle, presenter.CurrentShape);
                Assert.AreEqual(3f, presenter.CurrentRadius, 0.001f);
                Assert.AreEqual(new Vector3(0f, 0.1f, 0f), presenter.CurrentPosition);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphMode.None, presenter.CurrentMode);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        [Test]
        public void BossGroundTelegraphPresenter_UsesLaneForStraightProjectileAttackState()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject targetObject = new GameObject("Player_Target");
            GameObject presenterObject = new GameObject("BossGroundTelegraphPresenter");
            GameObject projectilePrefab = new GameObject("StraightProjectilePrefab");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attackDistance", 1.5f);
                SetPrivateField(bossArchetype, "attacks", new[] { attack });
                SetPrivateField(attack, "range", 4.8f);
                SetPrivateField(attack, "radius", 0.5f);
                SetPrivateField(attack, "projectilePrefab", projectilePrefab);
                SetPrivateField(attack, "projectileTrajectoryMode", ProjectileTrajectoryMode.Straight);

                bossObject.transform.position = Vector3.zero;
                targetObject.transform.position = new Vector3(0f, 0f, 6f);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyAttackController controller = bossObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "attackController", controller);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);
                SetPrivateField(bossBrain, "currentTarget", targetObject.transform);

                BossGroundTelegraphPresenter presenter = presenterObject.AddComponent<BossGroundTelegraphPresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "groundOffset", 0.1f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphMode.Attack, presenter.CurrentMode);
                Assert.AreEqual(BossGroundTelegraphPresenter.TelegraphShape.AttackLane, presenter.CurrentShape);
                Assert.AreEqual(0.5f, presenter.CurrentRadius, 0.001f);
                Assert.AreEqual(4.8f, presenter.CurrentLength, 0.001f);
                Assert.AreEqual(new Vector3(0f, 0.1f, 2.4f), presenter.CurrentPosition);
                AssertVector3Approximately(Vector3.forward, presenter.CurrentDirection);

                targetObject.transform.position = new Vector3(6f, 0f, 0f);
                InvokeMethod(presenter, "Tick", 0f);
                Assert.AreEqual(new Vector3(2.4f, 0.1f, 0f), presenter.CurrentPosition);
                AssertVector3Approximately(Vector3.right, presenter.CurrentDirection);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        [Test]
        public void BossGroundTelegraphPresenter_UsesStylePrefabAndMaterials()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            BossTelegraphStyleSO telegraphStyle = ScriptableObject.CreateInstance<BossTelegraphStyleSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject targetObject = new GameObject("Player_Target");
            GameObject presenterObject = new GameObject("BossGroundTelegraphPresenter");
            GameObject telegraphTemplate = new GameObject("StyledGroundTelegraphTemplate");
            Material engageMaterial = null;
            Material attackMaterial = null;

            try
            {
                Color engageColor = new Color(0.88f, 0.72f, 0.22f, 0.93f);
                Color attackColor = new Color(0.96f, 0.24f, 0.2f, 0.91f);
                engageMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
                attackMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
                GameObject telegraphCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
                telegraphCore.name = "GroundTelegraphCore";
                telegraphCore.transform.SetParent(telegraphTemplate.transform, false);
                Object.DestroyImmediate(telegraphCore.GetComponent<Collider>());

                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attackDistance", 1.5f);
                SetPrivateField(bossArchetype, "attacks", new[] { attack });
                SetPrivateField(attack, "range", 2.8f);
                SetPrivateField(attack, "radius", 0.45f);
                SetPrivateField(telegraphStyle, "groundTelegraphVisualPrefab", telegraphTemplate);
                SetPrivateField(telegraphStyle, "engageTelegraphMaterial", engageMaterial);
                SetPrivateField(telegraphStyle, "attackTelegraphMaterial", attackMaterial);
                SetPrivateField(telegraphStyle, "engageTelegraphColor", engageColor);
                SetPrivateField(telegraphStyle, "attackTelegraphColor", attackColor);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                bossObject.transform.position = Vector3.zero;
                targetObject.transform.position = new Vector3(0f, 0f, 3f);

                EnemyAttackController controller = bossObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "attackController", controller);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);
                SetPrivateField(bossBrain, "currentTarget", targetObject.transform);

                BossGroundTelegraphPresenter presenter = presenterObject.AddComponent<BossGroundTelegraphPresenter>();
                presenter.Configure(bossBrain, telegraphStyle);
                SetPrivateField(presenter, "groundOffset", 0.1f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyEngageState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.AreSame(telegraphTemplate, GetPrivateField<GameObject>(presenter, "currentVisualTemplate"));
                Assert.AreSame(engageMaterial, GetPrivateField<Material>(presenter, "currentMaterialTemplate"));
                AssertColorApproximately(engageColor, GetPrivateField<Material>(presenter, "telegraphMaterial").color);
                Assert.IsNotNull(GetPrivateField<GameObject>(presenter, "telegraphVisual").transform.Find("GroundTelegraphCore"));

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.AreSame(attackMaterial, GetPrivateField<Material>(presenter, "currentMaterialTemplate"));
                AssertColorApproximately(attackColor, GetPrivateField<Material>(presenter, "telegraphMaterial").color);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(telegraphTemplate);
                Object.DestroyImmediate(engageMaterial);
                Object.DestroyImmediate(attackMaterial);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(telegraphStyle);
                Object.DestroyImmediate(attack);
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

        private static void AssertVector3Approximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
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
