using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossImpactMarkerPresenterTests
    {
        [Test]
        public void BossImpactMarkerPresenter_ShowsProjectedImpactPointForBossAttack()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject targetObject = new GameObject("Player_Target");
            GameObject presenterObject = new GameObject("BossImpactMarkerPresenter");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attackDistance", 1.5f);
                SetPrivateField(bossArchetype, "attacks", new[] { attack });
                SetPrivateField(attack, "range", 2.4f);
                SetPrivateField(attack, "radius", 0.75f);
                SetPrivateField(attack, "startupSeconds", 0.35f);

                bossObject.transform.position = Vector3.zero;
                targetObject.transform.position = new Vector3(0f, 0f, 4f);

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

                BossImpactMarkerPresenter presenter = presenterObject.AddComponent<BossImpactMarkerPresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "groundOffset", 0.1f);
                SetPrivateField(presenter, "minimumLifetimeSeconds", 0.2f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyIdleGuardState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossImpactMarkerPresenter.MarkerShape.ImpactCircle, presenter.CurrentShape);
                Assert.AreEqual(0.75f, presenter.CurrentRadius, 0.001f);
                Assert.AreEqual(new Vector3(0f, 0.1f, 2.4f), presenter.CurrentPosition);

                InvokeMethod(presenter, "Tick", 0.4f);
                Assert.IsFalse(presenter.IsVisible);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                targetObject.transform.position = new Vector3(2f, 0f, 0f);
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(new Vector3(2.4f, 0.1f, 0f), presenter.CurrentPosition);
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
        public void BossImpactMarkerPresenter_UsesLaneForStraightProjectiles_AndCircleForArcShots()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO straightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO arcAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject targetObject = new GameObject("Player_Target");
            GameObject presenterObject = new GameObject("BossImpactMarkerPresenter");
            GameObject straightProjectilePrefab = new GameObject("StraightProjectilePrefab");
            GameObject arcProjectilePrefab = new GameObject("ArcProjectilePrefab");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attackDistance", 1.5f);

                SetPrivateField(straightAttack, "range", 4.7f);
                SetPrivateField(straightAttack, "radius", 0.4f);
                SetPrivateField(straightAttack, "startupSeconds", 0.26f);
                SetPrivateField(straightAttack, "projectilePrefab", straightProjectilePrefab);
                SetPrivateField(straightAttack, "projectileTrajectoryMode", ProjectileTrajectoryMode.Straight);

                SetPrivateField(arcAttack, "range", 5.2f);
                SetPrivateField(arcAttack, "radius", 0.45f);
                SetPrivateField(arcAttack, "startupSeconds", 0.34f);
                SetPrivateField(arcAttack, "projectilePrefab", arcProjectilePrefab);
                SetPrivateField(arcAttack, "projectileTrajectoryMode", ProjectileTrajectoryMode.Arc);

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

                BossImpactMarkerPresenter presenter = presenterObject.AddComponent<BossImpactMarkerPresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "groundOffset", 0.1f);
                SetPrivateField(presenter, "minimumLifetimeSeconds", 0.2f);

                SetPrivateField(bossArchetype, "attacks", new[] { straightAttack });
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossImpactMarkerPresenter.MarkerShape.AttackLane, presenter.CurrentShape);
                Assert.AreEqual(4.7f, presenter.CurrentLength, 0.001f);
                Assert.AreEqual(new Vector3(0f, 0.1f, 2.35f), presenter.CurrentPosition);
                AssertVector3Approximately(Vector3.forward, presenter.CurrentDirection);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                SetPrivateField(bossArchetype, "attacks", new[] { arcAttack });
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(BossImpactMarkerPresenter.MarkerShape.ImpactCircle, presenter.CurrentShape);
                Assert.AreEqual(0.45f, presenter.CurrentRadius, 0.001f);
                Assert.AreEqual(new Vector3(0f, 0.1f, 5.2f), presenter.CurrentPosition);
            }
            finally
            {
                Object.DestroyImmediate(arcProjectilePrefab);
                Object.DestroyImmediate(straightProjectilePrefab);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(arcAttack);
                Object.DestroyImmediate(straightAttack);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        [Test]
        public void BossImpactMarkerPresenter_UsesStylePrefabAndMaterial()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            BossTelegraphStyleSO telegraphStyle = ScriptableObject.CreateInstance<BossTelegraphStyleSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject targetObject = new GameObject("Player_Target");
            GameObject presenterObject = new GameObject("BossImpactMarkerPresenter");
            GameObject markerTemplate = new GameObject("StyledImpactMarkerTemplate");
            Material markerMaterial = null;

            try
            {
                Color markerColor = new Color(0.98f, 0.36f, 0.24f, 0.95f);
                markerMaterial = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
                GameObject markerCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
                markerCore.name = "ImpactMarkerCore";
                markerCore.transform.SetParent(markerTemplate.transform, false);
                Object.DestroyImmediate(markerCore.GetComponent<Collider>());

                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attackDistance", 1.5f);
                SetPrivateField(bossArchetype, "attacks", new[] { attack });
                SetPrivateField(attack, "range", 2.4f);
                SetPrivateField(attack, "radius", 0.6f);
                SetPrivateField(attack, "startupSeconds", 0.35f);
                SetPrivateField(telegraphStyle, "impactMarkerVisualPrefab", markerTemplate);
                SetPrivateField(telegraphStyle, "impactMarkerMaterial", markerMaterial);
                SetPrivateField(telegraphStyle, "impactMarkerColor", markerColor);

                bossObject.transform.position = Vector3.zero;
                targetObject.transform.position = new Vector3(0f, 0f, 4f);

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

                BossImpactMarkerPresenter presenter = presenterObject.AddComponent<BossImpactMarkerPresenter>();
                presenter.Configure(bossBrain, telegraphStyle);
                SetPrivateField(presenter, "groundOffset", 0.1f);
                SetPrivateField(presenter, "minimumLifetimeSeconds", 0.2f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.AreSame(markerTemplate, GetPrivateField<GameObject>(presenter, "currentVisualTemplate"));
                Assert.AreSame(markerMaterial, GetPrivateField<Material>(presenter, "currentMaterialTemplate"));
                AssertColorApproximately(markerColor, GetPrivateField<Material>(presenter, "markerMaterial").color);
                Assert.IsNotNull(GetPrivateField<GameObject>(presenter, "markerVisual").transform.Find("ImpactMarkerCore"));
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(markerTemplate);
                Object.DestroyImmediate(markerMaterial);
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
