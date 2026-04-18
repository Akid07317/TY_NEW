using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemyEngageState : EnemyState
    {
        private readonly EnemyStateMachine stateMachine;
        private float remainingTime;

        public EnemyEngageState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override void Enter()
        {
            remainingTime = Owner.Archetype != null ? Owner.Archetype.EngageDurationSeconds : 0f;
            Owner.Motor?.Stop();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.CurrentTarget == null || Owner.Archetype == null)
            {
                stateMachine.SwitchToIdle();
                return;
            }

            Owner.Motor?.FaceTarget(Owner.CurrentTarget);
            remainingTime -= deltaTime;

            if (remainingTime > 0f)
            {
                return;
            }

            stateMachine.SwitchToChase();
        }
    }
}
