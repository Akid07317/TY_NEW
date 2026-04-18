using CampusRPG.AI;
using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Combat
{
    public enum DamageDefenseOutcome
    {
        None,
        SuccessfulDodge,
        SuccessfulBlock
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
        public static DamageDefenseOutcome ResolveDefenseOutcome(PlayerCharacter playerCharacter)
        {
            if (playerCharacter == null || playerCharacter.StateMachine == null)
            {
                return DamageDefenseOutcome.None;
            }

            if (playerCharacter.StateMachine.TryNotifySuccessfulDodge())
            {
                return DamageDefenseOutcome.SuccessfulDodge;
            }

            return playerCharacter.StateMachine.IsBlocking
                ? DamageDefenseOutcome.SuccessfulBlock
                : DamageDefenseOutcome.None;
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
