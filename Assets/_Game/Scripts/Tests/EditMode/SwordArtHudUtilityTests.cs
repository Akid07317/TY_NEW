using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class SwordArtHudUtilityTests
    {
        [Test]
        public void Build_PrioritizesCurrentSwordArtWithReadableRoleAndInput()
        {
            GameObject playerObject = new GameObject("Player");
            AttackDefinitionSO crossStepAttack = CreateAttack("Cross Step Strike", "SwordArt_CrossStep");
            AttackDefinitionSO fallingAttack = CreateAttack("Falling Star Slam", "SwordArt_FallingStar");
            SwordArtDefinitionSO crossStep = CreateSwordArt("Cross Step", crossStepAttack);
            SwordArtDefinitionSO fallingStar = CreateSwordArt("Falling Star", fallingAttack);

            try
            {
                PlayerCombatController combatController = CreateCombatController(playerObject);
                SetPrivateField(combatController, "previewSwordArt", fallingStar);
                SetPrivateField(combatController, "previewSwordArtAttack", fallingAttack);
                SetPrivateField(combatController, "previewSwordArtTimer", 0.6f);
                SetPrivateField(combatController, "currentSwordArt", crossStep);
                SetPrivateField(combatController, "currentSwordArtAttack", crossStepAttack);
                SetPrivateField(combatController, "currentAttackDefinition", crossStepAttack);
                combatController.NotifyAttackTiming(0.2f, 0.8f);

                SwordArtHudPlan plan = SwordArtHudUtility.Build(combatController);

                Assert.AreEqual(SwordArtHudMode.Current, plan.Mode);
                Assert.AreEqual("Cross Step", plan.Title);
                Assert.AreEqual("EXECUTING", plan.Status);
                Assert.AreEqual("Roll counter", plan.Detail);
                Assert.AreEqual("Roll + Light", plan.InputHint);
                Assert.AreEqual(0.25f, plan.Progress01, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(crossStep);
                Object.DestroyImmediate(fallingStar);
                Object.DestroyImmediate(crossStepAttack);
                Object.DestroyImmediate(fallingAttack);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Build_ReportsBufferedCancelWindowBeforeGenericPreview()
        {
            GameObject playerObject = new GameObject("Player");
            ManaComponent mana = playerObject.AddComponent<ManaComponent>();
            AttackDefinitionSO heavyAttack = CreateAttack("Heavy Slash", "Heavy_01");
            AttackDefinitionSO ironGateAttack = CreateAttack("Iron Gate Hit", "SwordArt_IronGateBreak");
            SwordArtDefinitionSO ironGateBreak = CreateSwordArt("Iron Gate Break", ironGateAttack);

            try
            {
                PlayerCombatController combatController = CreateCombatController(playerObject);
                SetPrivateField(ironGateBreak, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(ironGateBreak, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(ironGateBreak, "anyContextTags", SwordArtContextTags.AfterHeavy);
                SetPrivateField(ironGateBreak, "cancelWindowSeconds", 0.25f);
                SetPrivateField(ironGateBreak, "resourceCost", 15f);
                SetPrivateField(combatController, "swordArts", new[] { ironGateBreak });
                SetPrivateField(combatController, "currentAttackDefinition", heavyAttack);
                mana.SetMax(100f, refillCurrent: true);
                combatController.NotifyAttackTiming(0.6f, 0.8f);
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.AfterHeavy);

                SwordArtHudPlan plan = SwordArtHudUtility.Build(combatController);

                Assert.AreEqual(SwordArtHudMode.CancelWindow, plan.Mode);
                Assert.AreEqual("Iron Gate Break", plan.Title);
                Assert.AreEqual("CHAIN OPEN 15 MP", plan.Status);
                Assert.AreEqual("Guard pressure", plan.Detail);
                Assert.AreEqual("Guard/Heavy + Heavy", plan.InputHint);
            }
            finally
            {
                Object.DestroyImmediate(ironGateBreak);
                Object.DestroyImmediate(heavyAttack);
                Object.DestroyImmediate(ironGateAttack);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Build_ShowsPreviewAndRecentSwordArtStates()
        {
            GameObject playerObject = new GameObject("Player");
            ManaComponent mana = playerObject.AddComponent<ManaComponent>();
            AttackDefinitionSO moonSeverAttack = CreateAttack("Moon Sever Cut", "SwordArt_MoonSever");
            SwordArtDefinitionSO moonSever = CreateSwordArt("Moon Sever", moonSeverAttack);

            try
            {
                PlayerCombatController combatController = CreateCombatController(playerObject);
                SetPrivateField(moonSever, "resourceCost", 12f);
                SetPrivateField(combatController, "previewSwordArt", moonSever);
                SetPrivateField(combatController, "previewSwordArtAttack", moonSeverAttack);
                SetPrivateField(combatController, "previewSwordArtTimer", 0.6f);
                mana.SetMax(100f, refillCurrent: true);

                SwordArtHudPlan previewPlan = SwordArtHudUtility.Build(combatController);

                Assert.AreEqual(SwordArtHudMode.Preview, previewPlan.Mode);
                Assert.AreEqual("READY 12 MP", previewPlan.Status);
                Assert.AreEqual("Air dodge slash", previewPlan.Detail);
                Assert.AreEqual("Air Dodge + Light", previewPlan.InputHint);

                SetPrivateField(combatController, "previewSwordArtTimer", 0f);
                combatController.NotifySwordArtStarted(moonSever, moonSeverAttack);
                combatController.NotifyAttackStarted(moonSeverAttack);
                combatController.NotifyAttackFinished(PlayerAttackRequest.Light);

                SwordArtHudPlan recentPlan = SwordArtHudUtility.Build(combatController);

                Assert.AreEqual(SwordArtHudMode.Recent, recentPlan.Mode);
                Assert.AreEqual("Moon Sever", recentPlan.Title);
                Assert.AreEqual("RECENT 12 MP", recentPlan.Status);

                combatController.Tick(2f);

                Assert.IsFalse(SwordArtHudUtility.Build(combatController).IsVisible);
            }
            finally
            {
                Object.DestroyImmediate(moonSever);
                Object.DestroyImmediate(moonSeverAttack);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Build_PreviewShowsMissingManaStatus_ForBlockedSwordArt()
        {
            GameObject playerObject = new GameObject("Player");
            ManaComponent mana = playerObject.AddComponent<ManaComponent>();
            AttackDefinitionSO moonSeverAttack = CreateAttack("Moon Sever Cut", "SwordArt_MoonSever");
            SwordArtDefinitionSO moonSever = CreateSwordArt("Moon Sever", moonSeverAttack);

            try
            {
                PlayerCombatController combatController = CreateCombatController(playerObject);
                SetPrivateField(moonSever, "resourceCost", 12f);
                SetPrivateField(combatController, "previewSwordArt", moonSever);
                SetPrivateField(combatController, "previewSwordArtAttack", moonSeverAttack);
                SetPrivateField(combatController, "previewSwordArtTimer", 0.6f);
                mana.SetMax(100f, refillCurrent: true);
                mana.SetCurrent(5f);

                SwordArtHudPlan previewPlan = SwordArtHudUtility.Build(combatController);

                Assert.AreEqual(SwordArtHudMode.Preview, previewPlan.Mode);
                Assert.AreEqual("NEED 12 MP", previewPlan.Status);
                Assert.AreEqual("Moon Sever", previewPlan.Title);
            }
            finally
            {
                Object.DestroyImmediate(moonSever);
                Object.DestroyImmediate(moonSeverAttack);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Layout_BuildsProgressTrackWithinReadableBottomPanel()
        {
            SwordArtHudLayout layout = SwordArtHudLayoutUtility.Build(360f, 240f);
            SwordArtHudLayout narrowLayout = SwordArtHudLayoutUtility.Build(240f, 240f);
            BossAttackCueLayout bossLayout = BossAttackCueLayoutUtility.Build(360f, 240f);

            Assert.That(layout.PanelRect.width, Is.GreaterThanOrEqualTo(280f));
            Assert.That(layout.PanelRect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(layout.PanelRect.xMax, Is.LessThanOrEqualTo(360f));
            Assert.That(layout.PanelRect.yMin, Is.GreaterThanOrEqualTo(16f));
            Assert.That(layout.PanelRect.yMin, Is.GreaterThanOrEqualTo(bossLayout.PanelRect.yMax + 10f));
            Assert.That(layout.ProgressTrackRect.xMin, Is.GreaterThan(layout.PanelRect.xMin));
            Assert.That(layout.ProgressTrackRect.xMax, Is.LessThan(layout.PanelRect.xMax));
            Assert.That(layout.ProgressTrackRect.yMax, Is.LessThan(layout.PanelRect.yMax));
            Assert.That(layout.HintRect.yMax, Is.LessThanOrEqualTo(layout.ProgressTrackRect.yMin));
            Assert.That(layout.TitleRect.xMax, Is.LessThanOrEqualTo(layout.StatusRect.xMin));
            Assert.That(narrowLayout.PanelRect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(narrowLayout.PanelRect.xMax, Is.LessThanOrEqualTo(240f));
        }

        [Test]
        public void Layout_ProgressFillTracksAndClampsTimingProgress()
        {
            Rect trackRect = new Rect(12f, 34f, 200f, 5f);

            Rect quarterFill = SwordArtHudLayoutUtility.BuildProgressFill(trackRect, 0.25f);
            Rect overFill = SwordArtHudLayoutUtility.BuildProgressFill(trackRect, 3f);
            Rect underFill = SwordArtHudLayoutUtility.BuildProgressFill(trackRect, -2f);

            Assert.AreEqual(trackRect.x, quarterFill.x, 0.001f);
            Assert.AreEqual(trackRect.y, quarterFill.y, 0.001f);
            Assert.AreEqual(50f, quarterFill.width, 0.001f);
            Assert.AreEqual(trackRect.height, quarterFill.height, 0.001f);
            Assert.AreEqual(trackRect.width, overFill.width, 0.001f);
            Assert.AreEqual(0f, underFill.width, 0.001f);
        }

        private static PlayerCombatController CreateCombatController(GameObject owner)
        {
            AttackExecutor attackExecutor = owner.AddComponent<AttackExecutor>();
            HitboxController hitboxController = owner.AddComponent<HitboxController>();
            PlayerCombatController combatController = owner.AddComponent<PlayerCombatController>();
            SetPrivateField(hitboxController, "attackExecutor", attackExecutor);
            SetPrivateField(combatController, "attackExecutor", attackExecutor);
            SetPrivateField(combatController, "hitboxController", hitboxController);
            InvokeMethod(combatController, "Awake");
            return combatController;
        }

        private static AttackDefinitionSO CreateAttack(string displayName, string animationStateName)
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SetPrivateField(attack, "displayName", displayName);
            SetPrivateField(attack, "animationStateName", animationStateName);
            return attack;
        }

        private static SwordArtDefinitionSO CreateSwordArt(string displayName, AttackDefinitionSO attackDefinition)
        {
            SwordArtDefinitionSO swordArt = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();
            SetPrivateField(swordArt, "displayName", displayName);
            SetPrivateField(swordArt, "attackDefinition", attackDefinition);
            return swordArt;
        }

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, arguments);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
