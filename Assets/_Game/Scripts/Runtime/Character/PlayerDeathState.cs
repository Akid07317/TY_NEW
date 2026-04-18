namespace CampusRPG.Character
{
    public sealed class PlayerDeathState : PlayerState
    {
        public PlayerDeathState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public override void Enter()
        {
            Owner.CombatController?.ResetRuntimeState();
            Owner.Motor?.ResetMotion();
        }
    }
}
