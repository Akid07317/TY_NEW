using System.Reflection;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class SwordArtResolverTests
    {
        [Test]
        public void TryResolve_MatchesSidewindCut_AfterDodgeSideInput()
        {
            SwordArtDefinitionSO sidewindCut = null;
            SwordArtDefinitionSO risingCleave = null;
            SwordArtDefinitionSO ironGateBreak = null;

            try
            {
                sidewindCut = BuildSwordArt(
                    "Sidewind_Cut",
                    "Sidewind Cut",
                    SwordArtTriggerAction.LightAttack,
                    SwordArtDirectionMask.Left | SwordArtDirectionMask.Right,
                    SwordArtContextTags.AfterDodge,
                    SwordArtContextTags.None,
                    0.25f);
                risingCleave = BuildRisingCleave();
                ironGateBreak = BuildIronGateBreak();

                SwordArtCommand command = new SwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Left,
                    SwordArtContextTags.AfterDodge,
                    ageSeconds: 0.12f);

                Assert.IsTrue(SwordArtResolver.TryResolve(
                    new[] { risingCleave, ironGateBreak, sidewindCut },
                    command,
                    out SwordArtDefinitionSO resolved));
                Assert.AreSame(sidewindCut, resolved);

                Assert.IsFalse(SwordArtResolver.TryResolve(
                    new[] { sidewindCut },
                    command.WithAge(0.35f),
                    out _));
            }
            finally
            {
                DestroySwordArt(sidewindCut);
                DestroySwordArt(risingCleave);
                DestroySwordArt(ironGateBreak);
            }
        }

        [Test]
        public void TryResolve_PrefersCrossStep_ForCombatRollLight()
        {
            SwordArtDefinitionSO sidewindCut = null;
            SwordArtDefinitionSO crossStep = null;

            try
            {
                sidewindCut = BuildSwordArt(
                    "Sidewind_Cut",
                    "Sidewind Cut",
                    SwordArtTriggerAction.LightAttack,
                    SwordArtDirectionMask.Left | SwordArtDirectionMask.Right,
                    SwordArtContextTags.AfterDodge,
                    SwordArtContextTags.None,
                    0.25f);
                crossStep = BuildCrossStep();

                SwordArtCommand combatRollCommand = new SwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterCombatRoll,
                    ageSeconds: 0.1f);

                Assert.IsTrue(SwordArtResolver.TryResolve(
                    new[] { sidewindCut, crossStep },
                    combatRollCommand,
                    out SwordArtDefinitionSO rollResolved));
                Assert.AreSame(crossStep, rollResolved);

                SwordArtCommand shortDodgeCommand = new SwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge,
                    ageSeconds: 0.1f);

                Assert.IsTrue(SwordArtResolver.TryResolve(
                    new[] { crossStep, sidewindCut },
                    shortDodgeCommand,
                    out SwordArtDefinitionSO shortDodgeResolved));
                Assert.AreSame(sidewindCut, shortDodgeResolved);
            }
            finally
            {
                DestroySwordArt(crossStep);
                DestroySwordArt(sidewindCut);
            }
        }

        [Test]
        public void TryResolve_MatchesMoonSever_AfterAirDodgeLightOnly()
        {
            SwordArtDefinitionSO moonSever = null;
            SwordArtDefinitionSO sidewindCut = null;

            try
            {
                moonSever = BuildMoonSever();
                sidewindCut = BuildSwordArt(
                    "Sidewind_Cut",
                    "Sidewind Cut",
                    SwordArtTriggerAction.LightAttack,
                    SwordArtDirectionMask.Left | SwordArtDirectionMask.Right,
                    SwordArtContextTags.AfterDodge,
                    SwordArtContextTags.None,
                    0.25f);

                SwordArtCommand airDodgeCommand = new SwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge,
                    ageSeconds: 0.1f);

                Assert.IsTrue(SwordArtResolver.TryResolve(
                    new[] { sidewindCut, moonSever },
                    airDodgeCommand,
                    out SwordArtDefinitionSO resolved));
                Assert.AreSame(moonSever, resolved);

                SwordArtCommand genericAirborneCommand = new SwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.Airborne,
                    ageSeconds: 0.1f);
                Assert.IsFalse(SwordArtResolver.TryResolve(new[] { moonSever }, genericAirborneCommand, out _));

                SwordArtCommand airDodgeHeavyCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge,
                    ageSeconds: 0.1f);
                Assert.IsFalse(SwordArtResolver.TryResolve(new[] { moonSever }, airDodgeHeavyCommand, out _));
            }
            finally
            {
                DestroySwordArt(sidewindCut);
                DestroySwordArt(moonSever);
            }
        }

        [Test]
        public void TryResolve_MatchesRisingCleave_FromForwardInputOrAirborneState()
        {
            SwordArtDefinitionSO risingCleave = null;

            try
            {
                risingCleave = BuildRisingCleave();

                SwordArtCommand forwardCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Forward,
                    SwordArtContextTags.None,
                    ageSeconds: 0.1f);
                Assert.IsTrue(SwordArtResolver.TryResolve(new[] { risingCleave }, forwardCommand, out SwordArtDefinitionSO forwardResolved));
                Assert.AreSame(risingCleave, forwardResolved);

                SwordArtCommand airborneCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.Airborne,
                    ageSeconds: 0.1f);
                Assert.IsTrue(SwordArtResolver.TryResolve(new[] { risingCleave }, airborneCommand, out SwordArtDefinitionSO airborneResolved));
                Assert.AreSame(risingCleave, airborneResolved);
            }
            finally
            {
                DestroySwordArt(risingCleave);
            }
        }

        [Test]
        public void TryResolve_PrefersFallingStar_ForAirborneNeutralOrBackwardHeavy()
        {
            SwordArtDefinitionSO risingCleave = null;
            SwordArtDefinitionSO fallingStar = null;

            try
            {
                risingCleave = BuildRisingCleave();
                fallingStar = BuildFallingStar();

                SwordArtCommand neutralAirborneCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.Airborne,
                    ageSeconds: 0.1f);
                Assert.IsTrue(SwordArtResolver.TryResolve(
                    new[] { risingCleave, fallingStar },
                    neutralAirborneCommand,
                    out SwordArtDefinitionSO neutralResolved));
                Assert.AreSame(fallingStar, neutralResolved);

                SwordArtCommand backwardAirborneCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Backward,
                    SwordArtContextTags.Airborne,
                    ageSeconds: 0.1f);
                Assert.IsTrue(SwordArtResolver.TryResolve(
                    new[] { risingCleave, fallingStar },
                    backwardAirborneCommand,
                    out SwordArtDefinitionSO backwardResolved));
                Assert.AreSame(fallingStar, backwardResolved);

                SwordArtCommand forwardAirborneCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Forward,
                    SwordArtContextTags.Airborne,
                    ageSeconds: 0.1f);
                Assert.IsTrue(SwordArtResolver.TryResolve(
                    new[] { fallingStar, risingCleave },
                    forwardAirborneCommand,
                    out SwordArtDefinitionSO forwardResolved));
                Assert.AreSame(risingCleave, forwardResolved);
            }
            finally
            {
                DestroySwordArt(fallingStar);
                DestroySwordArt(risingCleave);
            }
        }

        [Test]
        public void TryResolve_MatchesIronGateBreak_AfterBlockOrHeavy()
        {
            SwordArtDefinitionSO ironGateBreak = null;

            try
            {
                ironGateBreak = BuildIronGateBreak();

                SwordArtCommand blockCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.AfterBlock,
                    ageSeconds: 0.15f);
                Assert.IsTrue(SwordArtResolver.TryResolve(new[] { ironGateBreak }, blockCommand, out SwordArtDefinitionSO blockResolved));
                Assert.AreSame(ironGateBreak, blockResolved);

                SwordArtCommand heavyCommand = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.AfterHeavy,
                    ageSeconds: 0.15f);
                Assert.IsTrue(SwordArtResolver.TryResolve(new[] { ironGateBreak }, heavyCommand, out SwordArtDefinitionSO heavyResolved));
                Assert.AreSame(ironGateBreak, heavyResolved);
            }
            finally
            {
                DestroySwordArt(ironGateBreak);
            }
        }

        [Test]
        public void CommandBuffer_TracksShortWindowAndConsumesMatchedCommand()
        {
            SwordArtDefinitionSO sidewindCut = null;

            try
            {
                sidewindCut = BuildSwordArt(
                    "Sidewind_Cut",
                    "Sidewind Cut",
                    SwordArtTriggerAction.LightAttack,
                    SwordArtDirectionMask.Left | SwordArtDirectionMask.Right,
                    SwordArtContextTags.AfterDodge,
                    SwordArtContextTags.None,
                    0.25f);

                SwordArtCommandBuffer buffer = new SwordArtCommandBuffer();
                buffer.Buffer(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge);
                buffer.Tick(0.12f);

                Assert.IsTrue(buffer.TryResolve(new[] { sidewindCut }, out SwordArtDefinitionSO resolved));
                Assert.AreSame(sidewindCut, resolved);
                Assert.IsFalse(buffer.HasCommand);

                buffer.Buffer(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge);
                buffer.Tick(0.3f);

                Assert.IsFalse(buffer.TryResolve(new[] { sidewindCut }, out _));
                Assert.IsTrue(buffer.HasCommand);
            }
            finally
            {
                DestroySwordArt(sidewindCut);
            }
        }

        private static SwordArtDefinitionSO BuildRisingCleave()
        {
            return BuildSwordArt(
                "Rising_Cleave",
                "Rising Cleave",
                SwordArtTriggerAction.HeavyAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.None,
                SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne,
                0.3f);
        }

        private static SwordArtDefinitionSO BuildCrossStep()
        {
            return BuildSwordArt(
                "Cross_Step",
                "Cross Step",
                SwordArtTriggerAction.LightAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterCombatRoll,
                SwordArtContextTags.None,
                0.3f);
        }

        private static SwordArtDefinitionSO BuildMoonSever()
        {
            return BuildSwordArt(
                "Moon_Sever",
                "Moon Sever",
                SwordArtTriggerAction.LightAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge,
                SwordArtContextTags.None,
                0.28f);
        }

        private static SwordArtDefinitionSO BuildIronGateBreak()
        {
            return BuildSwordArt(
                "Iron_Gate_Break",
                "Iron Gate Break",
                SwordArtTriggerAction.HeavyAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.None,
                SwordArtContextTags.AfterBlock | SwordArtContextTags.AfterHeavy,
                0.35f);
        }

        private static SwordArtDefinitionSO BuildFallingStar()
        {
            return BuildSwordArt(
                "Falling_Star",
                "Falling Star",
                SwordArtTriggerAction.HeavyAttack,
                SwordArtDirectionMask.Neutral | SwordArtDirectionMask.Backward,
                SwordArtContextTags.Airborne,
                SwordArtContextTags.None,
                0.32f);
        }

        private static SwordArtDefinitionSO BuildSwordArt(
            string artId,
            string displayName,
            SwordArtTriggerAction triggerAction,
            SwordArtDirectionMask acceptedDirections,
            SwordArtContextTags requiredContextTags,
            SwordArtContextTags anyContextTags,
            float triggerWindowSeconds)
        {
            SwordArtDefinitionSO definition = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();
            SetPrivateField(definition, "artId", artId);
            SetPrivateField(definition, "displayName", displayName);
            SetPrivateField(definition, "triggerAction", triggerAction);
            SetPrivateField(definition, "acceptedDirections", acceptedDirections);
            SetPrivateField(definition, "requiredContextTags", requiredContextTags);
            SetPrivateField(definition, "anyContextTags", anyContextTags);
            SetPrivateField(definition, "triggerWindowSeconds", triggerWindowSeconds);
            return definition;
        }

        private static void DestroySwordArt(SwordArtDefinitionSO definition)
        {
            if (definition != null)
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
