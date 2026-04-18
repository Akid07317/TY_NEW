using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemyAttackState : EnemyState
    {
        private readonly EnemyStateMachine stateMachine;
        private float attackLockTimer;
        private float recoveryTimer;
        private bool attackExecuted;
        private AttackDefinitionSO pendingAttack;

        public EnemyAttackState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override void Enter()
        {
            pendingAttack = Owner.AttackController != null && Owner.Archetype != null
                ? Owner.AttackController.PreviewAttackForTarget(Owner.CurrentTarget, Owner.Archetype)
                : null;
            attackExecuted = false;
            recoveryTimer = 0f;
            attackLockTimer = Mathf.Max(0.2f, pendingAttack != null ? pendingAttack.StartupSeconds : 0.4f);
            Owner.Motor?.Stop();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.CurrentTarget == null || Owner.Archetype == null)
            {
                stateMachine.SwitchToIdle();
                return;
            }

            Owner.Motor?.FaceTarget(Owner.CurrentTarget);

            if (!attackExecuted)
            {
                attackLockTimer -= deltaTime;

                if (attackLockTimer > 0f)
                {
                    return;
                }

                if (pendingAttack != null)
                {
                    Owner.Motor?.AdvanceTowardsTarget(Owner.CurrentTarget, pendingAttack.ForwardMovement);
                }

                bool attackSucceeded = Owner.AttackController != null && Owner.Archetype != null
                    ? Owner.AttackController.TryAttack(Owner.CurrentTarget, Owner.Archetype)
                    : false;

                if (!attackSucceeded)
                {
                    SwitchAfterFailedAttack();
                    return;
                }

                attackExecuted = true;

                if (!ShouldHoldRecovery())
                {
                    stateMachine.SwitchToChase();
                    return;
                }

                recoveryTimer = ResolveRecoveryDuration();

                if (recoveryTimer <= 0f)
                {
                    stateMachine.SwitchToChase();
                }

                return;
            }

            recoveryTimer -= deltaTime;

            if (recoveryTimer > 0f)
            {
                return;
            }

            stateMachine.SwitchToChase();
        }

        private bool ShouldHoldRecovery()
        {
            return Owner.Archetype != null && Owner.Archetype.ArchetypeType == EnemyArchetypeType.Boss;
        }

        private float ResolveRecoveryDuration()
        {
            if (pendingAttack != null)
            {
                return Mathf.Max(0.15f, pendingAttack.RecoverySeconds);
            }

            return 0.2f;
        }

        private void SwitchAfterFailedAttack()
        {
            if (Owner.Archetype != null
                && Owner.Archetype.ArchetypeType == EnemyArchetypeType.Ranged
                && pendingAttack != null
                && pendingAttack.ProjectilePrefab != null)
            {
                stateMachine.SwitchToStrafe();
                return;
            }

            stateMachine.SwitchToChase();
        }
    }
}
