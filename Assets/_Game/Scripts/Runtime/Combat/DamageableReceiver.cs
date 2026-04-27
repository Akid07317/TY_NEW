using CampusRPG.AI;
using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Combat
{
    public sealed class DamageableReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private float playerHitStunSeconds = 0.08f;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            if (playerCharacter == null)
            {
                playerCharacter = GetComponent<PlayerCharacter>();
            }

            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }
        }

        public void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source)
        {
            ReceiveDamage(amount, hitPoint, source, null);
        }

        public void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source, AttackDefinitionSO incomingAttack)
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            DamageDefenseOutcome defenseOutcome = DamageableReactionUtility.ResolveDefenseOutcome(
                playerCharacter,
                incomingAttack);

            if (defenseOutcome == DamageDefenseOutcome.SuccessfulDodge)
            {
                return;
            }

            if (defenseOutcome == DamageDefenseOutcome.SuccessfulBlock)
            {
                playerCharacter.StateMachine?.ApplyBlockStun(incomingAttack != null ? incomingAttack.BlockStunSeconds : 0f);
                playerCharacter.CombatController?.NotifySuccessfulBlock();

                return;
            }

            health.ReceiveDamage(amount, hitPoint, source);

            if (health.IsDead)
            {
                return;
            }

            DamageableReactionPlan reactionPlan = DamageableReactionUtility.BuildPostDamageReaction(
                playerCharacter,
                enemyBrain,
                source,
                DamageableReactionUtility.ResolvePlayerHitStunSeconds(
                    playerHitStunSeconds,
                    defenseOutcome,
                    incomingAttack));

            if (reactionPlan.PlayerHitStunSeconds > 0f)
            {
                PlayerHitReactionType reactionType = defenseOutcome == DamageDefenseOutcome.GuardBroken
                    ? PlayerHitReactionType.GuardBreak
                    : PlayerHitReactionType.Standard;

                playerCharacter.StateMachine?.SwitchToHit(reactionPlan.PlayerHitStunSeconds, reactionType);
            }

            if (enemyBrain != null && reactionPlan.EnemyTarget != null)
            {
                enemyBrain.SetTarget(reactionPlan.EnemyTarget);
            }

            if (enemyBrain != null && reactionPlan.SwitchEnemyToChase)
            {
                enemyBrain.StateMachine?.SwitchToChase();
            }

            if (enemyBrain != null && reactionPlan.EnemyHitStunSeconds > 0f)
            {
                enemyBrain.StateMachine?.SwitchToHit(reactionPlan.EnemyHitStunSeconds);
            }
        }
    }
}
