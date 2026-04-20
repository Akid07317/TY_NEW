using UnityEngine;

namespace CampusRPG.Character
{
    public sealed class PlayerMantleState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private Vector3 targetPosition;
        private float durationSeconds;

        public PlayerMantleState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public void Configure(Vector3 mantleTargetPosition, float mantleDurationSeconds)
        {
            targetPosition = mantleTargetPosition;
            durationSeconds = Mathf.Max(0.01f, mantleDurationSeconds);
        }

        public override void Enter()
        {
            if (Owner.Motor == null || !Owner.Motor.BeginMantle(targetPosition, durationSeconds))
            {
                stateMachine.SwitchToLocomotion();
            }
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.Motor == null || !Owner.Motor.IsMantling)
            {
                stateMachine.SwitchToLocomotion();
            }
        }
    }
}
