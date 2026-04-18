namespace CampusRPG.Character
{
    public sealed class PlayerDodgeState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private float remainingTime;
        private float invulnerableRemaining;
        private bool hasRegisteredSuccessfulDodge;

        public PlayerDodgeState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public bool IsInvulnerable => invulnerableRemaining > 0f;

        public override void Enter()
        {
            Combat.CombatBalanceSO balance = Owner.CombatController != null ? Owner.CombatController.Balance : null;

            remainingTime = balance != null ? balance.DodgeDurationSeconds : 0.25f;
            invulnerableRemaining = balance != null ? balance.DodgeInvulnerableSeconds : 0.2f;
            hasRegisteredSuccessfulDodge = false;
            Owner.CombatController?.HandleDodgeStarted();
        }

        public override void Tick(float deltaTime)
        {
            remainingTime -= deltaTime;
            invulnerableRemaining -= deltaTime;

            if (remainingTime <= 0f)
            {
                stateMachine.SwitchToLocomotion();
            }
        }

        public override void HandleLightAttack()
        {
            if (Owner.CombatController != null && Owner.CombatController.HasDodgeFollowUpWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.DodgeFollowUp);
            }
        }

        public bool TryRegisterSuccessfulDodge()
        {
            if (!IsInvulnerable || hasRegisteredSuccessfulDodge)
            {
                return false;
            }

            hasRegisteredSuccessfulDodge = true;
            Owner.CombatController?.NotifySuccessfulDodge();
            return true;
        }
    }
}
