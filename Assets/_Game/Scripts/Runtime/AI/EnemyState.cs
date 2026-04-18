namespace CampusRPG.AI
{
    public abstract class EnemyState
    {
        protected EnemyState(EnemyBrain owner)
        {
            Owner = owner;
        }

        protected EnemyBrain Owner { get; }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }
    }
}
