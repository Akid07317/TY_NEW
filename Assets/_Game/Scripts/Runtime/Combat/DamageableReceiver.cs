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
        [SerializeField] private float playerHitStunSeconds = 0.2f;

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
            if (health == null || health.IsDead)
            {
                return;
            }

            DamageDefenseOutcome defenseOutcome = DamageableReactionUtility.ResolveDefenseOutcome(playerCharacter);

            if (defenseOutcome != DamageDefenseOutcome.None)
            {
                if (defenseOutcome == DamageDefenseOutcome.SuccessfulBlock)
                {
                    playerCharacter.CombatController?.NotifySuccessfulBlock();
                }

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
                playerHitStunSeconds);

            if (reactionPlan.PlayerHitStunSeconds > 0f)
            {
                playerCharacter.StateMachine?.SwitchToHit(reactionPlan.PlayerHitStunSeconds);
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
