using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        private EnemyBrain owner;
        private EnemyIdleGuardState idleState;
        private EnemyEngageState engageState;
        private EnemyChaseState chaseState;
        private EnemyStrafeState strafeState;
        private EnemyAttackState attackState;
        private EnemyHitState hitState;
        private EnemyDeathState deathState;

        [SerializeField] private string currentStateName = "IdleGuard";

        public string CurrentStateName => currentStateName;

        public EnemyState CurrentState { get; private set; }

        public void Initialize(EnemyBrain brain)
        {
            owner = brain;
            idleState = new EnemyIdleGuardState(owner, this);
            engageState = new EnemyEngageState(owner, this);
            chaseState = new EnemyChaseState(owner, this);
            strafeState = new EnemyStrafeState(owner, this);
            attackState = new EnemyAttackState(owner, this);
            hitState = new EnemyHitState(owner, this);
            deathState = new EnemyDeathState(owner, this);
            SwitchState(idleState, nameof(EnemyIdleGuardState));
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime);
        }

        public void SetState(string nextStateName)
        {
            currentStateName = nextStateName;
        }

        public void SwitchToIdle()
        {
            SwitchState(idleState, nameof(EnemyIdleGuardState));
        }

        public void SwitchToChase()
        {
            SwitchState(chaseState, nameof(EnemyChaseState));
        }

        public void SwitchToEngage()
        {
            SwitchState(engageState, nameof(EnemyEngageState));
        }

        public void SwitchToAttack()
        {
            SwitchState(attackState, nameof(EnemyAttackState));
        }

        public void SwitchToStrafe()
        {
            SwitchState(strafeState, nameof(EnemyStrafeState));
        }

        public void SwitchToDeath()
        {
            SwitchState(deathState, nameof(EnemyDeathState));
        }

        public void SwitchToHit(float duration)
        {
            hitState.SetDuration(duration);
            SwitchState(hitState, nameof(EnemyHitState));
        }

        private void SwitchState(EnemyState nextState, string stateName)
        {
            CurrentState?.Exit();
            CurrentState = nextState;
            currentStateName = stateName;
            CurrentState?.Enter();
        }
    }
}
