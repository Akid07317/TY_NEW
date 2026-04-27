using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Camera;
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
                Assert.AreEqual("Block or step out", presenter.CurrentResponseHint);
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
                Assert.AreEqual("Sidestep line shot", presenter.CurrentResponseHint);
                Assert.AreEqual(new Color(0.17f, 0.76f, 0.91f, 1f), presenter.CurrentCueAccentColor);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                SetPrivateField(bossArchetype, "attacks", new[] { arcAttack });
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Arc Shot Incoming", presenter.CurrentCueLabel);
                Assert.AreEqual("Core Bolt", presenter.CurrentAttackName);
                Assert.AreEqual("Leave marked impact", presenter.CurrentResponseHint);
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
        public void BossAttackCuePresenter_UsesResponseAwareCueLabelsForAntiAirAndRollCatch()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            BossTelegraphStyleSO telegraphStyle = ScriptableObject.CreateInstance<BossTelegraphStyleSO>();
            AttackDefinitionSO antiAirAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO chaseRollAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossAttackCuePresenter");
            GameObject antiAirProjectilePrefab = new GameObject("AntiAirProjectilePrefab");

            try
            {
                Color antiAirColor = new Color(0.29f, 0.66f, 1f, 1f);
                Color chaseRollColor = new Color(1f, 0.35f, 0.19f, 1f);
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attacks", new[] { antiAirAttack });
                SetPrivateField(telegraphStyle, "antiAirCueAccentColor", antiAirColor);
                SetPrivateField(telegraphStyle, "chaseRollCueAccentColor", chaseRollColor);

                SetPrivateField(antiAirAttack, "attackId", "Enemy_Gatekeeper_SkyHook");
                SetPrivateField(antiAirAttack, "displayName", "Sky Hook");
                SetPrivateField(antiAirAttack, "startupSeconds", 0.22f);
                SetPrivateField(antiAirAttack, "projectilePrefab", antiAirProjectilePrefab);
                SetPrivateField(antiAirAttack, "projectileTrajectoryMode", ProjectileTrajectoryMode.Straight);
                SetPrivateField(antiAirAttack, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);

                SetPrivateField(chaseRollAttack, "attackId", "Enemy_Gatekeeper_RollCatcher");
                SetPrivateField(chaseRollAttack, "displayName", "Pursuit Slam");
                SetPrivateField(chaseRollAttack, "startupSeconds", 0.28f);
                SetPrivateField(chaseRollAttack, "range", 4.25f);
                SetPrivateField(chaseRollAttack, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);

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
                Assert.AreEqual("Anti-Air Incoming", presenter.CurrentCueLabel);
                Assert.AreEqual("Sky Hook", presenter.CurrentAttackName);
                Assert.AreEqual("Land or guard; avoid air hang", presenter.CurrentResponseHint);
                Assert.AreEqual(antiAirColor, presenter.CurrentCueAccentColor);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                SetPrivateField(bossArchetype, "attacks", new[] { chaseRollAttack });
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(presenter.IsVisible);
                Assert.AreEqual("Roll Catch Incoming", presenter.CurrentCueLabel);
                Assert.AreEqual("Pursuit Slam", presenter.CurrentAttackName);
                Assert.AreEqual("Delay dodge; lane catches rolls", presenter.CurrentResponseHint);
                Assert.AreEqual(chaseRollColor, presenter.CurrentCueAccentColor);
            }
            finally
            {
                Object.DestroyImmediate(antiAirProjectilePrefab);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(chaseRollAttack);
                Object.DestroyImmediate(antiAirAttack);
                Object.DestroyImmediate(telegraphStyle);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        [Test]
        public void BossAttackCuePresenter_RequestsCameraImpulse_ForResponseCue()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO antiAirAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO chaseRollAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject presenterObject = new GameObject("BossAttackCuePresenter");
            GameObject cameraObject = new GameObject("Camera");
            GameObject antiAirProjectilePrefab = new GameObject("AntiAirProjectilePrefab");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attacks", new[] { antiAirAttack });
                SetPrivateField(antiAirAttack, "displayName", "Sky Hook");
                SetPrivateField(antiAirAttack, "projectilePrefab", antiAirProjectilePrefab);
                SetPrivateField(antiAirAttack, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);
                SetPrivateField(chaseRollAttack, "displayName", "Pursuit Slam");
                SetPrivateField(chaseRollAttack, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);
                EnemyAttackController controller = bossObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
                InvokeMethod(controller, "Awake");
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "attackController", controller);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);

                BossAttackCuePresenter presenter = presenterObject.AddComponent<BossAttackCuePresenter>();
                SetPrivateField(presenter, "bossEnemy", bossBrain);
                SetPrivateField(presenter, "cameraController", cameraController);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);
                Assert.IsTrue(cameraController.HasActiveImpactImpulse);

                cameraController.ResetRuntimeState();
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));
                InvokeMethod(presenter, "Tick", 0f);
                SetPrivateField(bossArchetype, "attacks", new[] { chaseRollAttack });
                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));
                InvokeMethod(presenter, "Tick", 0f);

                Assert.IsTrue(cameraController.HasActiveImpactImpulse);
            }
            finally
            {
                Object.DestroyImmediate(antiAirProjectilePrefab);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(chaseRollAttack);
                Object.DestroyImmediate(antiAirAttack);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        [Test]
        public void BossAttackCuePresenter_LayoutKeepsHintInsideSafeTopPanel()
        {
            BossAttackCueLayout smallLayout = BossAttackCueLayoutUtility.Build(320f);
            BossAttackCueLayout narrowLayout = BossAttackCueLayoutUtility.Build(240f);
            BossAttackCueLayout wideLayout = BossAttackCueLayoutUtility.Build(1920f);
            BossAttackCueLayout shortLayout = BossAttackCueLayoutUtility.Build(320f, 144f);

            Assert.That(smallLayout.PanelRect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(smallLayout.PanelRect.xMax, Is.LessThanOrEqualTo(320f));
            Assert.That(smallLayout.ResponseHintRect.xMin, Is.GreaterThan(smallLayout.PanelRect.xMin));
            Assert.That(smallLayout.ResponseHintRect.xMax, Is.LessThan(smallLayout.PanelRect.xMax));
            Assert.That(smallLayout.AttackNameRect.yMax, Is.LessThanOrEqualTo(smallLayout.ResponseHintRect.yMin));

            Assert.That(narrowLayout.PanelRect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(narrowLayout.PanelRect.xMax, Is.LessThanOrEqualTo(240f));
            Assert.That(wideLayout.PanelRect.width, Is.LessThanOrEqualTo(390f));
            Assert.That(shortLayout.PanelRect.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(shortLayout.PanelRect.yMax, Is.LessThanOrEqualTo(144f));
            Assert.That(shortLayout.ResponseHintRect.yMax, Is.LessThanOrEqualTo(shortLayout.PanelRect.yMax));
        }

        [Test]
        public void BossAttackCuePresenter_CompactsLongAttackNamesForNarrowTopCue()
        {
            BossAttackCueLayout narrowLayout = BossAttackCueLayoutUtility.Build(240f, 144f);
            string compactName = BossAttackCueTextUtility.BuildAttackNameLine(
                "Gatekeeper Pursuit Slam Cinematic Follow Through",
                narrowLayout.AttackNameRect.width,
                24);
            int characterBudget = BossAttackCueTextUtility.CalculateAttackNameCharacterBudget(
                narrowLayout.AttackNameRect.width,
                24);

            Assert.That(compactName.Length, Is.LessThanOrEqualTo(characterBudget));
            Assert.That(compactName, Does.Contain("..."));
            Assert.That(compactName, Does.StartWith("Gate"));
            Assert.That(compactName, Does.EndWith("rough"));
            Assert.AreEqual(
                "Pursuit Slam",
                BossAttackCueTextUtility.BuildAttackNameLine(
                    "  Pursuit   Slam  ",
                    narrowLayout.AttackNameRect.width,
                    24));
        }

        [Test]
        public void BossAttackCuePresenter_CompactsResponseHintsForNarrowTopCue()
        {
            BossAttackCueLayout narrowLayout = BossAttackCueLayoutUtility.Build(240f, 144f);
            string compactHint = BossAttackCueTextUtility.BuildResponseHintLine(
                "Delay dodge; lane catches rolls",
                narrowLayout.ResponseHintRect.width,
                13);
            int characterBudget = BossAttackCueTextUtility.CalculateResponseHintCharacterBudget(
                narrowLayout.ResponseHintRect.width,
                13);

            Assert.That(compactHint.Length, Is.LessThanOrEqualTo(characterBudget));
            Assert.AreEqual("Delay dodge; lane", compactHint);
            Assert.AreEqual(
                "Sidestep shot",
                BossAttackCueTextUtility.BuildResponseHintLine(
                    "  Sidestep   line shot  ",
                    narrowLayout.ResponseHintRect.width,
                    13));

            string unknownLongHint = BossAttackCueTextUtility.BuildResponseHintLine(
                "Very long local preview response instruction that should compact",
                80f,
                13);
            Assert.That(
                unknownLongHint.Length,
                Is.LessThanOrEqualTo(BossAttackCueTextUtility.CalculateResponseHintCharacterBudget(80f, 13)));
            Assert.That(unknownLongHint, Does.Contain("..."));
        }

        [Test]
        public void BossAttackCuePresenter_StylesClipTextInsideFormalPanel()
        {
            GUIStyle labelStyle = BossAttackCueStyleUtility.BuildCueLabelStyle(new GUIStyle(), Color.yellow);
            GUIStyle nameStyle = BossAttackCueStyleUtility.BuildAttackNameStyle(new GUIStyle());
            GUIStyle hintStyle = BossAttackCueStyleUtility.BuildResponseHintStyle(new GUIStyle());

            Assert.AreEqual(TextClipping.Clip, labelStyle.clipping);
            Assert.AreEqual(TextClipping.Clip, nameStyle.clipping);
            Assert.AreEqual(TextClipping.Clip, hintStyle.clipping);
            Assert.IsFalse(labelStyle.wordWrap);
            Assert.IsFalse(nameStyle.wordWrap);
            Assert.IsFalse(hintStyle.wordWrap);
            Assert.AreEqual(Color.yellow, labelStyle.normal.textColor);
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
                SetPrivateField(closeAttack, "breaksGuard", true);
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
                Assert.AreEqual("Guard Break Incoming", presenter.CurrentCueLabel);
                Assert.AreEqual("Gate Slam", presenter.CurrentAttackName);
                Assert.AreEqual("Dodge heavy; guard breaks", presenter.CurrentResponseHint);
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
