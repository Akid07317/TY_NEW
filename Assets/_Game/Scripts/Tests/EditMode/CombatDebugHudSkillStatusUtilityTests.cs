using System.Reflection;
using CampusRPG.Combat;
using CampusRPG.Skills;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatDebugHudSkillStatusUtilityTests
    {
        [Test]
        public void BuildSkillLine_ReportsReadyPendingCooldownAndBlockedStates()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f, displayName: "Spell Bolt");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    "Force Burst: Ready (20 MP, 0.2s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.AreEqual(
                    "Force Burst: Pending 0.2s (20 MP)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
                Assert.AreEqual(
                    "Spell Bolt: Blocked By Pending: Force Burst (0.2s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill2", skillController, 1));

                Assert.IsTrue(skillController.CancelPendingCast(0, firstSkill));
                Assert.IsTrue(skillController.TryBeginCast(1, out SkillDefinitionSO secondSkill));
                Assert.IsTrue(skillController.TryCommitCast(1, secondSkill));
                Assert.AreEqual(
                    "Spell Bolt: Cooldown 2.0s / 2.0s (0%)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill2", skillController, 1));

                mana.SetCurrent(10f);
                Assert.AreEqual(
                    "Force Burst: Need More Mana (10/20)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));

                Assert.AreEqual(
                    "Skill3: Empty Slot",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill3", skillController, 2));
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
        public void BuildSkillLine_FollowsSkillControllerRuntimeStatusPriority()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f, displayName: "Spell Bolt");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.AreEqual(
                    "Force Burst: Pending 0.2s (20 MP)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));

                Assert.IsTrue(skillController.CancelPendingCast(0, firstSkill));
                Assert.IsTrue(skillController.TryBeginCast(1, out SkillDefinitionSO secondSkill));
                Assert.IsTrue(skillController.TryCommitCast(1, secondSkill));
                mana.SetCurrent(0f);

                Assert.AreEqual(
                    "Spell Bolt: Cooldown 2.0s / 2.0s (0%)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill2", skillController, 1));
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
        public void BuildSkillLine_UsesSkillDisplayName_WhenAvailable()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    "Force Burst: Ready (20 MP, 0.2s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_PreservesShortHotkeyPrefix_WhenDisplayNameIsAvailable()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    "Q Force Burst: Ready (20 MP, 0.2s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Q", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_ShowsManaCost_WhenSkillIsReady()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 35f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    "Force Burst: Ready (35 MP, 0.2s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_ShowsManaCostAndCastTime_WhenSkillIsReady()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 35f, cooldownSeconds: 4f, castDurationSeconds: 0.45f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    "Force Burst: Ready (35 MP, 0.5s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_ShowsEmptySlot_WhenSkillSlotHasNoSkill()
        {
            GameObject playerObject = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.AreEqual(
                    "Skill3: Empty Slot",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill3", skillController, 2));
            }
            finally
            {
                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }
            }
        }

        [Test]
        public void BuildSkillLine_ShowsPendingCastDuration_WhenSkillIsPending()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.45f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out _));

                Assert.AreEqual(
                    "Force Burst: Pending 0.5s (20 MP)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_ShowsCooldownSecondsAndProgressPercent()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.IsTrue(skillController.TryCommitCast(0, firstSkill));
                skillController.Tick(1f);

                Assert.AreEqual(
                    "Force Burst: Cooldown 3.0s / 4.0s (25%)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_ShowsRemainingAndTotalCooldownDuration_WhenSkillIsCoolingDown()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 6f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));
                Assert.IsTrue(skillController.TryCommitCast(0, firstSkill));
                skillController.Tick(1.5f);

                Assert.AreEqual(
                    "Force Burst: Cooldown 4.5s / 6.0s (25%)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_ShowsCurrentManaAndRequiredMana_WhenBlockedByMana()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 35f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);
                mana.SetCurrent(12f);

                Assert.AreEqual(
                    "Force Burst: Need More Mana (12/35)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
        public void BuildSkillLine_ShowsPendingSkillName_WhenBlockedByOtherPendingCast()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.2f, displayName: "Force Burst");
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f, displayName: "Spell Bolt");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out SkillDefinitionSO firstSkill));

                Assert.AreEqual(
                    "Spell Bolt: Blocked By Pending: Force Burst (0.2s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill2", skillController, 1));
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
        public void BuildSkillLine_ShowsPendingSkillCastTime_WhenBlockedByOtherPendingCast()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;
            SkillDefinitionSO skillDefinition2 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 20f, cooldownSeconds: 4f, castDurationSeconds: 0.45f, displayName: "Force Burst");
                skillDefinition2 = BuildSkillDefinition(manaCost: 15f, cooldownSeconds: 2f, castDurationSeconds: 0.1f, displayName: "Spell Bolt");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                SetPrivateField(skillController, "skill2", skillDefinition2);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out _));

                Assert.AreEqual(
                    "Spell Bolt: Blocked By Pending: Force Burst (0.5s cast)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill2", skillController, 1));
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
        public void BuildSkillLine_ShowsManaCost_WhenSkillIsPending()
        {
            GameObject playerObject = null;
            SkillDefinitionSO skillDefinition1 = null;

            try
            {
                playerObject = new GameObject("Player");
                ManaComponent mana = playerObject.AddComponent<ManaComponent>();
                SkillController skillController = playerObject.AddComponent<SkillController>();
                skillDefinition1 = BuildSkillDefinition(manaCost: 35f, cooldownSeconds: 4f, castDurationSeconds: 0.45f, displayName: "Force Burst");
                SetPrivateField(skillController, "skill1", skillDefinition1);
                InvokeMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(skillController.TryBeginCast(0, out _));

                Assert.AreEqual(
                    "Force Burst: Pending 0.5s (35 MP)",
                    CombatDebugHudSkillStatusUtility.BuildSkillLine("Skill1", skillController, 0));
            }
            finally
            {
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
            string displayName = "New Skill")
        {
            SkillDefinitionSO skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            SetPrivateField(skillDefinition, "displayName", displayName);
            SetPrivateField(skillDefinition, "manaCost", manaCost);
            SetPrivateField(skillDefinition, "cooldownSeconds", cooldownSeconds);
            SetPrivateField(skillDefinition, "castDurationSeconds", castDurationSeconds);
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
