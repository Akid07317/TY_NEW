namespace CampusRPG.Character
{
    public sealed class PlayerLocomotionState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;

        public PlayerLocomotionState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.InputReader != null && Owner.InputReader.IsBlockHeld)
            {
                stateMachine.SwitchToBlock();
            }
        }

        public override void HandleLightAttack()
        {
            if (Owner.CombatController != null && Owner.CombatController.HasDodgeFollowUpWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.DodgeFollowUp);
                return;
            }

            if (IsAirborneWithoutExecutableSwordArt())
            {
                return;
            }

            stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
        }

        public override void HandleHeavyAttack()
        {
            if (Owner.CombatController != null && Owner.CombatController.HasCounterWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Counter);
                return;
            }

            if (IsAirborneWithoutExecutableSwordArt())
            {
                return;
            }

            stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
        }

        public override void HandleDodge()
        {
            if (IsAirborne())
            {
                stateMachine.SwitchToAirDodge();
                return;
            }

            stateMachine.SwitchToDodge(ResolveGroundEvasiveActionType());
        }

        public override void HandleSkill1()
        {
            stateMachine.SwitchToSkill(0);
        }

        public override void HandleSkill2()
        {
            stateMachine.SwitchToSkill(1);
        }

        private bool IsAirborneWithoutExecutableSwordArt()
        {
            return IsAirborne()
                && (Owner.CombatController == null || !Owner.CombatController.TryPreviewBufferedSwordArt(out _, out _));
        }

        private bool IsAirborne()
        {
            return Owner.Motor != null && !Owner.Motor.IsGrounded;
        }

        private PlayerEvasiveActionType ResolveGroundEvasiveActionType()
        {
            bool hasLockOnTarget = Owner.LockOnTargetSelector != null && Owner.LockOnTargetSelector.CurrentTarget != null;
            bool hasCommittedMoveInput = Owner.InputReader != null && Owner.InputReader.MoveValue.sqrMagnitude >= 0.25f;
            return !hasLockOnTarget && hasCommittedMoveInput
                ? PlayerEvasiveActionType.CombatRoll
                : PlayerEvasiveActionType.GroundDodge;
        }
    }
}
