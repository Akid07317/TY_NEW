using CampusRPG.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyBrain))]
    [RequireComponent(typeof(EnemyStateMachine))]
    public sealed class EnemyCombatAnimationRelay : MonoBehaviour
    {
        private const string ImportedVisualRootName = "ImportedEnemyVisualRoot";
        private static readonly int GroundSpeedHash = Animator.StringToHash(EnemyCombatAnimationPlanUtility.GroundSpeedParameterName);
        private static readonly int ResponseReadHash = Animator.StringToHash(EnemyCombatAnimationPlanUtility.ResponseReadParameterName);
        private static readonly int AntiAirReadHash = Animator.StringToHash(EnemyCombatAnimationPlanUtility.AntiAirReadParameterName);
        private static readonly int ChaseRollReadHash = Animator.StringToHash(EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName);
        private static readonly int GuardBreakReadHash = Animator.StringToHash(EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName);
        private const string CombatPoseLayerName = "CombatPose";

        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private int baseLayerIndex;
        [SerializeField] private float crossFadeSeconds = 0.05f;
        [SerializeField] private float locomotionDampSeconds = 0.04f;

        private Vector3 lastWorldPosition;
        private string currentAnimatorStateName = string.Empty;
        private int lastObservedStateRevision = -1;
        private int combatPoseLayerIndex = -1;
        private Animator preparedAnimator;

        private void Awake()
        {
            EnsureReferences();
            lastWorldPosition = transform.position;
            lastObservedStateRevision = -1;
            combatPoseLayerIndex = -1;
        }

        private void OnEnable()
        {
            EnsureReferences();
            lastWorldPosition = transform.position;
            currentAnimatorStateName = string.Empty;
            lastObservedStateRevision = -1;
            combatPoseLayerIndex = -1;
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
            ResolveCurrentAttackContext(
                out AttackDefinitionSO attackDefinition,
                out EnemyAttackPresentationPhase attackPhase,
                out float attackProgress);
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                archetypeType,
                stateMachine != null ? stateMachine.CurrentStateName : string.Empty,
                ResolveMoveSpeedNormalized(deltaTime),
                attackDefinition,
                attackPhase,
                attackProgress);
            string animatorStateName = ResolvePlayableStateName(plan);
            int stateRevision = stateMachine != null ? stateMachine.StateRevision : -1;
            bool shouldRestartCurrentState = stateRevision != lastObservedStateRevision
                && ShouldRestartClipOnStateReenter(animatorStateName);

            animator.SetFloat(GroundSpeedHash, plan.GroundSpeedNormalized, locomotionDampSeconds, Time.deltaTime);
            UpdateResponseReadParameters(plan, deltaTime);
            UpdateCombatPoseLayer(animatorStateName, plan.GroundSpeedNormalized, deltaTime);

            if (!shouldRestartCurrentState
                && string.Equals(currentAnimatorStateName, animatorStateName, System.StringComparison.Ordinal))
            {
                lastObservedStateRevision = stateRevision;
                return;
            }

            lastObservedStateRevision = stateRevision;
            currentAnimatorStateName = animatorStateName;
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

            Animator importedAnimator = FindImportedPreviewAnimator();

            if (importedAnimator != null
                && (animator == null || animator.transform == transform || !CanSampleHumanoid(animator)))
            {
                animator = importedAnimator;
                currentAnimatorStateName = string.Empty;
                combatPoseLayerIndex = -1;
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            PrepareDrivenAnimator();
            return enemyBrain != null && stateMachine != null && animator != null;
        }

        private Animator FindImportedPreviewAnimator()
        {
            Transform importedVisualRoot = transform.Find(ImportedVisualRootName);
            return importedVisualRoot != null
                ? importedVisualRoot.GetComponentInChildren<Animator>(true)
                : null;
        }

        private static bool CanSampleHumanoid(Animator candidate)
        {
            return candidate != null
                && candidate.avatar != null
                && candidate.avatar.isValid;
        }

        private void PrepareDrivenAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            if (preparedAnimator == animator)
            {
                return;
            }

            preparedAnimator = animator;
            currentAnimatorStateName = string.Empty;
            combatPoseLayerIndex = -1;

            if (animator.isActiveAndEnabled)
            {
                animator.Rebind();
            }
        }

        private void ResolveCurrentAttackContext(
            out AttackDefinitionSO attackDefinition,
            out EnemyAttackPresentationPhase attackPhase,
            out float attackProgress)
        {
            if (stateMachine != null && stateMachine.CurrentState is EnemyAttackState attackState)
            {
                attackDefinition = attackState.CurrentAttackDefinition;
                attackPhase = attackState.PresentationPhase;
                attackProgress = attackState.PresentationProgress;
                return;
            }

            attackDefinition = null;
            attackPhase = EnemyAttackPresentationPhase.None;
            attackProgress = 0f;
        }

        private string ResolvePlayableStateName(EnemyCombatAnimationPlan plan)
        {
            if (HasAnimatorState(plan.StateName))
            {
                return plan.StateName;
            }

            if (!string.Equals(plan.FallbackStateName, plan.StateName, System.StringComparison.Ordinal)
                && HasAnimatorState(plan.FallbackStateName))
            {
                return plan.FallbackStateName;
            }

            return plan.StateName;
        }

        private bool HasAnimatorState(string stateName)
        {
            if (animator == null
                || string.IsNullOrEmpty(stateName)
                || baseLayerIndex < 0
                || baseLayerIndex >= animator.layerCount)
            {
                return false;
            }

            if (animator.HasState(baseLayerIndex, Animator.StringToHash(stateName)))
            {
                return true;
            }

            string layerName = animator.GetLayerName(baseLayerIndex);
            return !string.IsNullOrEmpty(layerName)
                && animator.HasState(baseLayerIndex, Animator.StringToHash(layerName + "." + stateName));
        }

        private void UpdateResponseReadParameters(EnemyCombatAnimationPlan plan, float deltaTime)
        {
            float responseRead = plan.ResponseReadNormalized;
            TrySetFloatParameter(ResponseReadHash, responseRead, deltaTime);
            TrySetFloatParameter(
                AntiAirReadHash,
                plan.TargetResponse == EnemyTargetResponseType.AntiAir ? responseRead : 0f,
                deltaTime);
            TrySetFloatParameter(
                ChaseRollReadHash,
                plan.TargetResponse == EnemyTargetResponseType.ChaseRoll ? responseRead : 0f,
                deltaTime);
            TrySetFloatParameter(
                GuardBreakReadHash,
                plan.TargetResponse == EnemyTargetResponseType.GuardBreak ? responseRead : 0f,
                deltaTime);
        }

        private void TrySetFloatParameter(int parameterHash, float value, float deltaTime)
        {
            if (animator == null || !HasFloatParameter(parameterHash))
            {
                return;
            }

            animator.SetFloat(parameterHash, value, locomotionDampSeconds, deltaTime);
        }

        private bool HasFloatParameter(int parameterHash)
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];

                if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateCombatPoseLayer(string stateName, float groundSpeedNormalized, float deltaTime)
        {
            if (animator == null)
            {
                return;
            }

            if (combatPoseLayerIndex < 0 || combatPoseLayerIndex >= animator.layerCount)
            {
                combatPoseLayerIndex = animator.GetLayerIndex(CombatPoseLayerName);
            }

            if (combatPoseLayerIndex < 0 || combatPoseLayerIndex >= animator.layerCount)
            {
                return;
            }

            float targetWeight = ResolveCombatPoseLayerTargetWeight(stateName, groundSpeedNormalized);
            float blendDuration = Mathf.Max(0.01f, crossFadeSeconds);
            float currentWeight = animator.GetLayerWeight(combatPoseLayerIndex);
            float nextWeight = Mathf.MoveTowards(currentWeight, targetWeight, deltaTime / blendDuration);
            animator.SetLayerWeight(combatPoseLayerIndex, nextWeight);
        }

        public static float ResolveCombatPoseLayerTargetWeight(string stateName, float groundSpeedNormalized)
        {
            if (!string.Equals(stateName, EnemyCombatAnimationPlanUtility.LocomotionStateName, System.StringComparison.Ordinal))
            {
                return 0f;
            }

            return Mathf.Clamp01(groundSpeedNormalized) > 0.05f ? 0f : 0.15f;
        }

        private float ResolveMoveSpeedNormalized(float deltaTime)
        {
            Vector3 planarDelta = transform.position - lastWorldPosition;
            planarDelta.y = 0f;
            lastWorldPosition = transform.position;

            float speed = planarDelta.magnitude / deltaTime;

            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                Vector3 agentVelocity = navMeshAgent.velocity;
                agentVelocity.y = 0f;
                Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
                desiredVelocity.y = 0f;
                speed = Mathf.Max(speed, agentVelocity.magnitude, desiredVelocity.magnitude);
            }

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
