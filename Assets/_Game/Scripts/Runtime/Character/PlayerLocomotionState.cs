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

            stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
        }

        public override void HandleHeavyAttack()
        {
            if (Owner.CombatController != null && Owner.CombatController.HasCounterWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Counter);
                return;
            }

            stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
        }

        public override void HandleDodge()
        {
            stateMachine.SwitchToDodge();
        }

        public override void HandleSkill1()
        {
            stateMachine.SwitchToSkill(0);
        }

        public override void HandleSkill2()
        {
            stateMachine.SwitchToSkill(1);
        }
    }
}
