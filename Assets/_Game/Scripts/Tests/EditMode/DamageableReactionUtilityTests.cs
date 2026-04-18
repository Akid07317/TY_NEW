using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class DamageableReactionUtilityTests
    {
        [Test]
        public void BuildPostDamageReaction_UsesPlayerHitStun_AndEnemyAggroTarget()
        {
            GameObject playerObject = new GameObject("Player");
            GameObject enemyObject = new GameObject("Enemy");
            GameObject sourceChild = new GameObject("SourceChild");
            EnemyArchetypeSO archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();

            try
            {
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                EnemyBrain enemyBrain = enemyObject.AddComponent<EnemyBrain>();
                sourceChild.transform.SetParent(playerObject.transform);
                SetPrivateField(archetype, "hitStunSeconds", 0.35f);
                SetPrivateField(enemyBrain, "archetype", archetype);

                DamageableReactionPlan plan = DamageableReactionUtility.BuildPostDamageReaction(
                    player,
                    enemyBrain,
                    sourceChild,
                    0.2f);

                Assert.AreEqual(0.2f, plan.PlayerHitStunSeconds, 0.0001f);
                Assert.AreEqual(0.35f, plan.EnemyHitStunSeconds, 0.0001f);
                Assert.AreSame(player.transform, plan.EnemyTarget);
                Assert.IsTrue(plan.SwitchEnemyToChase);
            }
            finally
            {
                Object.DestroyImmediate(archetype);
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void BuildPostDamageReaction_UsesDefaultEnemyHitStun_WithoutArchetype()
        {
            GameObject enemyObject = new GameObject("Enemy");

            try
            {
                EnemyBrain enemyBrain = enemyObject.AddComponent<EnemyBrain>();

                DamageableReactionPlan plan = DamageableReactionUtility.BuildPostDamageReaction(
                    null,
                    enemyBrain,
                    null,
                    0.2f);

                Assert.AreEqual(0f, plan.PlayerHitStunSeconds, 0.0001f);
                Assert.AreEqual(0.15f, plan.EnemyHitStunSeconds, 0.0001f);
                Assert.IsNull(plan.EnemyTarget);
                Assert.IsFalse(plan.SwitchEnemyToChase);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void TryResolveAggroTarget_RejectsNonPlayerSource()
        {
            GameObject source = new GameObject("Source");

            try
            {
                Assert.IsFalse(DamageableReactionUtility.TryResolveAggroTarget(source, out Transform target));
                Assert.IsNull(target);
            }
            finally
            {
                Object.DestroyImmediate(source);
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
