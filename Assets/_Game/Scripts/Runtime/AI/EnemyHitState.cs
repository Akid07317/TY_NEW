namespace CampusRPG.AI
{
    public sealed class EnemyHitState : EnemyState
    {
        private readonly EnemyStateMachine stateMachine;
        private float remainingTime;

        public EnemyHitState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public void SetDuration(float duration)
        {
            remainingTime = duration;
        }

        public override void Enter()
        {
            Owner.Motor?.Stop();
        }

        public override void Tick(float deltaTime)
        {
            remainingTime -= deltaTime;

            if (remainingTime > 0f)
            {
                return;
            }

            if (Owner.CurrentTarget != null)
            {
                stateMachine.SwitchToChase();
                return;
            }

            stateMachine.SwitchToIdle();
        }
    }
}
