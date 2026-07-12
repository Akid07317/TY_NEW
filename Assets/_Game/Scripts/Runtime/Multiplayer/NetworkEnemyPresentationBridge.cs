using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class NetworkEnemyPresentationBridge : MonoBehaviour
    {
        private const float DefaultAuthoritativeAttackPresentationSeconds = 0.35f;

        [SerializeField] private NetworkEnemyAvatar avatar;
        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private HealthComponent health;
        [SerializeField] private EnemySensing sensing;
        [SerializeField] private EnemyMotor motor;
        [SerializeField] private EnemyAttackController attackController;
        [SerializeField] private NavMeshAgent navMeshAgent;

        private bool localEnemyDriverSuppressed;
        private bool hasAuthoritativeAttackPresentationSample;
        private uint lastAuthoritativeAttackPresentationSequence;
        private bool hasObservedFormalAttackPresentation;
        private float formalAttackPresentationTimer;

        public bool LocalEnemyDriverSuppressed => localEnemyDriverSuppressed;

        public bool IsFormalDeathStateActive =>
            health != null
            && health.IsDead
            && stateMachine != null
            && (stateMachine.CurrentState is EnemyDeathState
                || stateMachine.CurrentStateName == nameof(EnemyDeathState));

        public bool IsFormalAttackStateActive =>
            health != null
            && !health.IsDead
            && stateMachine != null
            && (stateMachine.CurrentState is EnemyAttackState
                || stateMachine.CurrentStateName == nameof(EnemyAttackState));

        public bool HasObservedFormalAttackPresentation => hasObservedFormalAttackPresentation;

        public void Configure(
            NetworkEnemyAvatar networkAvatar,
            EnemyBrain formalEnemyBrain,
            EnemyStateMachine formalStateMachine,
            HealthComponent formalHealth,
            EnemySensing formalSensing,
            EnemyMotor formalMotor,
            EnemyAttackController formalAttackController,
            NavMeshAgent formalNavMeshAgent)
        {
            avatar = networkAvatar;
            enemyBrain = formalEnemyBrain;
            stateMachine = formalStateMachine;
            health = formalHealth;
            sensing = formalSensing;
            motor = formalMotor;
            attackController = formalAttackController;
            navMeshAgent = formalNavMeshAgent;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            EnsureStateMachineInitialized();
            ApplyEnemyDriverAuthority(avatar != null && avatar.ShouldAllowServerFormalEnemyDriver);
            ConstrainPresentationTransform(transform);
        }

        private void Update()
        {
            ResolveReferences();
            EnsureStateMachineInitialized();

            if (avatar == null)
            {
                ApplyEnemyDriverAuthority(false);
                return;
            }

            ApplyEnemyDriverAuthority(avatar.ShouldAllowServerFormalEnemyDriver);
            ApplyAuthoritativeState(avatar.CurrentHealth, avatar.IsDead);
            ApplyAuthoritativeAttackPresentation(
                avatar.AttackPresentationSequence,
                avatar.AttackPresentationCode,
                avatar.IsDead);
            TickFormalAttackPresentation(Time.deltaTime, avatar.IsDead);
        }

        private void LateUpdate()
        {
            if (avatar != null && avatar.ShouldAllowServerFormalEnemyDriver)
            {
                avatar.CommitServerFormalDriverPose(transform.position, transform.rotation);
            }

            ConstrainPresentationTransform(transform);
        }

        public bool ApplyEnemyDriverAuthority(bool allowLocalDriver)
        {
            SetEnabled(enemyBrain, allowLocalDriver);
            SetEnabled(sensing, allowLocalDriver);
            SetEnabled(motor, allowLocalDriver);
            SetEnabled(attackController, allowLocalDriver);
            SetEnabled(navMeshAgent, allowLocalDriver);

            localEnemyDriverSuppressed = !allowLocalDriver
                && IsDisabled(enemyBrain)
                && IsDisabled(sensing)
                && IsDisabled(motor)
                && IsDisabled(attackController)
                && IsDisabled(navMeshAgent);

            return localEnemyDriverSuppressed;
        }

        public bool ApplyAuthoritativeState(int authoritativeHealth, bool authoritativeDead)
        {
            return ApplyAuthoritativeEnemyPresentationState(
                health,
                stateMachine,
                transform.position,
                avatar != null ? avatar.gameObject : gameObject,
                authoritativeHealth,
                authoritativeDead);
        }

        public bool ApplyAuthoritativeAttackPresentation(
            uint attackPresentationSequence,
            int attackPresentationCode,
            bool authoritativeDead)
        {
            bool isNewPresentation =
                !hasAuthoritativeAttackPresentationSample
                || attackPresentationSequence != lastAuthoritativeAttackPresentationSequence;

            hasAuthoritativeAttackPresentationSample = true;
            lastAuthoritativeAttackPresentationSequence = attackPresentationSequence;

            if (!isNewPresentation || attackPresentationSequence == 0u)
            {
                return false;
            }

            bool applied = ApplyAuthoritativeEnemyAttackPresentationState(
                health,
                stateMachine,
                attackPresentationCode,
                authoritativeDead);

            if (applied)
            {
                hasObservedFormalAttackPresentation = true;
                formalAttackPresentationTimer = DefaultAuthoritativeAttackPresentationSeconds;
            }

            return applied;
        }

        public static bool ApplyAuthoritativeEnemyPresentationState(
            HealthComponent health,
            EnemyStateMachine stateMachine,
            Vector3 hitPoint,
            GameObject source,
            int authoritativeHealth,
            bool authoritativeDead)
        {
            bool applied = false;
            int clampedHealth = Mathf.Max(0, authoritativeHealth);

            if (authoritativeDead)
            {
                if (health != null && !health.IsDead)
                {
                    health.ReceiveDamage(float.MaxValue, hitPoint, source);
                    applied = true;
                }

                if (health != null && !Mathf.Approximately(health.CurrentValue, 0f))
                {
                    health.SetCurrent(0f);
                    applied = true;
                }

                if (stateMachine != null
                    && stateMachine.CurrentState is not EnemyDeathState
                    && stateMachine.CurrentStateName != nameof(EnemyDeathState))
                {
                    stateMachine.SwitchToDeath();
                    applied = true;
                }

                return applied;
            }

            if (health != null && !Mathf.Approximately(health.CurrentValue, clampedHealth))
            {
                health.SetCurrent(clampedHealth);
                applied = true;
            }

            if (stateMachine != null
                && clampedHealth > 0
                && (stateMachine.CurrentState is EnemyDeathState
                    || stateMachine.CurrentStateName == nameof(EnemyDeathState)))
            {
                stateMachine.SwitchToIdle();
                applied = true;
            }

            return applied;
        }

        public static bool ApplyAuthoritativeEnemyAttackPresentationState(
            HealthComponent health,
            EnemyStateMachine stateMachine,
            int attackPresentationCode,
            bool authoritativeDead)
        {
            if (stateMachine == null
                || authoritativeDead
                || attackPresentationCode == NetworkEnemyAvatar.NoAttackPresentationCode
                || (health != null && health.IsDead)
                || stateMachine.CurrentState is EnemyDeathState
                || stateMachine.CurrentStateName == nameof(EnemyDeathState))
            {
                return false;
            }

            stateMachine.SwitchToAttack();
            return stateMachine.CurrentState is EnemyAttackState
                || stateMachine.CurrentStateName == nameof(EnemyAttackState);
        }

        public static void ConstrainPresentationTransform(Transform presentationRoot)
        {
            if (presentationRoot == null)
            {
                return;
            }

            presentationRoot.localPosition = Vector3.zero;
            presentationRoot.localRotation = Quaternion.identity;
            presentationRoot.localScale = Vector3.one;
        }

        private void EnsureStateMachineInitialized()
        {
            if (stateMachine != null && stateMachine.CurrentState == null && enemyBrain != null)
            {
                stateMachine.Initialize(enemyBrain);
            }
        }

        private void TickFormalAttackPresentation(float deltaTime, bool authoritativeDead)
        {
            if (authoritativeDead || !IsFormalAttackStateActive)
            {
                formalAttackPresentationTimer = 0f;
                return;
            }

            formalAttackPresentationTimer -= Mathf.Max(0f, deltaTime);
            if (formalAttackPresentationTimer <= 0f)
            {
                stateMachine.SwitchToIdle();
            }
        }

        private void ResolveReferences()
        {
            if (avatar == null)
            {
                avatar = GetComponentInParent<NetworkEnemyAvatar>();
            }

            if (avatar == null)
            {
                avatar = GetComponentInChildren<NetworkEnemyAvatar>(true);
            }

            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }

            if (enemyBrain == null)
            {
                enemyBrain = GetComponentInChildren<EnemyBrain>(true);
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }

            if (stateMachine == null && enemyBrain != null)
            {
                stateMachine = enemyBrain.StateMachine;
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            if (health == null && enemyBrain != null)
            {
                health = enemyBrain.Health;
            }

            if (sensing == null)
            {
                sensing = GetComponent<EnemySensing>();
            }

            if (sensing == null && enemyBrain != null)
            {
                sensing = enemyBrain.Sensing;
            }

            if (motor == null)
            {
                motor = GetComponent<EnemyMotor>();
            }

            if (motor == null && enemyBrain != null)
            {
                motor = enemyBrain.Motor;
            }

            if (attackController == null)
            {
                attackController = GetComponent<EnemyAttackController>();
            }

            if (attackController == null && enemyBrain != null)
            {
                attackController = enemyBrain.AttackController;
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }
        }

        private static void SetEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour != null && behaviour.enabled != enabled)
            {
                behaviour.enabled = enabled;
            }
        }

        private static bool IsDisabled(Behaviour behaviour)
        {
            return behaviour == null || !behaviour.enabled;
        }
    }
}
