using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemyIdleGuardState : EnemyState
    {
        private readonly EnemyStateMachine stateMachine;

        public EnemyIdleGuardState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override void Enter()
        {
            Owner.Motor?.Stop();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.Sensing == null || Owner.Archetype == null)
            {
                return;
            }

            Transform target = Owner.Sensing.FindTarget(Owner.transform.position, Owner.Archetype.AggroDistance);

            if (target == null)
            {
                return;
            }

            Owner.SetTarget(target);

            if (Owner.Archetype.ArchetypeType == EnemyArchetypeType.Boss && Owner.Archetype.EngageDurationSeconds > 0f)
            {
                stateMachine.SwitchToEngage();
                return;
            }

            stateMachine.SwitchToChase();
        }
    }
}
