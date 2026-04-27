namespace CampusRPG.Character
{
    public abstract class PlayerState
    {
        protected PlayerState(PlayerCharacter owner)
        {
            Owner = owner;
        }

        protected PlayerCharacter Owner { get; }

        public virtual bool AllowsMovement => true;

        public virtual bool AllowsJump => true;

        public virtual float MovementSpeedScale => 1f;

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void HandleLightAttack()
        {
        }

        public virtual void HandleHeavyAttack()
        {
        }

        public virtual void HandleDodge()
        {
        }

        public virtual void HandleSkill1()
        {
        }

        public virtual void HandleSkill2()
        {
        }
    }
}
