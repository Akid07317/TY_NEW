using UnityEngine;

namespace CampusRPG.Character
{
    public sealed class PlayerBlockState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private float guardStartupRemaining;
        private float blockStunRemaining;

        public PlayerBlockState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public bool IsGuardActive => guardStartupRemaining <= 0f;

        public bool IsInBlockStun => blockStunRemaining > 0f;

        public override void Enter()
        {
            guardStartupRemaining = ResolveGuardStartupSeconds();
            blockStunRemaining = 0f;
        }

        public override void Tick(float deltaTime)
        {
            float clampedDeltaTime = Mathf.Max(0f, deltaTime);
            guardStartupRemaining = Mathf.Max(0f, guardStartupRemaining - clampedDeltaTime);

            if (blockStunRemaining > 0f)
            {
                blockStunRemaining = Mathf.Max(0f, blockStunRemaining - clampedDeltaTime);
                return;
            }

            if (Owner.InputReader == null || !Owner.InputReader.IsBlockHeld)
            {
                stateMachine.SwitchToLocomotion();
                return;
            }
        }

        public override void HandleHeavyAttack()
        {
            if (blockStunRemaining > 0f)
            {
                return;
            }

            if (Owner.CombatController != null && Owner.CombatController.HasCounterWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Counter);
            }
        }

        public override void HandleDodge()
        {
            if (blockStunRemaining > 0f)
            {
                return;
            }

            stateMachine.SwitchToDodge(PlayerEvasiveActionType.CombatRoll);
        }

        public void ApplyBlockStun(float durationSeconds)
        {
            blockStunRemaining = Mathf.Max(blockStunRemaining, Mathf.Max(0f, durationSeconds));
        }

        private float ResolveGuardStartupSeconds()
        {
            Combat.CombatBalanceSO balance = Owner.CombatController != null ? Owner.CombatController.Balance : null;
            return balance != null ? Mathf.Max(0f, balance.GuardStartupSeconds) : 0f;
        }
    }
}
