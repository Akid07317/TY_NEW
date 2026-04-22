using UnityEngine;

namespace CampusRPG.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(EnemyStateMachine))]
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyCombatAnimationRelay : MonoBehaviour
    {
        private static readonly int GroundSpeedHash = Animator.StringToHash(EnemyCombatAnimationPlanUtility.GroundSpeedParameterName);

        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private Animator animator;
        [SerializeField] private int baseLayerIndex;
        [SerializeField] private float crossFadeSeconds = 0.08f;
        [SerializeField] private float locomotionDampSeconds = 0.08f;

        private Vector3 lastWorldPosition;
        private string currentAnimatorStateName = string.Empty;
        private int lastObservedStateRevision = -1;

        private void Awake()
        {
            EnsureReferences();
            lastWorldPosition = transform.position;
            lastObservedStateRevision = -1;
        }

        private void OnEnable()
        {
            EnsureReferences();
            lastWorldPosition = transform.position;
            currentAnimatorStateName = string.Empty;
            lastObservedStateRevision = -1;
        }

        private void Update()
        {
            if (!EnsureReferences() || animator.runtimeAnimatorController == null || animator.avatar == null)
            {
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            EnemyArchetypeType archetypeType = enemyBrain != null && enemyBrain.Archetype != null
                ? enemyBrain.Archetype.ArchetypeType
                : EnemyArchetypeType.Melee;
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                archetypeType,
                stateMachine != null ? stateMachine.CurrentStateName : string.Empty,
                ResolveMoveSpeedNormalized(deltaTime));
            int stateRevision = stateMachine != null ? stateMachine.StateRevision : -1;
            bool shouldRestartCurrentState = stateRevision != lastObservedStateRevision
                && ShouldRestartClipOnStateReenter(plan.StateName);

            animator.SetFloat(GroundSpeedHash, plan.GroundSpeedNormalized, locomotionDampSeconds, Time.deltaTime);

            if (!shouldRestartCurrentState
                && string.Equals(currentAnimatorStateName, plan.StateName, System.StringComparison.Ordinal))
            {
                lastObservedStateRevision = stateRevision;
                return;
            }

            lastObservedStateRevision = stateRevision;
            currentAnimatorStateName = plan.StateName;
            float transitionSeconds = shouldRestartCurrentState
                ? Mathf.Min(Mathf.Max(0f, crossFadeSeconds), 0.04f)
                : Mathf.Max(0f, crossFadeSeconds);
            animator.CrossFadeInFixedTime(currentAnimatorStateName, transitionSeconds, baseLayerIndex, 0f, 0f);
        }

        private bool EnsureReferences()
        {
            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            return enemyBrain != null && stateMachine != null && animator != null;
        }

        private float ResolveMoveSpeedNormalized(float deltaTime)
        {
            Vector3 planarDelta = transform.position - lastWorldPosition;
            planarDelta.y = 0f;
            lastWorldPosition = transform.position;

            float speed = planarDelta.magnitude / deltaTime;
            float baselineMoveSpeed = enemyBrain != null && enemyBrain.Archetype != null
                ? Mathf.Max(0.01f, enemyBrain.Archetype.MoveSpeed)
                : 3.5f;
            return Mathf.Clamp01(speed / baselineMoveSpeed);
        }

        private static bool ShouldRestartClipOnStateReenter(string stateName)
        {
            return !string.Equals(stateName, EnemyCombatAnimationPlanUtility.LocomotionStateName, System.StringComparison.Ordinal);
        }
    }
}
