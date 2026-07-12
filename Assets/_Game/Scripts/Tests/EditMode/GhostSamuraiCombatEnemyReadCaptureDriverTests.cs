using System;
using System.Linq;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class GhostSamuraiCombatEnemyReadCaptureDriverTests
    {
        [Test]
        public void ParseRequest_RecognizesDefaultRangedAndGuardInputModes()
        {
            object[] args = { "enemyread-scene 1714412000", null, null, null };

            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("Default", args[1]?.ToString());
            Assert.AreEqual(false, args[2]);
            Assert.AreEqual(true, args[3]);

            args = new object[] { "enemyread-ranged-clean 1714412001", null, null, null };
            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("RangedVariants", args[1]?.ToString());
            Assert.AreEqual(true, args[2]);
            Assert.AreEqual(false, args[3]);

            args = new object[] { "enemyread-guard-input-scene 1714412002", null, null, null };
            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("GuardInputValidation", args[1]?.ToString());
            Assert.AreEqual(false, args[2]);
            Assert.AreEqual(true, args[3]);
        }

        [Test]
        public void ResolveCaptureSteps_DefaultScenarioRunsCombatTestEnemyReadSequenceInDocumentedOrder()
        {
            object previousScenario = DriverScenarioField.GetValue(null);

            try
            {
                DriverScenarioField.SetValue(null, Enum.Parse(CaptureScenarioType, "Default"));
                Array steps = ResolveCaptureStepsMethod.Invoke(null, null) as Array;

                Assert.IsNotNull(steps);
                Assert.AreEqual(3, steps.Length);

                string[] labels = steps.Cast<object>()
                    .Select(step => CaptureStepLabelProperty.GetValue(step) as string)
                    .ToArray();
                string[] kinds = steps.Cast<object>()
                    .Select(step => CaptureStepKindProperty.GetValue(step)?.ToString())
                    .ToArray();
                string[] commands = steps.Cast<object>()
                    .Select(step => CaptureStepCommandProperty.GetValue(step)?.ToString())
                    .ToArray();

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "EnemyMelee / Guard Swing",
                        "EnemyMobile / Feint Dash",
                        "EnemyRanged / Arc Bolt"
                    },
                    labels);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "EnemyMelee",
                        "EnemyMobile",
                        "EnemyRanged"
                    },
                    kinds);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "Default",
                        "Default",
                        "Default"
                    },
                    commands);
            }
            finally
            {
                DriverScenarioField.SetValue(null, previousScenario);
            }
        }

        [Test]
        public void ResolveCaptureSteps_RangedVariantScenarioRunsBowResponseReadSequence()
        {
            object previousScenario = DriverScenarioField.GetValue(null);

            try
            {
                DriverScenarioField.SetValue(null, Enum.Parse(CaptureScenarioType, "RangedVariants"));
                Array steps = ResolveCaptureStepsMethod.Invoke(null, null) as Array;

                Assert.IsNotNull(steps);
                Assert.AreEqual(3, steps.Length);

                string[] labels = steps.Cast<object>()
                    .Select(step => CaptureStepLabelProperty.GetValue(step) as string)
                    .ToArray();
                string[] commands = steps.Cast<object>()
                    .Select(step => CaptureStepCommandProperty.GetValue(step)?.ToString())
                    .ToArray();
                string[] prepActions = steps.Cast<object>()
                    .Select(step => CaptureStepPrepActionProperty.GetValue(step)?.ToString())
                    .ToArray();

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "EnemyRanged / Anti-Air Shot",
                        "EnemyRanged / Chase Roll Shot",
                        "EnemyRanged / Guard Break Shot"
                    },
                    labels);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "RangedAntiAir",
                        "RangedChaseRoll",
                        "RangedGuardBreak"
                    },
                    commands);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        "None",
                        "CombatRoll",
                        "Block"
                    },
                    prepActions);
            }
            finally
            {
                DriverScenarioField.SetValue(null, previousScenario);
            }
        }

        [Test]
        public void ResolveCaptureSteps_GuardInputScenarioTargetsOrdinaryMeleeGuardSwing()
        {
            object previousScenario = DriverScenarioField.GetValue(null);

            try
            {
                DriverScenarioField.SetValue(null, Enum.Parse(CaptureScenarioType, "GuardInputValidation"));
                Array steps = ResolveCaptureStepsMethod.Invoke(null, null) as Array;

                Assert.IsNotNull(steps);
                Assert.AreEqual(1, steps.Length);
                object step = steps.GetValue(0);
                Assert.AreEqual("EnemyMelee", CaptureStepKindProperty.GetValue(step)?.ToString());
                Assert.AreEqual("Default", CaptureStepCommandProperty.GetValue(step)?.ToString());
                Assert.AreEqual(1.50f, CaptureStepPlayerDistanceProperty.GetValue(step));
                StringAssert.Contains("Guard Input Validation", CaptureStepLabelProperty.GetValue(step) as string);
            }
            finally
            {
                DriverScenarioField.SetValue(null, previousScenario);
            }
        }

        [Test]
        public void GuardInputCaptureSteps_RequireStartupFailureBeforeActiveGuardSuccess()
        {
            Array steps = GuardInputCaptureStepsField.GetValue(null) as Array;

            Assert.IsNotNull(steps);
            Assert.AreEqual(8, steps.Length);

            string[] commands = steps.Cast<object>()
                .Select(step => GuardInputCaptureStepCommandProperty.GetValue(step)?.ToString())
                .ToArray();
            float[] minimumTimes = steps.Cast<object>()
                .Select(step => (float)GuardInputCaptureStepMinimumElapsedSecondsProperty.GetValue(step))
                .ToArray();
            string[] labels = steps.Cast<object>()
                .Select(step => GuardInputCaptureStepLabelProperty.GetValue(step) as string)
                .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "PressStartupGuard",
                    "TriggerStartupAttack",
                    "ReleaseStartupGuard",
                    "ResetForActiveGuard",
                    "PressActiveGuard",
                    "TriggerActiveGuardAttack",
                    "ReleaseActiveGuard",
                    "RecordResult"
                },
                commands);
            CollectionAssert.AreEqual(
                new[] { 0.10f, 0.10f, 0.20f, 0.35f, 0.45f, 0.45f, 0.95f, 1.10f },
                minimumTimes);
            StringAssert.Contains("<Keyboard>/leftCtrl", labels[0]);
            StringAssert.Contains("startup", labels[1].ToLowerInvariant());
            StringAssert.Contains("active guard", labels[5].ToLowerInvariant());
        }

        [Test]
        public void IsExpectedGuardInputCommit_RequiresPlayerTargetMeleeArchetypeAttackIdAndPositiveDamage()
        {
            GameObject expectedTargetObject = new GameObject("ExpectedPlayerTarget");
            GameObject otherTargetObject = new GameObject("OtherTarget");
            EnemyArchetypeSO expectedArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            EnemyArchetypeSO otherArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO expectedAttack = CreateAttack("Enemy_Melee");
            AttackDefinitionSO otherAttack = CreateAttack("Enemy_Mobile");

            try
            {
                AssertExpectedCommit(
                    new EnemyAttackCommit(expectedTargetObject.transform, expectedArchetype, expectedAttack, 12f),
                    expectedTargetObject.transform,
                    expectedArchetype,
                    true);
                AssertExpectedCommit(
                    new EnemyAttackCommit(otherTargetObject.transform, expectedArchetype, expectedAttack, 12f),
                    expectedTargetObject.transform,
                    expectedArchetype,
                    false);
                AssertExpectedCommit(
                    new EnemyAttackCommit(expectedTargetObject.transform, otherArchetype, expectedAttack, 12f),
                    expectedTargetObject.transform,
                    expectedArchetype,
                    false);
                AssertExpectedCommit(
                    new EnemyAttackCommit(expectedTargetObject.transform, expectedArchetype, otherAttack, 12f),
                    expectedTargetObject.transform,
                    expectedArchetype,
                    false);
                AssertExpectedCommit(
                    new EnemyAttackCommit(expectedTargetObject.transform, expectedArchetype, expectedAttack, 0f),
                    expectedTargetObject.transform,
                    expectedArchetype,
                    false);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expectedTargetObject);
                UnityEngine.Object.DestroyImmediate(otherTargetObject);
                UnityEngine.Object.DestroyImmediate(expectedArchetype);
                UnityEngine.Object.DestroyImmediate(otherArchetype);
                UnityEngine.Object.DestroyImmediate(expectedAttack);
                UnityEngine.Object.DestroyImmediate(otherAttack);
            }
        }

        [Test]
        public void GuardInputVitals_RequireStartupDamageThenActiveZeroDamageWithCounterWindow()
        {
            Assert.AreEqual(
                true,
                MatchesStartupGuardFailureVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 88f, 12f, 0f, 0f, false }));
            Assert.AreEqual(
                false,
                MatchesStartupGuardFailureVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 100f, 12f, 0f, 0f, false }));
            Assert.AreEqual(
                false,
                MatchesStartupGuardFailureVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 88f, 12f, 0f, 20f, false }));
            Assert.AreEqual(
                false,
                MatchesStartupGuardFailureVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 88f, 12f, 0f, 0f, true }));

            Assert.AreEqual(
                true,
                MatchesActiveGuardBlockVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 100f, 0f, 20f, 20f, true }));
            Assert.AreEqual(
                false,
                MatchesActiveGuardBlockVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 88f, 0f, 20f, 20f, true }));
            Assert.AreEqual(
                false,
                MatchesActiveGuardBlockVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 100f, 0f, 20f, 20f, false }));
            Assert.AreEqual(
                false,
                MatchesActiveGuardBlockVitalsMethod.Invoke(
                    null,
                    new object[] { 100f, 100f, 0f, 10f, 20f, true }));
        }

        private static AttackDefinitionSO CreateAttack(string attackId)
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            typeof(AttackDefinitionSO).GetField("attackId", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(attack, attackId);
            return attack;
        }

        private static void AssertExpectedCommit(
            EnemyAttackCommit commit,
            Transform expectedTarget,
            EnemyArchetypeSO expectedArchetype,
            bool expected)
        {
            Assert.AreEqual(
                expected,
                IsExpectedGuardInputCommitMethod.Invoke(
                    null,
                    new object[] { commit, expectedTarget, expectedArchetype }));
        }

        private static MethodInfo ParseRequestMethod =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetMethod(
                "ParseRequest",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo ResolveCaptureStepsMethod =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetMethod(
                "ResolveCaptureSteps",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static FieldInfo DriverScenarioField =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetField(
                "driverScenario",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static Type CaptureScenarioType =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetNestedType(
                "CaptureScenario",
                BindingFlags.NonPublic);

        private static Type CaptureStepType =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetNestedType(
                "CaptureStep",
                BindingFlags.NonPublic);

        private static PropertyInfo CaptureStepLabelProperty =>
            CaptureStepType.GetProperty("Label", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo CaptureStepKindProperty =>
            CaptureStepType.GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo CaptureStepCommandProperty =>
            CaptureStepType.GetProperty("Command", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo CaptureStepPrepActionProperty =>
            CaptureStepType.GetProperty("PrepAction", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo CaptureStepPlayerDistanceProperty =>
            CaptureStepType.GetProperty("PlayerDistance", BindingFlags.Instance | BindingFlags.Public);

        private static FieldInfo GuardInputCaptureStepsField =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetField(
                "GuardInputCaptureSteps",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static Type GuardInputCaptureStepType =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetNestedType(
                "GuardInputCaptureStep",
                BindingFlags.NonPublic);

        private static PropertyInfo GuardInputCaptureStepMinimumElapsedSecondsProperty =>
            GuardInputCaptureStepType.GetProperty("MinimumElapsedSeconds", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo GuardInputCaptureStepCommandProperty =>
            GuardInputCaptureStepType.GetProperty("Command", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo GuardInputCaptureStepLabelProperty =>
            GuardInputCaptureStepType.GetProperty("Label", BindingFlags.Instance | BindingFlags.Public);

        private static MethodInfo IsExpectedGuardInputCommitMethod =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetMethod(
                "IsExpectedGuardInputCommit",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo MatchesStartupGuardFailureVitalsMethod =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetMethod(
                "MatchesStartupGuardFailureVitals",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo MatchesActiveGuardBlockVitalsMethod =>
            typeof(GhostSamuraiCombatEnemyReadCaptureDriverMenu).GetMethod(
                "MatchesActiveGuardBlockVitals",
                BindingFlags.Static | BindingFlags.NonPublic);
    }
}
