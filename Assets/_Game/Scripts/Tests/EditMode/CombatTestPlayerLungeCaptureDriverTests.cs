using System;
using System.Linq;
using System.Reflection;
using CampusRPG.EditorTools;
using NUnit.Framework;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatTestPlayerLungeCaptureDriverTests
    {
        [Test]
        public void ParseRequest_RecognizesIronGateBreakObservationModes()
        {
            object[] args = { "swordart-irongate-scene", null, null, null };

            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("SwordArtIronGateBreak", args[1]?.ToString());
            Assert.AreEqual(false, args[2]);
            Assert.AreEqual(true, args[3]);

            args = new object[] { "swordart-irongate-clean", null, null, null };
            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("SwordArtIronGateBreak", args[1]?.ToString());
            Assert.AreEqual(true, args[2]);
            Assert.AreEqual(false, args[3]);
        }

        [Test]
        public void ParseRequest_RecognizesFlankObservationModes()
        {
            object[] args = { "swordart-flank-scene", null, null, null };

            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("SwordArtFlank", args[1]?.ToString());
            Assert.AreEqual(false, args[2]);
            Assert.AreEqual(true, args[3]);

            args = new object[] { "swordart-flank-clean", null, null, null };
            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("SwordArtFlank", args[1]?.ToString());
            Assert.AreEqual(true, args[2]);
            Assert.AreEqual(false, args[3]);
        }

        [Test]
        public void ParseRequest_RecognizesAirHeavyObservationModes()
        {
            object[] args = { "swordart-airheavy-scene", null, null, null };

            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("SwordArtAirHeavy", args[1]?.ToString());
            Assert.AreEqual(false, args[2]);
            Assert.AreEqual(true, args[3]);

            args = new object[] { "swordart-airheavy-clean", null, null, null };
            ParseRequestMethod.Invoke(null, args);

            Assert.AreEqual("SwordArtAirHeavy", args[1]?.ToString());
            Assert.AreEqual(true, args[2]);
            Assert.AreEqual(false, args[3]);
        }

        [Test]
        public void ResolveCaptureSteps_IronGateBreakScenarioContainsBlockAndHeavyChainCases()
        {
            object previousScenario = DriverScenarioField.GetValue(null);

            try
            {
                object ironGateScenario = Enum.Parse(CaptureScenarioType, "SwordArtIronGateBreak");
                DriverScenarioField.SetValue(null, ironGateScenario);

                Array steps = ResolveCaptureStepsMethod.Invoke(null, null) as Array;

                Assert.IsNotNull(steps);
                Assert.AreEqual(5, steps.Length);

                string[] labels = steps.Cast<object>()
                    .Select(step => CaptureStepLabelProperty.GetValue(step) as string)
                    .ToArray();
                string[] commands = steps.Cast<object>()
                    .Select(step => CaptureStepCommandProperty.GetValue(step)?.ToString())
                    .ToArray();

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "Iron Gate Break hit / AfterBlock + Heavy",
                        "Heavy_01 hit / queue Iron Gate Break",
                        "Iron Gate Break hit / AfterHeavy + Heavy",
                        "Heavy_01 whiff / queue Iron Gate Break",
                        "Iron Gate Break whiff / AfterHeavy + Heavy"
                    },
                    labels);
                Assert.AreEqual("BlockIntoIronGateBreak", commands[0]);
                Assert.AreEqual("HeavyIntoIronGateBreak", commands[2]);
                Assert.AreEqual("HeavyIntoIronGateBreak", commands[4]);
            }
            finally
            {
                DriverScenarioField.SetValue(null, previousScenario);
            }
        }

        [Test]
        public void ResolveCaptureSteps_FlankScenarioContainsSidewindAndCrossStepCases()
        {
            object previousScenario = DriverScenarioField.GetValue(null);

            try
            {
                object flankScenario = Enum.Parse(CaptureScenarioType, "SwordArtFlank");
                DriverScenarioField.SetValue(null, flankScenario);

                Array steps = ResolveCaptureStepsMethod.Invoke(null, null) as Array;

                Assert.IsNotNull(steps);
                Assert.AreEqual(5, steps.Length);

                string[] labels = steps.Cast<object>()
                    .Select(step => CaptureStepLabelProperty.GetValue(step) as string)
                    .ToArray();
                string[] commands = steps.Cast<object>()
                    .Select(step => CaptureStepCommandProperty.GetValue(step)?.ToString())
                    .ToArray();

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "GroundDodge only / spacing reset",
                        "Sidewind Cut hit / Dodge Left + Light",
                        "Sidewind Cut whiff / Dodge Right + Light",
                        "Cross Step hit / Roll + Light",
                        "Cross Step whiff / Roll + Light"
                    },
                    labels);
                Assert.AreEqual("GroundDodgeOnly", commands[0]);
                Assert.AreEqual("DodgeLeftLightFollowUp", commands[1]);
                Assert.AreEqual("DodgeRightLightFollowUp", commands[2]);
                Assert.AreEqual("CombatRollLightFollowUp", commands[3]);
                Assert.AreEqual("CombatRollLightFollowUp", commands[4]);
            }
            finally
            {
                DriverScenarioField.SetValue(null, previousScenario);
            }
        }

        [Test]
        public void ResolveCaptureSteps_AirHeavyScenarioContainsRisingAndFallingCases()
        {
            object previousScenario = DriverScenarioField.GetValue(null);

            try
            {
                object airHeavyScenario = Enum.Parse(CaptureScenarioType, "SwordArtAirHeavy");
                DriverScenarioField.SetValue(null, airHeavyScenario);

                Array steps = ResolveCaptureStepsMethod.Invoke(null, null) as Array;

                Assert.IsNotNull(steps);
                Assert.AreEqual(5, steps.Length);

                string[] labels = steps.Cast<object>()
                    .Select(step => CaptureStepLabelProperty.GetValue(step) as string)
                    .ToArray();
                string[] commands = steps.Cast<object>()
                    .Select(step => CaptureStepCommandProperty.GetValue(step)?.ToString())
                    .ToArray();

                CollectionAssert.AreEqual(
                    new[]
                    {
                        "Rising Cleave hit / Airborne + Forward Heavy",
                        "Falling Star hit / Airborne + Neutral Heavy",
                        "Rising Cleave hit / AirDodge + Forward Heavy",
                        "Falling Star hit / AirDodge + Heavy",
                        "Falling Star whiff / AirDodge + Heavy"
                    },
                    labels);
                Assert.AreEqual("AirborneForwardHeavy", commands[0]);
                Assert.AreEqual("AirborneNeutralHeavy", commands[1]);
                Assert.AreEqual("AirDodgeForwardHeavyFollowUp", commands[2]);
                Assert.AreEqual("AirDodgeHeavyFollowUp", commands[3]);
                Assert.AreEqual("AirDodgeHeavyFollowUp", commands[4]);
            }
            finally
            {
                DriverScenarioField.SetValue(null, previousScenario);
            }
        }

        private static MethodInfo ParseRequestMethod =>
            typeof(CombatTestPlayerLungeCaptureDriverMenu).GetMethod(
                "ParseRequest",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo ResolveCaptureStepsMethod =>
            typeof(CombatTestPlayerLungeCaptureDriverMenu).GetMethod(
                "ResolveCaptureSteps",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static FieldInfo DriverScenarioField =>
            typeof(CombatTestPlayerLungeCaptureDriverMenu).GetField(
                "driverScenario",
                BindingFlags.Static | BindingFlags.NonPublic);

        private static Type CaptureScenarioType =>
            typeof(CombatTestPlayerLungeCaptureDriverMenu).GetNestedType(
                "CaptureScenario",
                BindingFlags.NonPublic);

        private static Type CaptureStepType =>
            typeof(CombatTestPlayerLungeCaptureDriverMenu).GetNestedType(
                "CaptureStep",
                BindingFlags.NonPublic);

        private static PropertyInfo CaptureStepLabelProperty =>
            CaptureStepType.GetProperty("Label", BindingFlags.Instance | BindingFlags.Public);

        private static PropertyInfo CaptureStepCommandProperty =>
            CaptureStepType.GetProperty("Command", BindingFlags.Instance | BindingFlags.Public);
    }
}
