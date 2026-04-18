using CampusRPG.Interaction;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class EncounterControllerUtilityTests
    {
        [Test]
        public void BuildProgressPlan_UsesClearedBranchBeforeRuntimeFlags()
        {
            EncounterProgressPlan plan = EncounterControllerUtility.BuildProgressPlan(
                hasClearedProgress: true,
                isActive: true,
                startActive: true,
                resetUncleared: true);

            Assert.IsTrue(plan.ShouldApplyClearedState);
            Assert.IsFalse(plan.ShouldKeepActiveState);
            Assert.IsFalse(plan.ShouldResetUncleared);
            Assert.IsFalse(plan.ShouldActivateFromStart);
        }

        [Test]
        public void BuildProgressPlan_UsesActiveBranch_WhenEncounterIsRunning()
        {
            EncounterProgressPlan plan = EncounterControllerUtility.BuildProgressPlan(
                hasClearedProgress: false,
                isActive: true,
                startActive: true,
                resetUncleared: true);

            Assert.IsFalse(plan.ShouldApplyClearedState);
            Assert.IsTrue(plan.ShouldKeepActiveState);
            Assert.IsFalse(plan.ShouldResetUncleared);
            Assert.IsFalse(plan.ShouldActivateFromStart);
        }

        [Test]
        public void BuildProgressPlan_UsesIdleBranch_ForUnclearedManualEncounter()
        {
            EncounterProgressPlan plan = EncounterControllerUtility.BuildProgressPlan(
                hasClearedProgress: false,
                isActive: false,
                startActive: false,
                resetUncleared: true);

            Assert.IsFalse(plan.ShouldApplyClearedState);
            Assert.IsFalse(plan.ShouldKeepActiveState);
            Assert.IsTrue(plan.ShouldResetUncleared);
            Assert.IsFalse(plan.ShouldActivateFromStart);
        }

        [Test]
        public void AreAllMembersDefeated_ReturnsFalse_WhenAnyBoundMemberIsAlive()
        {
            GameObject defeatedObject = new GameObject("Defeated");
            GameObject aliveObject = new GameObject("Alive");

            try
            {
                EnemyEncounterMember defeatedMember = defeatedObject.AddComponent<EnemyEncounterMember>();
                EnemyEncounterMember aliveMember = aliveObject.AddComponent<EnemyEncounterMember>();
                SetPrivateField(defeatedMember, "isDefeated", true);
                SetPrivateField(aliveMember, "isDefeated", false);

                Assert.IsFalse(EncounterControllerUtility.AreAllMembersDefeated(new[] { defeatedMember, aliveMember }));
            }
            finally
            {
                Object.DestroyImmediate(defeatedObject);
                Object.DestroyImmediate(aliveObject);
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
