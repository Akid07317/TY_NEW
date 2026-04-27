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

        [Test]
        public void ResolveDefenseOutcome_RequiresGuardStartupToFinishBeforeBlockingDamage()
        {
            GameObject playerObject = new GameObject("Player");
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();

            try
            {
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = playerObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = playerObject.AddComponent<PlayerCombatController>();
                SetPrivateField(balance, "guardStartupSeconds", 0.08f);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(combatController, "balance", balance);

                stateMachine.Initialize(player);
                stateMachine.SwitchToBlock();

                Assert.IsTrue(stateMachine.IsBlocking);
                Assert.IsFalse(stateMachine.HasActiveGuard);
                Assert.AreEqual(DamageDefenseOutcome.None, DamageableReactionUtility.ResolveDefenseOutcome(player));

                SetPrivateField(stateMachine.CurrentState, "guardStartupRemaining", 0f);

                Assert.IsTrue(stateMachine.HasActiveGuard);
                Assert.AreEqual(DamageDefenseOutcome.SuccessfulBlock, DamageableReactionUtility.ResolveDefenseOutcome(player));
            }
            finally
            {
                Object.DestroyImmediate(balance);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ResolveDefenseOutcome_RequiresDodgeStartupToFinishBeforeSuccessfulDodge()
        {
            GameObject playerObject = new GameObject("Player");
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();

            try
            {
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = playerObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = playerObject.AddComponent<PlayerCombatController>();
                SetPrivateField(balance, "dodgeDurationSeconds", 0.25f);
                SetPrivateField(balance, "dodgeInvulnerableStartupSeconds", 0.04f);
                SetPrivateField(balance, "dodgeInvulnerableSeconds", 0.2f);
                SetPrivateField(balance, "dodgeFollowUpWindowSeconds", 0.8f);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(combatController, "balance", balance);

                stateMachine.Initialize(player);
                stateMachine.SwitchToDodge();

                Assert.IsFalse(stateMachine.IsInvulnerable);
                Assert.IsFalse(combatController.HasDodgeFollowUpWindow);
                Assert.AreEqual(DamageDefenseOutcome.None, DamageableReactionUtility.ResolveDefenseOutcome(player));

                stateMachine.Tick(0.04f);

                Assert.IsTrue(stateMachine.IsInvulnerable);
                Assert.AreEqual(DamageDefenseOutcome.SuccessfulDodge, DamageableReactionUtility.ResolveDefenseOutcome(player));
                Assert.IsTrue(combatController.HasDodgeFollowUpWindow);

                Assert.AreEqual(DamageDefenseOutcome.None, DamageableReactionUtility.ResolveDefenseOutcome(player));
            }
            finally
            {
                Object.DestroyImmediate(balance);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ReceiveDamage_ActiveGuardBlocksDamageAndOpensCounterFeedback()
        {
            GameObject playerObject = new GameObject("Player");
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();

            try
            {
                BuildPlayerDamageReceiver(
                    playerObject,
                    balance,
                    out HealthComponent health,
                    out GaugeComponent gauges,
                    out PlayerStateMachine stateMachine,
                    out PlayerCombatController combatController,
                    out DamageableReceiver receiver);
                SetPrivateField(balance, "guardStartupSeconds", 0.08f);
                SetPrivateField(balance, "guardCounterGaugeGain", 20f);
                SetPrivateField(balance, "counterWindowSeconds", 0.8f);

                stateMachine.SwitchToBlock();
                SetPrivateField(stateMachine.CurrentState, "guardStartupRemaining", 0f);

                receiver.ReceiveDamage(30f, Vector3.zero, null);

                Assert.AreEqual(100f, health.CurrentValue, 0.001f);
                Assert.AreEqual(20f, gauges.CounterGauge, 0.001f);
                Assert.IsTrue(combatController.HasCounterWindow);
            }
            finally
            {
                Object.DestroyImmediate(balance);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ReceiveDamage_GuardStartupFailureTakesDamageAndDoesNotOpenCounterFeedback()
        {
            GameObject playerObject = new GameObject("Player");
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();

            try
            {
                BuildPlayerDamageReceiver(
                    playerObject,
                    balance,
                    out HealthComponent health,
                    out GaugeComponent gauges,
                    out PlayerStateMachine stateMachine,
                    out PlayerCombatController combatController,
                    out DamageableReceiver receiver);
                SetPrivateField(balance, "guardStartupSeconds", 0.08f);
                SetPrivateField(balance, "guardCounterGaugeGain", 20f);
                SetPrivateField(balance, "counterWindowSeconds", 0.8f);

                stateMachine.SwitchToBlock();

                receiver.ReceiveDamage(30f, Vector3.zero, null);

                Assert.AreEqual(70f, health.CurrentValue, 0.001f);
                Assert.AreEqual(0f, gauges.CounterGauge, 0.001f);
                Assert.IsFalse(combatController.HasCounterWindow);
            }
            finally
            {
                Object.DestroyImmediate(balance);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ResolveDefenseOutcome_ActiveGuardCanBeBrokenByGuardBreakAttack()
        {
            GameObject playerObject = new GameObject("Player");
            AttackDefinitionSO guardBreakAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = playerObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(guardBreakAttack, "breaksGuard", true);

                stateMachine.Initialize(player);
                stateMachine.SwitchToBlock();
                SetPrivateField(stateMachine.CurrentState, "guardStartupRemaining", 0f);

                Assert.AreEqual(
                    DamageDefenseOutcome.GuardBroken,
                    DamageableReactionUtility.ResolveDefenseOutcome(player, guardBreakAttack));
            }
            finally
            {
                Object.DestroyImmediate(guardBreakAttack);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ReceiveDamage_ActiveGuardAppliesBlockStunWithoutTakingDamage()
        {
            GameObject playerObject = new GameObject("Player");
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();
            AttackDefinitionSO blockStunAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                BuildPlayerDamageReceiver(
                    playerObject,
                    balance,
                    out HealthComponent health,
                    out GaugeComponent gauges,
                    out PlayerStateMachine stateMachine,
                    out PlayerCombatController combatController,
                    out DamageableReceiver receiver);
                SetPrivateField(balance, "guardStartupSeconds", 0.08f);
                SetPrivateField(balance, "guardCounterGaugeGain", 20f);
                SetPrivateField(balance, "counterWindowSeconds", 0.8f);
                SetPrivateField(blockStunAttack, "blockStunSeconds", 0.18f);

                stateMachine.SwitchToBlock();
                SetPrivateField(stateMachine.CurrentState, "guardStartupRemaining", 0f);

                receiver.ReceiveDamage(30f, Vector3.zero, null, blockStunAttack);

                Assert.AreEqual(100f, health.CurrentValue, 0.001f);
                Assert.AreEqual(20f, gauges.CounterGauge, 0.001f);
                Assert.IsTrue(combatController.HasCounterWindow);
                Assert.IsInstanceOf<PlayerBlockState>(stateMachine.CurrentState);
                Assert.IsTrue(((PlayerBlockState)stateMachine.CurrentState).IsInBlockStun);

                stateMachine.CurrentState.HandleHeavyAttack();

                Assert.IsInstanceOf<PlayerBlockState>(stateMachine.CurrentState);

                stateMachine.Tick(0.19f);

                Assert.IsInstanceOf<PlayerBlockState>(stateMachine.CurrentState);
                Assert.IsFalse(((PlayerBlockState)stateMachine.CurrentState).IsInBlockStun);
            }
            finally
            {
                Object.DestroyImmediate(blockStunAttack);
                Object.DestroyImmediate(balance);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ReceiveDamage_ActiveGuardBreakTakesDamageAndDoesNotOpenCounterFeedback()
        {
            GameObject playerObject = new GameObject("Player");
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();
            AttackDefinitionSO guardBreakAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                BuildPlayerDamageReceiver(
                    playerObject,
                    balance,
                    out HealthComponent health,
                    out GaugeComponent gauges,
                    out PlayerStateMachine stateMachine,
                    out PlayerCombatController combatController,
                    out DamageableReceiver receiver);
                SetPrivateField(balance, "guardStartupSeconds", 0.08f);
                SetPrivateField(balance, "guardCounterGaugeGain", 20f);
                SetPrivateField(balance, "counterWindowSeconds", 0.8f);
                SetPrivateField(guardBreakAttack, "breaksGuard", true);
                SetPrivateField(guardBreakAttack, "guardBreakHitStunSeconds", 0.16f);

                stateMachine.SwitchToBlock();
                SetPrivateField(stateMachine.CurrentState, "guardStartupRemaining", 0f);

                receiver.ReceiveDamage(30f, Vector3.zero, null, guardBreakAttack);

                Assert.AreEqual(70f, health.CurrentValue, 0.001f);
                Assert.AreEqual(0f, gauges.CounterGauge, 0.001f);
                Assert.IsFalse(combatController.HasCounterWindow);
                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerHitReactionType.GuardBreak, stateMachine.CurrentHitReactionType);

                stateMachine.Tick(0.13f);

                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerHitReactionType.GuardBreak, stateMachine.CurrentHitReactionType);

                stateMachine.Tick(0.04f);

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
            }
            finally
            {
                Object.DestroyImmediate(guardBreakAttack);
                Object.DestroyImmediate(balance);
                Object.DestroyImmediate(playerObject);
            }
        }

        private static void BuildPlayerDamageReceiver(
            GameObject playerObject,
            CombatBalanceSO balance,
            out HealthComponent health,
            out GaugeComponent gauges,
            out PlayerStateMachine stateMachine,
            out PlayerCombatController combatController,
            out DamageableReceiver receiver)
        {
            PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
            health = playerObject.AddComponent<HealthComponent>();
            gauges = playerObject.AddComponent<GaugeComponent>();
            stateMachine = playerObject.AddComponent<PlayerStateMachine>();
            combatController = playerObject.AddComponent<PlayerCombatController>();
            receiver = playerObject.AddComponent<DamageableReceiver>();
            SetPrivateField(player, "stateMachine", stateMachine);
            SetPrivateField(player, "combatController", combatController);
            SetPrivateField(combatController, "balance", balance);
            SetPrivateField(combatController, "gauges", gauges);
            SetPrivateField(receiver, "health", health);
            SetPrivateField(receiver, "playerCharacter", player);
            stateMachine.Initialize(player);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
