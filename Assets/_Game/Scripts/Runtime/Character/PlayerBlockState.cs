namespace CampusRPG.Character
{
    public sealed class PlayerBlockState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;

        public PlayerBlockState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public override void Tick(float deltaTime)
        {
            if (Owner.InputReader == null || !Owner.InputReader.IsBlockHeld)
            {
                stateMachine.SwitchToLocomotion();
            }
        }

        public override void HandleHeavyAttack()
        {
            if (Owner.CombatController != null && Owner.CombatController.HasCounterWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Counter);
            }
        }
    }
}
