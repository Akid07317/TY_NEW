namespace CampusRPG.Character
{
    public sealed class PlayerHitState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private float remainingTime;

        public PlayerHitState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public void SetDuration(float duration)
        {
            remainingTime = duration;
        }

        public override void Tick(float deltaTime)
        {
            remainingTime -= deltaTime;

            if (remainingTime <= 0f)
            {
                stateMachine.SwitchToLocomotion();
            }
        }
    }
}
