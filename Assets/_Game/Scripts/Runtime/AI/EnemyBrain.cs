using CampusRPG.Combat;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.AI
{
    [RequireComponent(typeof(DamageableReceiver))]
    public sealed class EnemyBrain : MonoBehaviour, ICheckpointRestoreParticipant
    {
        [SerializeField] private EnemyArchetypeSO archetype;
        [SerializeField] private Transform currentTarget;
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private EnemySensing sensing;
        [SerializeField] private EnemyMotor motor;
        [SerializeField] private EnemyAttackController attackController;
        [SerializeField] private HealthComponent health;

        public EnemyArchetypeSO Archetype => archetype;

        public CheckpointRestoreGroup RestoreGroup => CheckpointRestoreGroup.Enemy;

        public int RestorePriority => 0;

        public Transform CurrentTarget => currentTarget;

        public EnemyStateMachine StateMachine => stateMachine;

        public EnemySensing Sensing => sensing;

        public EnemyMotor Motor => motor;

        public EnemyAttackController AttackController => attackController;

        public HealthComponent Health => health;

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }

            if (sensing == null)
            {
                sensing = GetComponent<EnemySensing>();
            }

            if (motor == null)
            {
                motor = GetComponent<EnemyMotor>();
            }

            if (attackController == null)
            {
                attackController = GetComponent<EnemyAttackController>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            if (archetype != null && health != null)
            {
                health.SetMax(archetype.MaxHealth, true);
            }

            if (motor != null && archetype != null)
            {
                motor.SetMoveSpeed(archetype.MoveSpeed);
            }

            stateMachine?.Initialize(this);
        }

        private void OnEnable()
        {
            CheckpointRestoreSceneResetter.RegisterParticipant(this);

            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            CheckpointRestoreSceneResetter.UnregisterParticipant(this);

            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                return;
            }

            if (currentTarget != null && !HasLivingTarget(currentTarget))
            {
                ClearTarget();
            }

            attackController?.Tick(Time.deltaTime);
            stateMachine?.Tick(Time.deltaTime);
        }

        public void SetTarget(Transform target)
        {
            currentTarget = HasLivingTarget(target) ? target : null;
        }

        public void ClearTarget()
        {
            currentTarget = null;
        }

        public void ResetForCheckpointRestore()
        {
            attackController?.ResetRuntimeState();

            if (health != null)
            {
                if (archetype != null)
                {
                    health.SetMax(archetype.MaxHealth, true);
                }
                else
                {
                    health.RestoreFull();
                }
            }

            ClearTarget();
            motor?.Stop();

            if (stateMachine != null)
            {
                if (stateMachine.CurrentState == null)
                {
                    stateMachine.Initialize(this);
                }
                else
                {
                    stateMachine.SwitchToIdle();
                }
            }
        }

        private void HandleDeath()
        {
            stateMachine?.SwitchToDeath();
        }

        private static bool HasLivingTarget(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            HealthComponent targetHealth = target.GetComponentInParent<HealthComponent>();
            return targetHealth == null || !targetHealth.IsDead;
        }
    }
}
