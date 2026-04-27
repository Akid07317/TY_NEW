using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class SkillLifecycleTests
    {
        [Test]
        public void TryBeginCast_OnlyValidates_AndCommitSpendsResources()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition = BuildSkillDefinition(manaCost: 25f, cooldownSeconds: 5f, castDurationSeconds: 0.2f);
                SetPrivateField(skillController, "skill1", skillDefinition);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO resolvedSkill));
                Assert.AreSame(skillDefinition, resolvedSkill);
                Assert.IsTrue(skillController.HasPendingCast);
                Assert.AreEqual(0, skillController.PendingSlotIndex);
                Assert.AreSame(skillDefinition, skillController.PendingSkill);
                Assert.AreEqual(100f, mana.CurrentValue, 0.001f);
                Assert.AreEqual(0f, skillController.GetRemainingCooldown(0), 0.001f);

                Assert.IsTrue(skillController.TryCommitCast(0, resolvedSkill));
                Assert.IsFalse(skillController.HasPendingCast);
                Assert.AreEqual(75f, mana.CurrentValue, 0.001f);
                Assert.AreEqual(5f, skillController.GetRemainingCooldown(0), 0.001f);
            }
            finally
            {
                if (skillDefinition != null)
                {
                    Object.DestroyImmediate(skillDefinition);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void PlayerSkillState_CancelBeforeCommit_DoesNotSpendResources()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = playerObject.AddComponent<PlayerStateMachine>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition = BuildSkillDefinition(
                    manaCost: 30f,
                    cooldownSeconds: 8f,
                    castDurationSeconds: 0.5f,
                    allowsMovementDuringCast: true,
                    movementSpeedScale: 0.45f);
                SetPrivateField(skillController, "skill1", skillDefinition);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "skillController", skillController);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                stateMachine.Initialize(player);
                stateMachine.SwitchToSkill(0);

                Assert.IsInstanceOf<PlayerSkillState>(stateMachine.CurrentState);
                Assert.IsTrue(skillController.HasPendingCast);
                Assert.AreEqual(0, skillController.PendingSlotIndex);
                Assert.AreSame(skillDefinition, skillController.PendingSkill);
                Assert.IsTrue(stateMachine.AllowsMovement);
                Assert.IsFalse(stateMachine.AllowsJump);
                Assert.AreEqual(0.45f, stateMachine.MovementSpeedScale, 0.001f);
                Assert.AreEqual(100f, mana.CurrentValue, 0.001f);
                Assert.AreEqual(0f, skillController.GetRemainingCooldown(0), 0.001f);

                stateMachine.SwitchToHit(0.08f);

                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.IsFalse(skillController.HasPendingCast);
                Assert.AreEqual(100f, mana.CurrentValue, 0.001f);
                Assert.AreEqual(0f, skillController.GetRemainingCooldown(0), 0.001f);
            }
            finally
            {
                if (skillDefinition != null)
                {
                    Object.DestroyImmediate(skillDefinition);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void TryBeginCast_BlocksOtherPendingSlot_UntilCurrentCastClears()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.3f);
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f);
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.AreSame(skillDefinition1, firstSkill);
                Assert.IsFalse(skillController.TryBeginCast(1, out SkillDefinitionSO blockedSkill));
                Assert.IsNull(blockedSkill);

                Assert.IsTrue(skillController.CancelPendingCast(0, firstSkill));
                Assert.IsFalse(skillController.HasPendingCast);
                Assert.IsTrue(skillController.TryBeginCast(1, out SkillDefinitionSO secondSkill));
                Assert.AreSame(skillDefinition2, secondSkill);
            }
            finally
            {
                if (skillDefinition2 != null)
                {
                    Object.DestroyImmediate(skillDefinition2);
                }

                if (skillDefinition1 != null)
                {
                    Object.DestroyImmediate(skillDefinition1);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void RestoreFromCheckpoint_ClearsPendingCast_AndSkillCooldowns()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                HealthComponent health = playerObject.AddComponent<HealthComponent>();
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                GaugeComponent gauges = playerObject.AddComponent<GaugeComponent>();
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.3f);
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f);
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                SetPrivateField(player, "skillController", skillController);
                SetPrivateField(player, "health", health);
                SetPrivateField(player, "mana", mana);
                SetPrivateField(player, "gauges", gauges);
                InvokeMethod(skillController, "Awake");
                health.SetMax(100f, refillCurrent: true);
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.IsTrue(skillController.CancelPendingCast(0, firstSkill));
                Assert.IsTrue(skillController.TryBeginCast(1, out SkillDefinitionSO secondSkill));
                Assert.IsTrue(skillController.TryCommitCast(1, secondSkill));
                Assert.AreEqual(2f, skillController.GetRemainingCooldown(1), 0.001f);
                Assert.IsTrue(skillController.TryBeginCast(0, out firstSkill));
                Assert.IsTrue(skillController.HasPendingCast);

                player.RestoreFromCheckpoint(Vector3.zero, Quaternion.identity, 90f, 55f);

                Assert.IsFalse(skillController.HasPendingCast);
                Assert.AreEqual(0f, skillController.GetRemainingCooldown(0), 0.001f);
                Assert.AreEqual(0f, skillController.GetRemainingCooldown(1), 0.001f);
                Assert.AreEqual(90f, health.CurrentValue, 0.001f);
                Assert.AreEqual(55f, mana.CurrentValue, 0.001f);
            }
            finally
            {
                if (skillDefinition2 != null)
                {
                    Object.DestroyImmediate(skillDefinition2);
                }

                if (skillDefinition1 != null)
                {
                    Object.DestroyImmediate(skillDefinition1);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void CooldownQuery_ReportsRunningState_AndNormalizedProgress()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f);
                SetPrivateField(skillController, "skill1", skillDefinition);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsFalse(skillController.IsOnCooldown(0));
                Assert.AreEqual(1f, skillController.GetCooldownProgressNormalized(0), 0.001f);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO resolvedSkill));
                Assert.IsTrue(skillController.TryCommitCast(0, resolvedSkill));

                Assert.IsTrue(skillController.IsOnCooldown(0));
                Assert.AreEqual(0f, skillController.GetCooldownProgressNormalized(0), 0.001f);

                skillController.Tick(1f);

                Assert.IsTrue(skillController.IsOnCooldown(0));
                Assert.AreEqual(0.25f, skillController.GetCooldownProgressNormalized(0), 0.001f);

                skillController.Tick(3f);

                Assert.IsFalse(skillController.IsOnCooldown(0));
                Assert.AreEqual(1f, skillController.GetCooldownProgressNormalized(0), 0.001f);
            }
            finally
            {
                if (skillDefinition != null)
                {
                    Object.DestroyImmediate(skillDefinition);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void CanBeginCast_PureQuery_DoesNotRegisterPendingCast()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f);
                SetPrivateField(skillController, "skill1", skillDefinition);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.CanBeginCast(0, out SkillDefinitionSO resolvedSkill));
                Assert.AreSame(skillDefinition, resolvedSkill);
                Assert.IsFalse(skillController.HasPendingCast);
                Assert.AreEqual(-1, skillController.PendingSlotIndex);
                Assert.IsNull(skillController.PendingSkill);
            }
            finally
            {
                if (skillDefinition != null)
                {
                    Object.DestroyImmediate(skillDefinition);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void CanBeginCast_RespectsCooldownManaAndOtherPendingSlot()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f);
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f);
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.IsTrue(skillController.CanBeginCast(0, out SkillDefinitionSO sameSlotSkill));
                Assert.AreSame(firstSkill, sameSlotSkill);
                Assert.IsFalse(skillController.CanBeginCast(1, out SkillDefinitionSO blockedByPendingSkill));
                Assert.AreSame(skillDefinition2, blockedByPendingSkill);

                Assert.IsTrue(skillController.CancelPendingCast(0, firstSkill));
                Assert.IsTrue(skillController.TryBeginCast(1, out SkillDefinitionSO secondSkill));
                Assert.IsTrue(skillController.TryCommitCast(1, secondSkill));
                Assert.IsFalse(skillController.CanBeginCast(1, out SkillDefinitionSO blockedByCooldownSkill));
                Assert.AreSame(skillDefinition2, blockedByCooldownSkill);

                mana.SetCurrent(10f);
                Assert.IsFalse(skillController.CanBeginCast(0, out SkillDefinitionSO blockedByManaSkill));
                Assert.AreSame(skillDefinition1, blockedByManaSkill);
            }
            finally
            {
                if (skillDefinition2 != null)
                {
                    Object.DestroyImmediate(skillDefinition2);
                }

                if (skillDefinition1 != null)
                {
                    Object.DestroyImmediate(skillDefinition1);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void GetBeginCastBlockReason_ReportsCommonFailureReasons()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f);
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f);
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    SkillBeginCastBlockReason.None,
                    skillController.GetBeginCastBlockReason(0, out SkillDefinitionSO readySkill));
                Assert.AreSame(skillDefinition1, readySkill);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.AreEqual(
                    SkillBeginCastBlockReason.OtherPendingCast,
                    skillController.GetBeginCastBlockReason(1, out SkillDefinitionSO blockedByPendingSkill));
                Assert.AreSame(skillDefinition2, blockedByPendingSkill);

                Assert.IsTrue(skillController.CancelPendingCast(0, firstSkill));
                Assert.IsTrue(skillController.TryBeginCast(1, out SkillDefinitionSO secondSkill));
                Assert.IsTrue(skillController.TryCommitCast(1, secondSkill));
                Assert.AreEqual(
                    SkillBeginCastBlockReason.Cooldown,
                    skillController.GetBeginCastBlockReason(1, out SkillDefinitionSO blockedByCooldownSkill));
                Assert.AreSame(skillDefinition2, blockedByCooldownSkill);

                mana.SetCurrent(10f);
                Assert.AreEqual(
                    SkillBeginCastBlockReason.NotEnoughMana,
                    skillController.GetBeginCastBlockReason(0, out SkillDefinitionSO blockedByManaSkill));
                Assert.AreSame(skillDefinition1, blockedByManaSkill);

                Assert.AreEqual(
                    SkillBeginCastBlockReason.MissingSkill,
                    skillController.GetBeginCastBlockReason(99, out SkillDefinitionSO missingSkill));
                Assert.IsNull(missingSkill);
            }
            finally
            {
                if (skillDefinition2 != null)
                {
                    Object.DestroyImmediate(skillDefinition2);
                }

                if (skillDefinition1 != null)
                {
                    Object.DestroyImmediate(skillDefinition1);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void GetSlotRuntimeStatus_ReportsReadyPendingCooldownBlockedAndMissingSkill()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f);
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f);
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    SkillSlotRuntimeStatus.Ready,
                    skillController.GetSlotRuntimeStatus(0, out SkillDefinitionSO readySkill, out SkillBeginCastBlockReason readyBlockReason));
                Assert.AreSame(skillDefinition1, readySkill);
                Assert.AreEqual(SkillBeginCastBlockReason.None, readyBlockReason);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.AreEqual(
                    SkillSlotRuntimeStatus.Pending,
                    skillController.GetSlotRuntimeStatus(0, out SkillDefinitionSO pendingSkill, out SkillBeginCastBlockReason pendingBlockReason));
                Assert.AreSame(skillDefinition1, pendingSkill);
                Assert.AreEqual(SkillBeginCastBlockReason.None, pendingBlockReason);

                Assert.AreEqual(
                    SkillSlotRuntimeStatus.Blocked,
                    skillController.GetSlotRuntimeStatus(1, out SkillDefinitionSO blockedSkill, out SkillBeginCastBlockReason blockedReason));
                Assert.AreSame(skillDefinition2, blockedSkill);
                Assert.AreEqual(SkillBeginCastBlockReason.OtherPendingCast, blockedReason);

                Assert.IsTrue(skillController.CancelPendingCast(0, firstSkill));
                Assert.IsTrue(skillController.TryBeginCast(1, out SkillDefinitionSO secondSkill));
                Assert.IsTrue(skillController.TryCommitCast(1, secondSkill));
                Assert.AreEqual(
                    SkillSlotRuntimeStatus.Cooldown,
                    skillController.GetSlotRuntimeStatus(1, out SkillDefinitionSO cooldownSkill, out SkillBeginCastBlockReason cooldownBlockReason));
                Assert.AreSame(skillDefinition2, cooldownSkill);
                Assert.AreEqual(SkillBeginCastBlockReason.Cooldown, cooldownBlockReason);

                mana.SetCurrent(10f);
                Assert.AreEqual(
                    SkillSlotRuntimeStatus.Blocked,
                    skillController.GetSlotRuntimeStatus(0, out SkillDefinitionSO manaBlockedSkill, out SkillBeginCastBlockReason manaBlockedReason));
                Assert.AreSame(skillDefinition1, manaBlockedSkill);
                Assert.AreEqual(SkillBeginCastBlockReason.NotEnoughMana, manaBlockedReason);

                Assert.AreEqual(
                    SkillSlotRuntimeStatus.MissingSkill,
                    skillController.GetSlotRuntimeStatus(99, out SkillDefinitionSO missingSkill, out SkillBeginCastBlockReason missingBlockReason));
                Assert.IsNull(missingSkill);
                Assert.AreEqual(SkillBeginCastBlockReason.MissingSkill, missingBlockReason);
            }
            finally
            {
                if (skillDefinition2 != null)
                {
                    Object.DestroyImmediate(skillDefinition2);
                }

                if (skillDefinition1 != null)
                {
                    Object.DestroyImmediate(skillDefinition1);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        private static SkillDefinitionSO BuildSkillDefinition(
            float manaCost,
            float cooldownSeconds,
            float castDurationSeconds,
            bool allowsMovementDuringCast = false,
            float movementSpeedScale = 1f)
        {
            SkillDefinitionSO skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            SetPrivateField(skillDefinition, "manaCost", manaCost);
            SetPrivateField(skillDefinition, "cooldownSeconds", cooldownSeconds);
            SetPrivateField(skillDefinition, "castDurationSeconds", castDurationSeconds);
            SetPrivateField(skillDefinition, "allowsMovementDuringCast", allowsMovementDuringCast);
            SetPrivateField(skillDefinition, "movementSpeedScale", movementSpeedScale);
            SetPrivateField(skillDefinition, "targetMode", SkillTargetMode.Self);
            return skillDefinition;
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void InvokeMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
        }
    }
}
