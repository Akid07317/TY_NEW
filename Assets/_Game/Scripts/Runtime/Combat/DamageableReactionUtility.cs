using CampusRPG.AI;
using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Combat
{
    public enum DamageDefenseOutcome
    {
        None,
        SuccessfulDodge,
        SuccessfulBlock,
        GuardBroken
    }

    public readonly struct DamageableReactionPlan
    {
        public DamageableReactionPlan(
            float playerHitStunSeconds,
            float enemyHitStunSeconds,
            Transform enemyTarget,
            bool switchEnemyToChase)
        {
            PlayerHitStunSeconds = playerHitStunSeconds;
            EnemyHitStunSeconds = enemyHitStunSeconds;
            EnemyTarget = enemyTarget;
            SwitchEnemyToChase = switchEnemyToChase;
        }

        public float PlayerHitStunSeconds { get; }

        public float EnemyHitStunSeconds { get; }

        public Transform EnemyTarget { get; }

        public bool SwitchEnemyToChase { get; }
    }

    public static class DamageableReactionUtility
    {
        public static DamageDefenseOutcome ResolveDefenseOutcome(
            PlayerCharacter playerCharacter,
            AttackDefinitionSO incomingAttack = null)
        {
            if (playerCharacter == null || playerCharacter.StateMachine == null)
            {
                return DamageDefenseOutcome.None;
            }

            if (playerCharacter.StateMachine.TryNotifySuccessfulDodge())
            {
                return DamageDefenseOutcome.SuccessfulDodge;
            }

            if (!playerCharacter.StateMachine.HasActiveGuard)
            {
                return DamageDefenseOutcome.None;
            }

            return incomingAttack != null && incomingAttack.BreaksGuard
                ? DamageDefenseOutcome.GuardBroken
                : DamageDefenseOutcome.SuccessfulBlock;
        }

        public static float ResolvePlayerHitStunSeconds(
            float defaultHitStunSeconds,
            DamageDefenseOutcome defenseOutcome,
            AttackDefinitionSO incomingAttack)
        {
            float hitStunSeconds = Mathf.Max(0f, defaultHitStunSeconds);

            if (defenseOutcome == DamageDefenseOutcome.GuardBroken && incomingAttack != null)
            {
                hitStunSeconds = Mathf.Max(hitStunSeconds, incomingAttack.GuardBreakHitStunSeconds);
            }

            return hitStunSeconds;
        }

        public static DamageableReactionPlan BuildPostDamageReaction(
            PlayerCharacter playerCharacter,
            EnemyBrain enemyBrain,
            GameObject source,
            float playerHitStunSeconds)
        {
            Transform enemyTarget = null;
            bool switchEnemyToChase = false;

            if (enemyBrain != null && TryResolveAggroTarget(source, out Transform attackerTarget))
            {
                enemyTarget = attackerTarget;
                switchEnemyToChase = true;
            }

            return new DamageableReactionPlan(
                playerCharacter != null ? Mathf.Max(0f, playerHitStunSeconds) : 0f,
                enemyBrain != null
                    ? enemyBrain.Archetype != null
                        ? enemyBrain.Archetype.HitStunSeconds
                        : 0.15f
                    : 0f,
                enemyTarget,
                switchEnemyToChase);
        }

        public static bool TryResolveAggroTarget(GameObject source, out Transform target)
        {
            target = null;

            if (source == null)
            {
                return false;
            }

            PlayerCharacter attacker = source.GetComponentInParent<PlayerCharacter>();

            if (attacker == null)
            {
                return false;
            }

            target = attacker.transform;
            return true;
        }
    }
}
