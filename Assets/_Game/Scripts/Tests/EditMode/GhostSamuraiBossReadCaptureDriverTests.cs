using System;
using System.Linq;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class GhostSamuraiBossReadCaptureDriverTests
    {
        [Test]
        public void ParseRequest_RecognizesCleanAndSceneModes()
        {
            object[] args = { "bossread-scene 1714410000", null, null };

            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual(false, args[1]);
            Assert.AreEqual(true, args[2]);

            args = new object[] { "bossread-clean 1714410001", null, null };
            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual(true, args[1]);
            Assert.AreEqual(false, args[2]);
        }

        [Test]
        public void ParseScenario_RecognizesGateSlamInputModes()
        {
            Assert.AreEqual(
                "GateSlamGuardInput",
                ParseScenarioMethod.Invoke(null, new object[] { "bossread-guard-input" })?.ToString());
            Assert.AreEqual(
                "GateSlamDodgeInput",
                ParseScenarioMethod.Invoke(null, new object[] { "bossread-dodge-input" })?.ToString());
            Assert.AreEqual(
                "ReadSequence",
                ParseScenarioMethod.Invoke(null, new object[] { "bossread-clean" })?.ToString());
        }

        [Test]
        public void GateSlamInputSteps_UsePhysicalKeyboardBindingsAndRecordResults()
        {
            Array guardSteps = GateSlamGuardInputStepsField.GetValue(null) as Array;
            Array dodgeSteps = GateSlamDodgeInputStepsField.GetValue(null) as Array;

            Assert.IsNotNull(guardSteps);
            Assert.IsNotNull(dodgeSteps);
            Assert.AreEqual(4, guardSteps.Length);
            Assert.AreEqual(4, dodgeSteps.Length);

            CollectionAssert.AreEqual(
                new[] { "PressGuard", "TriggerGateSlam", "ReleaseGuard", "RecordResult" },
                guardSteps.Cast<object>()
                    .Select(step => InputCaptureStepCommandProperty.GetValue(step)?.ToString())
                    .ToArray());
            CollectionAssert.AreEqual(
                new[] { "TriggerGateSlam", "PressDodge", "ReleaseDodge", "RecordResult" },
                dodgeSteps.Cast<object>()
                    .Select(step => InputCaptureStepCommandProperty.GetValue(step)?.ToString())
                    .ToArray());

            StringAssert.Contains(
                "<Keyboard>/leftCtrl",
                InputCaptureStepLabelProperty.GetValue(guardSteps.GetValue(0)) as string);
            StringAssert.Contains(
                "<Keyboard>/leftShift",
                InputCaptureStepLabelProperty.GetValue(dodgeSteps.GetValue(1)) as string);
        }

        [Test]
        public void CaptureSteps_RunGatekeeperReadSequenceInDocumentedOrder()
        {
            Array steps = CaptureStepsField.GetValue(null) as Array;

            Assert.IsNotNull(steps);
            Assert.AreEqual(3, steps.Length);

            string[] labels = steps.Cast<object>()
                .Select(step => CaptureStepLabelProperty.GetValue(step) as string)
                .ToArray();
            string[] commands = steps.Cast<object>()
                .Select(step => CaptureStepCommandProperty.GetValue(step)?.ToString())
                .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Sky Hook / Anti-Air",
                    "Pursuit Slam / Roll Catch",
                    "Gate Slam / Guard Break"
                },
                labels);
            CollectionAssert.AreEqual(
                new[]
                {
                    "SkyHook",
                    "PursuitSlam",
                    "GateSlam"
                },
                commands);
        }

        [Test]
        public void FindBossBrain_IncludesInactiveGatekeeperEncounterMember()
        {
            GameObject bossObject = new GameObject("Boss_Gatekeeper");

            try
            {
                EnemyBrain boss = bossObject.AddComponent<EnemyBrain>();
                bossObject.SetActive(false);

                Assert.AreSame(boss, FindBossBrainMethod.Invoke(null, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bossObject);
            }
        }

        private static MethodInfo ParseRequestMethod =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetMethod(
                "ParseRequest",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo ParseScenarioMethod =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetMethod(
                "ParseScenario",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static FieldInfo CaptureStepsField =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetField(
                "CaptureSteps",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static FieldInfo GateSlamGuardInputStepsField =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetField(
                "GateSlamGuardInputSteps",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static FieldInfo GateSlamDodgeInputStepsField =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetField(
                "GateSlamDodgeInputSteps",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo FindBossBrainMethod =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetMethod(
                "FindBossBrain",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static Type CaptureStepType =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetNestedType(
                "CaptureStep",
                BindingFlags.NonPublic);

        private static Type InputCaptureStepType =>
            typeof(GhostSamuraiBossReadCaptureDriverMenu).GetNestedType(
                "InputCaptureStep",
                BindingFlags.NonPublic);

        private static PropertyInfo CaptureStepLabelProperty =>
            CaptureStepType.GetProperty("Label", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo CaptureStepCommandProperty =>
            CaptureStepType.GetProperty("Command", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo InputCaptureStepCommandProperty =>
            InputCaptureStepType.GetProperty("Command", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo InputCaptureStepLabelProperty =>
            InputCaptureStepType.GetProperty("Label", BindingFlags.Instance | BindingFlags.Public);
    }
}
