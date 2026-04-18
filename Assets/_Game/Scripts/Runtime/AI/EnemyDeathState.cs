namespace CampusRPG.AI
{
    public sealed class EnemyDeathState : EnemyState
    {
        public EnemyDeathState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
        }

        public override void Enter()
        {
            Owner.Motor?.Stop();
            Owner.ClearTarget();
        }
    }
}
