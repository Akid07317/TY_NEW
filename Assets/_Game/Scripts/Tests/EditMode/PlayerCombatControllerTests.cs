using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class PlayerCombatControllerTests
    {
        [Test]
        public void LightCombo_StopsAtThirdHit_AndResetsToFirst()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO attack1 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO attack2 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO attack3 = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(controller, "lightAttackCombo", new[] { attack1, attack2, attack3 });

                Assert.AreSame(attack1, controller.ResolveAttack(PlayerAttackRequest.Light));
                Assert.IsTrue(controller.CanQueueNextLightAttack);

                controller.NotifyAttackFinished(PlayerAttackRequest.Light);
                Assert.AreSame(attack2, controller.ResolveAttack(PlayerAttackRequest.Light));
                Assert.IsTrue(controller.CanQueueNextLightAttack);

                controller.NotifyAttackFinished(PlayerAttackRequest.Light);
                Assert.AreSame(attack3, controller.ResolveAttack(PlayerAttackRequest.Light));
                Assert.IsFalse(controller.CanQueueNextLightAttack);

                controller.NotifyAttackFinished(PlayerAttackRequest.Light);
                Assert.AreSame(attack1, controller.ResolveAttack(PlayerAttackRequest.Light));
            }
            finally
            {
                Object.DestroyImmediate(attack1);
                Object.DestroyImmediate(attack2);
                Object.DestroyImmediate(attack3);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AnimationEventActivation_UsesPreparedHitboxAndTracksCurrentAttack()
        {
            GameObject attacker = new GameObject("PlayerCombat");
            AttackExecutor executor = attacker.AddComponent<AttackExecutor>();
            HitboxController hitboxController = attacker.AddComponent<HitboxController>();
            PlayerCombatController controller = attacker.AddComponent<PlayerCombatController>();
            GameObject target = CreateTarget("Target", new Vector3(0f, 0f, 1.1f));
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(executor, "attackOrigin", attacker.transform);
                SetPrivateField(hitboxController, "attackExecutor", executor);
                SetPrivateField(controller, "attackExecutor", executor);
                SetPrivateField(controller, "hitboxController", hitboxController);
                SetPrivateField(attack, "animationStateName", "Light_Anim");
                SetPrivateField(attack, "damageMultiplier", 1f);
                SetPrivateField(attack, "hitboxShape", AttackHitboxShape.Box);
                SetPrivateField(attack, "hitboxLocalCenter", new Vector3(0f, 0f, 1.1f));
                SetPrivateField(attack, "hitboxHalfExtents", new Vector3(0.45f, 0.45f, 0.45f));
                SetPrivateField(attack, "hitboxActivationMode", AttackHitboxActivationMode.AnimationEvent);

                hitboxController.Prepare(attack, 12f, attacker);
                controller.NotifyAttackStarted(attack);

                Assert.AreSame(attack, controller.CurrentAttackDefinition);
                Assert.AreEqual("Light_Anim", controller.CurrentAttackAnimationStateName);
                Assert.IsFalse(controller.ActivatePreparedHitboxFromAnimationEvent());

                hitboxController.OpenActivationWindow();

                Assert.IsTrue(controller.ActivatePreparedHitboxFromAnimationEvent());
                Assert.AreEqual(12f, target.GetComponent<TestDamageable>().TotalDamageReceived);

                controller.ClearPreparedHitboxFromAnimationEvent();
                controller.NotifyAttackFinished(PlayerAttackRequest.Heavy);

                Assert.IsNull(controller.CurrentAttackDefinition);
                Assert.AreEqual(string.Empty, controller.CurrentAttackAnimationStateName);
            }
            finally
            {
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(attacker);
            }
        }

        [Test]
        public void TimedWindowAttack_IgnoresHitboxAnimationEvents()
        {
            GameObject attacker = new GameObject("PlayerCombat");
            AttackExecutor executor = attacker.AddComponent<AttackExecutor>();
            HitboxController hitboxController = attacker.AddComponent<HitboxController>();
            PlayerCombatController controller = attacker.AddComponent<PlayerCombatController>();
            GameObject target = CreateTarget("Target", new Vector3(0f, 0f, 1.1f));
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(executor, "attackOrigin", attacker.transform);
                SetPrivateField(hitboxController, "attackExecutor", executor);
                SetPrivateField(controller, "attackExecutor", executor);
                SetPrivateField(controller, "hitboxController", hitboxController);
                SetPrivateField(attack, "damageMultiplier", 1f);
                SetPrivateField(attack, "hitboxShape", AttackHitboxShape.Box);
                SetPrivateField(attack, "hitboxLocalCenter", new Vector3(0f, 0f, 1.1f));
                SetPrivateField(attack, "hitboxHalfExtents", new Vector3(0.45f, 0.45f, 0.45f));
                SetPrivateField(attack, "hitboxActivationMode", AttackHitboxActivationMode.TimedWindow);

                hitboxController.Prepare(attack, 12f, attacker);
                controller.NotifyAttackStarted(attack);
                hitboxController.OpenActivationWindow();

                Assert.IsFalse(controller.ActivatePreparedHitboxFromAnimationEvent());
                Assert.AreEqual(0f, target.GetComponent<TestDamageable>().TotalDamageReceived);

                controller.ClearPreparedHitboxFromAnimationEvent();

                Assert.IsTrue(hitboxController.Activate());
                Assert.AreEqual(12f, target.GetComponent<TestDamageable>().TotalDamageReceived);
            }
            finally
            {
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(attacker);
            }
        }

        [Test]
        public void TryPreviewSwordArt_ResolvesCandidateAttack_WithoutChangingBaseAttackChain()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(controller, "lightAttackCombo", new[] { lightAttack });
                SetPrivateField(sidewindCut, "artId", "Sidewind_Cut");
                SetPrivateField(sidewindCut, "displayName", "Sidewind Cut");
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left | SwordArtDirectionMask.Right);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(controller, "swordArts", new[] { sidewindCut });

                SwordArtCommand command = new SwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Left,
                    SwordArtContextTags.AfterDodge,
                    ageSeconds: 0.1f);

                Assert.IsTrue(controller.TryPreviewSwordArt(command, out SwordArtDefinitionSO resolvedArt, out AttackDefinitionSO resolvedAttack));
                Assert.AreSame(sidewindCut, resolvedArt);
                Assert.AreSame(sidewindAttack, resolvedAttack);
                Assert.AreSame(lightAttack, controller.ResolveAttack(PlayerAttackRequest.Light));
            }
            finally
            {
                Object.DestroyImmediate(sidewindCut);
                Object.DestroyImmediate(sidewindAttack);
                Object.DestroyImmediate(lightAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TryPreviewSwordArt_ReturnsFalse_WhenCandidateHasNoAttackDefinition()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            SwordArtDefinitionSO risingCleave = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(risingCleave, "artId", "Rising_Cleave");
                SetPrivateField(risingCleave, "displayName", "Rising Cleave");
                SetPrivateField(risingCleave, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(risingCleave, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(risingCleave, "anyContextTags", SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne);
                SetPrivateField(risingCleave, "triggerWindowSeconds", 0.3f);
                SetPrivateField(controller, "swordArts", new[] { risingCleave });

                SwordArtCommand command = new SwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Forward,
                    ageSeconds: 0.1f);

                Assert.IsFalse(controller.TryPreviewSwordArt(command, out SwordArtDefinitionSO resolvedArt, out AttackDefinitionSO resolvedAttack));
                Assert.AreSame(risingCleave, resolvedArt);
                Assert.IsNull(resolvedAttack);
            }
            finally
            {
                Object.DestroyImmediate(risingCleave);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BufferedSwordArtCommand_TicksPreviewsAndConsumesWithoutExecutingAttackChain()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(controller, "lightAttackCombo", new[] { lightAttack });
                SetPrivateField(sidewindCut, "artId", "Sidewind_Cut");
                SetPrivateField(sidewindCut, "displayName", "Sidewind Cut");
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left | SwordArtDirectionMask.Right);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(controller, "swordArts", new[] { sidewindCut });

                controller.BufferSwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge);
                controller.Tick(0.12f);

                Assert.IsTrue(controller.HasBufferedSwordArtCommand);
                Assert.AreEqual(0.12f, controller.CurrentBufferedSwordArtCommand.AgeSeconds, 0.0001f);
                Assert.IsTrue(controller.TryPreviewBufferedSwordArt(out SwordArtDefinitionSO previewArt, out AttackDefinitionSO previewAttack));
                Assert.AreSame(sidewindCut, previewArt);
                Assert.AreSame(sidewindAttack, previewAttack);
                Assert.IsTrue(controller.HasBufferedSwordArtCommand);
                Assert.AreSame(lightAttack, controller.ResolveAttack(PlayerAttackRequest.Light));

                Assert.IsTrue(controller.TryConsumeBufferedSwordArt(out SwordArtDefinitionSO consumedArt, out AttackDefinitionSO consumedAttack));
                Assert.AreSame(sidewindCut, consumedArt);
                Assert.AreSame(sidewindAttack, consumedAttack);
                Assert.IsFalse(controller.HasBufferedSwordArtCommand);
            }
            finally
            {
                Object.DestroyImmediate(sidewindCut);
                Object.DestroyImmediate(sidewindAttack);
                Object.DestroyImmediate(lightAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TryConsumeBufferedSwordArt_SpendsMana_WhenSwordArtIsAffordable()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();
            ManaComponent mana = gameObject.AddComponent<ManaComponent>();

            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO gateBreakAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO gateBreak = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(controller, "mana", mana);
                SetPrivateField(controller, "lightAttackCombo", new[] { lightAttack });
                SetPrivateField(gateBreak, "artId", "Iron_Gate_Break");
                SetPrivateField(gateBreak, "displayName", "Iron Gate Break");
                SetPrivateField(gateBreak, "attackDefinition", gateBreakAttack);
                SetPrivateField(gateBreak, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(gateBreak, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(gateBreak, "anyContextTags", SwordArtContextTags.AfterHeavy);
                SetPrivateField(gateBreak, "triggerWindowSeconds", 0.35f);
                SetPrivateField(gateBreak, "resourceCost", 15f);
                SetPrivateField(controller, "swordArts", new[] { gateBreak });
                mana.SetMax(100f, refillCurrent: true);

                controller.BufferSwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.AfterHeavy);

                Assert.IsTrue(controller.CanAffordSwordArt(gateBreak));
                Assert.IsTrue(controller.TryConsumeBufferedSwordArt(out SwordArtDefinitionSO consumedArt, out AttackDefinitionSO consumedAttack));
                Assert.AreSame(gateBreak, consumedArt);
                Assert.AreSame(gateBreakAttack, consumedAttack);
                Assert.AreEqual(85f, mana.CurrentValue, 0.001f);
                Assert.IsFalse(controller.HasBufferedSwordArtCommand);
            }
            finally
            {
                Object.DestroyImmediate(gateBreak);
                Object.DestroyImmediate(gateBreakAttack);
                Object.DestroyImmediate(lightAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TryConsumeBufferedSwordArt_ReturnsFalseWithoutEnoughMana_AndKeepsPreviewReadable()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();
            ManaComponent mana = gameObject.AddComponent<ManaComponent>();

            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO moonSeverAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO moonSever = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(controller, "mana", mana);
                SetPrivateField(controller, "lightAttackCombo", new[] { lightAttack });
                SetPrivateField(moonSever, "artId", "Moon_Sever");
                SetPrivateField(moonSever, "displayName", "Moon Sever");
                SetPrivateField(moonSever, "attackDefinition", moonSeverAttack);
                SetPrivateField(moonSever, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(moonSever, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(
                    moonSever,
                    "requiredContextTags",
                    SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge);
                SetPrivateField(moonSever, "triggerWindowSeconds", 0.28f);
                SetPrivateField(moonSever, "resourceCost", 12f);
                SetPrivateField(controller, "swordArts", new[] { moonSever });
                mana.SetMax(100f, refillCurrent: true);
                mana.SetCurrent(5f);

                Assert.IsTrue(controller.TryRecordSwordArtPreviewCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge));
                Assert.IsFalse(controller.CanAffordSwordArt(moonSever));

                Assert.IsFalse(controller.TryConsumeBufferedSwordArt(out SwordArtDefinitionSO blockedArt, out AttackDefinitionSO blockedAttack));
                Assert.AreSame(moonSever, blockedArt);
                Assert.IsNull(blockedAttack);
                Assert.IsFalse(controller.HasBufferedSwordArtCommand);
                Assert.IsTrue(controller.HasSwordArtPreview);
                Assert.AreEqual(5f, mana.CurrentValue, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(moonSever);
                Object.DestroyImmediate(moonSeverAttack);
                Object.DestroyImmediate(lightAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TryRecordSwordArtPreviewCommand_StoresHudPreviewWithoutConsumingBufferedCommand()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(sidewindCut, "artId", "Sidewind_Cut");
                SetPrivateField(sidewindCut, "displayName", "Sidewind Cut");
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left | SwordArtDirectionMask.Right);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(controller, "swordArts", new[] { sidewindCut });

                Assert.IsTrue(controller.TryRecordSwordArtPreviewCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Left,
                    SwordArtContextTags.AfterDodge));

                Assert.IsTrue(controller.HasSwordArtPreview);
                Assert.AreSame(sidewindCut, controller.PreviewSwordArt);
                Assert.AreSame(sidewindAttack, controller.PreviewSwordArtAttack);
                Assert.IsTrue(controller.HasBufferedSwordArtCommand);

                controller.Tick(1f);

                Assert.IsFalse(controller.HasSwordArtPreview);
                Assert.IsTrue(controller.HasBufferedSwordArtCommand);
            }
            finally
            {
                Object.DestroyImmediate(sidewindCut);
                Object.DestroyImmediate(sidewindAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TryRecordSwordArtPreviewCommand_ClearsBufferedCommand_WhenInputDoesNotResolve()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(sidewindCut, "artId", "Sidewind_Cut");
                SetPrivateField(sidewindCut, "displayName", "Sidewind Cut");
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left | SwordArtDirectionMask.Right);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(controller, "swordArts", new[] { sidewindCut });

                Assert.IsFalse(controller.TryRecordSwordArtPreviewCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.None));

                Assert.IsFalse(controller.HasSwordArtPreview);
                Assert.IsFalse(controller.HasBufferedSwordArtCommand);
            }
            finally
            {
                Object.DestroyImmediate(sidewindCut);
                Object.DestroyImmediate(sidewindAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NotifySwordArtStarted_TracksActiveSwordArtUntilAttackEndsOrCancels()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);

                controller.NotifySwordArtStarted(sidewindCut, sidewindAttack);
                controller.NotifyAttackStarted(sidewindAttack);

                Assert.IsTrue(controller.HasCurrentSwordArt);
                Assert.AreSame(sidewindCut, controller.CurrentSwordArt);
                Assert.AreSame(sidewindAttack, controller.CurrentSwordArtAttack);

                controller.NotifyAttackFinished(PlayerAttackRequest.Light);

                Assert.IsFalse(controller.HasCurrentSwordArt);
                Assert.IsNull(controller.CurrentSwordArt);
                Assert.IsNull(controller.CurrentSwordArtAttack);

                controller.NotifySwordArtStarted(sidewindCut, sidewindAttack);
                controller.NotifyAttackStarted(lightAttack);

                Assert.IsFalse(controller.HasCurrentSwordArt);

                controller.NotifySwordArtStarted(sidewindCut, sidewindAttack);
                controller.NotifyAttackStarted(sidewindAttack);
                controller.NotifyAttackCanceled();

                Assert.IsFalse(controller.HasCurrentSwordArt);
            }
            finally
            {
                Object.DestroyImmediate(sidewindCut);
                Object.DestroyImmediate(lightAttack);
                Object.DestroyImmediate(sidewindAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AttackTiming_ReportsBufferedSwordArtCancelWindowStatus()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO gateBreakAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO gateBreak = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(gateBreak, "attackDefinition", gateBreakAttack);
                SetPrivateField(gateBreak, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(gateBreak, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(gateBreak, "anyContextTags", SwordArtContextTags.AfterHeavy);
                SetPrivateField(gateBreak, "triggerWindowSeconds", 0.35f);
                SetPrivateField(gateBreak, "cancelWindowSeconds", 0.25f);
                SetPrivateField(controller, "swordArts", new[] { gateBreak });

                controller.NotifyAttackStarted(heavyAttack);
                controller.NotifyAttackTiming(0.2f, 0.8f);
                controller.BufferSwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.AfterHeavy);

                Assert.IsTrue(controller.TryGetBufferedSwordArtCancelWindowStatus(
                    out SwordArtDefinitionSO pendingArt,
                    out AttackDefinitionSO pendingAttack,
                    out bool isOpen,
                    out float secondsUntilOpen));
                Assert.AreSame(gateBreak, pendingArt);
                Assert.AreSame(gateBreakAttack, pendingAttack);
                Assert.IsFalse(isOpen);
                Assert.AreEqual(0.35f, secondsUntilOpen, 0.001f);

                controller.NotifyAttackTiming(0.56f, 0.8f);

                Assert.IsTrue(controller.TryGetBufferedSwordArtCancelWindowStatus(
                    out _,
                    out _,
                    out isOpen,
                    out secondsUntilOpen));
                Assert.IsTrue(isOpen);
                Assert.AreEqual(0f, secondsUntilOpen, 0.001f);

                controller.NotifyAttackFinished(PlayerAttackRequest.Heavy);

                Assert.IsFalse(controller.TryGetBufferedSwordArtCancelWindowStatus(
                    out _,
                    out _,
                    out _,
                    out _));
            }
            finally
            {
                Object.DestroyImmediate(gateBreak);
                Object.DestroyImmediate(gateBreakAttack);
                Object.DestroyImmediate(heavyAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ResetRuntimeState_ClearsBufferedSwordArtCommand()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();
            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(controller, "swordArts", new[] { sidewindCut });

                controller.TryRecordSwordArtPreviewCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Left,
                    SwordArtContextTags.AfterDodge);

                Assert.IsTrue(controller.HasBufferedSwordArtCommand);
                Assert.IsTrue(controller.HasSwordArtPreview);
                controller.NotifySwordArtStarted(sidewindCut, sidewindAttack);
                controller.NotifyAttackStarted(sidewindAttack);
                Assert.IsTrue(controller.HasCurrentSwordArt);

                controller.ResetRuntimeState();

                Assert.IsFalse(controller.HasBufferedSwordArtCommand);
                Assert.IsFalse(controller.HasSwordArtPreview);
                Assert.IsFalse(controller.HasCurrentSwordArt);
            }
            finally
            {
                Object.DestroyImmediate(sidewindCut);
                Object.DestroyImmediate(sidewindAttack);
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static GameObject CreateTarget(string name, Vector3 position)
        {
            GameObject target = new GameObject(name);
            target.transform.position = position;
            target.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            target.AddComponent<TestDamageable>();
            return target;
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public float TotalDamageReceived { get; private set; }

            public void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source)
            {
                TotalDamageReceived += amount;
            }
        }
    }
}
