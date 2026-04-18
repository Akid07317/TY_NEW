using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossAttackCuePresenterTests
    {
        [Test]
        public void BossAttackCuePresenter_ShowsWhenBossEntersAttackState()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossAttackCuePresenter");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attacks", new[] { attack });

                SetPrivateField(attack, "attackId", "Enemy_Gatekeeper_Reach");
                SetPrivateField(attack, "displayName", "Hall Sweep");
                SetPrivateField(attack, "startupSeconds", 0.4f);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyAttackController controller = bossObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                InvokeMethod(controller, "Awake");
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "attackController", controller);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);

                BossAttackCuePresenter presenter = presenterObject.AddComponent<BossAttackCuePresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "minimumVisibleSeconds", 0.25f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyIdleGuardState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsFalse(presenter.IsVisible);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Incoming Attack", presenter.CurrentCueLabel);
                Assert.AreEqual("Hall Sweep", presenter.CurrentAttackName);
                Assert.AreEqual(0.4f, presenter.RemainingVisibleSeconds, 0.001f);

                InvokeMethod(presenter, "Tick", 0.2f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual(0.2f, presenter.RemainingVisibleSeconds, 0.001f);

                InvokeMethod(presenter, "Tick", 0.25f);
                Assert.IsFalse(presenter.IsVisible);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Hall Sweep", presenter.CurrentAttackName);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        [Test]
        public void BossAttackCuePresenter_UsesTrajectoryAwareCueLabelsForProjectileAttacks()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            BossTelegraphStyleSO telegraphStyle = ScriptableObject.CreateInstance<BossTelegraphStyleSO>();
            AttackDefinitionSO straightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO arcAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossAttackCuePresenter");
            GameObject straightProjectilePrefab = new GameObject("StraightProjectilePrefab");
            GameObject arcProjectilePrefab = new GameObject("ArcProjectilePrefab");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attacks", new[] { straightAttack });
                SetPrivateField(telegraphStyle, "straightProjectileCueAccentColor", new Color(0.17f, 0.76f, 0.91f, 1f));
                SetPrivateField(telegraphStyle, "arcProjectileCueAccentColor", new Color(0.94f, 0.44f, 0.21f, 1f));

                SetPrivateField(straightAttack, "attackId", "Enemy_Gatekeeper_Burst");
                SetPrivateField(straightAttack, "displayName", "Gate Lance");
                SetPrivateField(straightAttack, "startupSeconds", 0.26f);
                SetPrivateField(straightAttack, "projectilePrefab", straightProjectilePrefab);
                SetPrivateField(straightAttack, "projectileTrajectoryMode", ProjectileTrajectoryMode.Straight);

                SetPrivateField(arcAttack, "attackId", "Enemy_Gatekeeper_Arc");
                SetPrivateField(arcAttack, "displayName", "Core Bolt");
                SetPrivateField(arcAttack, "startupSeconds", 0.34f);
                SetPrivateField(arcAttack, "projectilePrefab", arcProjectilePrefab);
                SetPrivateField(arcAttack, "projectileTrajectoryMode", ProjectileTrajectoryMode.Arc);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyAttackController controller = bossObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                InvokeMethod(controller, "Awake");
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "attackController", controller);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);

                BossAttackCuePresenter presenter = presenterObject.AddComponent<BossAttackCuePresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "telegraphStyle", telegraphStyle);
                SetPrivateField(presenter, "minimumVisibleSeconds", 0.25f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Line Shot Incoming", presenter.CurrentCueLabel);
                Assert.AreEqual("Gate Lance", presenter.CurrentAttackName);
                Assert.AreEqual(new Color(0.17f, 0.76f, 0.91f, 1f), presenter.CurrentCueAccentColor);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                SetPrivateField(bossArchetype, "attacks", new[] { arcAttack });
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Arc Shot Incoming", presenter.CurrentCueLabel);
                Assert.AreEqual("Core Bolt", presenter.CurrentAttackName);
                Assert.AreEqual(new Color(0.94f, 0.44f, 0.21f, 1f), presenter.CurrentCueAccentColor);
            }
            finally
            {
                Object.DestroyImmediate(arcProjectilePrefab);
                Object.DestroyImmediate(straightProjectilePrefab);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(arcAttack);
                Object.DestroyImmediate(straightAttack);
                Object.DestroyImmediate(telegraphStyle);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        [Test]
        public void BossAttackCuePresenter_UsesBossTargetAwarePreviewForFarTargets()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO closeAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO projectileAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject targetObject = new GameObject("Player_Target");
            GameObject presenterObject = new GameObject("BossAttackCuePresenter");
            DamageableReceiver targetReceiver = null;

            try
            {
                targetObject.transform.position = new Vector3(0f, 0f, 1.3f);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attacks", new[] { closeAttack, projectileAttack });
                SetPrivateField(closeAttack, "attackId", "Enemy_Gatekeeper");
                SetPrivateField(closeAttack, "displayName", "Gate Slam");
                SetPrivateField(closeAttack, "startupSeconds", 0.2f);
                SetPrivateField(closeAttack, "range", 1.8f);
                SetPrivateField(closeAttack, "radius", 0.35f);
                SetPrivateField(projectileAttack, "attackId", "Enemy_Gatekeeper_Arc");
                SetPrivateField(projectileAttack, "displayName", "Arc Bolt");
                SetPrivateField(projectileAttack, "startupSeconds", 0.45f);
                SetPrivateField(projectileAttack, "range", 4.5f);
                SetPrivateField(projectileAttack, "radius", 0.35f);

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
                InvokeMethod(targetReceiver, "Awake");
                Physics.SyncTransforms();

                BossAttackCuePresenter presenter = presenterObject.AddComponent<BossAttackCuePresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "minimumVisibleSeconds", 0.25f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Gate Slam", presenter.CurrentAttackName);
                Assert.AreEqual(0.25f, presenter.RemainingVisibleSeconds, 0.001f);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                targetObject.transform.position = new Vector3(0f, 0f, 4.5f);
                Physics.SyncTransforms();
                Assert.AreSame(projectileAttack, controller.PreviewAttackForTarget(targetObject.transform, bossArchetype));
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Arc Bolt", presenter.CurrentAttackName);
                Assert.AreEqual(0.45f, presenter.RemainingVisibleSeconds, 0.001f);
            }
            finally
            {
                GameObject projectilePrefab = (GameObject)GetPrivateField(projectileAttack, "projectilePrefab");

                if (projectilePrefab != null)
                {
                    Object.DestroyImmediate(projectilePrefab);
                }

                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(projectileAttack);
                Object.DestroyImmediate(closeAttack);
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

        private static object GetPrivateField(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return field.GetValue(instance);
        }
    }
}
