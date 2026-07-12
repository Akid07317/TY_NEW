using System.Collections.Generic;
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
        private string currentAnimatorBaseStateName = string.Empty;
        private int lastObservedStateRevision = -1;
        private int combatPoseLayerIndex = -1;
        private Animator preparedAnimator;
        private Transform importedVisualRoot;
        private bool hasImportedVisualRootAnchor;
        private Vector3 importedVisualRootAnchorLocalPosition;
        private Quaternion importedVisualRootAnchorLocalRotation = Quaternion.identity;
        private Vector3 importedVisualRootAnchorLocalScale = Vector3.one;
        private Transform importedAnimatorTransform;
        private bool hasImportedAnimatorTransformAnchor;
        private Vector3 importedAnimatorAnchorLocalPosition;
        private Quaternion importedAnimatorAnchorLocalRotation = Quaternion.identity;
        private Vector3 importedAnimatorAnchorLocalScale = Vector3.one;
        private readonly Dictionary<string, int> attackVariantCursorByStateName = new Dictionary<string, int>();

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
            currentAnimatorBaseStateName = string.Empty;
            lastObservedStateRevision = -1;
            combatPoseLayerIndex = -1;
            ClearImportedPreviewAnchors();
            attackVariantCursorByStateName.Clear();
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
            string animatorBaseStateName = ResolvePlayableBaseStateName(plan);
            int stateRevision = stateMachine != null ? stateMachine.StateRevision : -1;
            bool shouldRestartCurrentState = stateRevision != lastObservedStateRevision
                && ShouldRestartClipOnStateReenter(animatorBaseStateName);
            string animatorStateName = ResolveAnimatorStateNameForFrame(animatorBaseStateName, shouldRestartCurrentState);

            animator.SetFloat(GroundSpeedHash, plan.GroundSpeedNormalized, locomotionDampSeconds, Time.deltaTime);
            UpdateResponseReadParameters(plan, deltaTime);
            UpdateCombatPoseLayer(
                animatorBaseStateName,
                plan.GroundSpeedNormalized,
                plan.ResponseReadNormalized,
                deltaTime);

            if (!shouldRestartCurrentState
                && string.Equals(currentAnimatorBaseStateName, animatorBaseStateName, System.StringComparison.Ordinal)
                && string.Equals(currentAnimatorStateName, animatorStateName, System.StringComparison.Ordinal))
            {
                lastObservedStateRevision = stateRevision;
                return;
            }

            lastObservedStateRevision = stateRevision;
            currentAnimatorBaseStateName = animatorBaseStateName;
            currentAnimatorStateName = animatorStateName;
            float transitionSeconds = shouldRestartCurrentState
                ? Mathf.Min(Mathf.Max(0f, crossFadeSeconds), 0.04f)
                : Mathf.Max(0f, crossFadeSeconds);
            animator.CrossFadeInFixedTime(currentAnimatorStateName, transitionSeconds, baseLayerIndex, 0f, 0f);
        }

        private void LateUpdate()
        {
            StabilizeImportedPreviewTransforms();
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
                currentAnimatorBaseStateName = string.Empty;
                combatPoseLayerIndex = -1;
            }

            PrepareImportedPreviewAnchors(importedAnimator);

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
            Transform visualRoot = transform.Find(ImportedVisualRootName);
            return visualRoot != null
                ? visualRoot.GetComponentInChildren<Animator>(true)
                : null;
        }

        private void PrepareImportedPreviewAnchors(Animator importedAnimator)
        {
            Transform visualRoot = transform.Find(ImportedVisualRootName);

            if (visualRoot != importedVisualRoot)
            {
                importedVisualRoot = visualRoot;
                hasImportedVisualRootAnchor = false;
            }

            if (importedVisualRoot != null && !hasImportedVisualRootAnchor)
            {
                importedVisualRootAnchorLocalPosition = importedVisualRoot.localPosition;
                importedVisualRootAnchorLocalRotation = importedVisualRoot.localRotation;
                importedVisualRootAnchorLocalScale = importedVisualRoot.localScale;
                hasImportedVisualRootAnchor = true;
            }

            Transform animatorTransform = importedAnimator != null
                                           && importedVisualRoot != null
                                           && importedAnimator.transform != importedVisualRoot
                                           && importedAnimator.transform.IsChildOf(importedVisualRoot)
                ? importedAnimator.transform
                : null;

            if (animatorTransform != importedAnimatorTransform)
            {
                importedAnimatorTransform = animatorTransform;
                hasImportedAnimatorTransformAnchor = false;
            }

            if (importedAnimatorTransform != null && !hasImportedAnimatorTransformAnchor)
            {
                importedAnimatorAnchorLocalPosition = importedAnimatorTransform.localPosition;
                importedAnimatorAnchorLocalRotation = importedAnimatorTransform.localRotation;
                importedAnimatorAnchorLocalScale = importedAnimatorTransform.localScale;
                hasImportedAnimatorTransformAnchor = true;
            }
        }

        private void StabilizeImportedPreviewTransforms()
        {
            if (importedVisualRoot == null)
            {
                PrepareImportedPreviewAnchors(FindImportedPreviewAnimator());
            }

            if (importedVisualRoot != null && hasImportedVisualRootAnchor)
            {
                importedVisualRoot.localPosition = importedVisualRootAnchorLocalPosition;
                importedVisualRoot.localRotation = importedVisualRootAnchorLocalRotation;
                importedVisualRoot.localScale = importedVisualRootAnchorLocalScale;
            }

            if (importedAnimatorTransform != null && hasImportedAnimatorTransformAnchor)
            {
                importedAnimatorTransform.localPosition = importedAnimatorAnchorLocalPosition;
                importedAnimatorTransform.localRotation = importedAnimatorAnchorLocalRotation;
                importedAnimatorTransform.localScale = importedAnimatorAnchorLocalScale;
            }
        }

        private void ClearImportedPreviewAnchors()
        {
            importedVisualRoot = null;
            hasImportedVisualRootAnchor = false;
            importedAnimatorTransform = null;
            hasImportedAnimatorTransformAnchor = false;
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
            currentAnimatorBaseStateName = string.Empty;
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

        private string ResolvePlayableBaseStateName(EnemyCombatAnimationPlan plan)
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

        private string ResolveAnimatorStateNameForFrame(string baseStateName, bool shouldRestartCurrentState)
        {
            if (string.IsNullOrEmpty(baseStateName))
            {
                return baseStateName;
            }

            if (!shouldRestartCurrentState
                && string.Equals(currentAnimatorBaseStateName, baseStateName, System.StringComparison.Ordinal)
                && !string.IsNullOrEmpty(currentAnimatorStateName))
            {
                return currentAnimatorStateName;
            }

            return ResolveNextAttackVariantStateName(baseStateName);
        }

        private string ResolveNextAttackVariantStateName(string baseStateName)
        {
            if (!IsAttackStateName(baseStateName))
            {
                return baseStateName;
            }

            int variantCount = CountContiguousAttackVariantStates(baseStateName);

            if (variantCount <= 0)
            {
                return baseStateName;
            }

            attackVariantCursorByStateName.TryGetValue(baseStateName, out int cursor);
            int variantIndex = (cursor % variantCount) + 1;
            attackVariantCursorByStateName[baseStateName] = (cursor + 1) % variantCount;
            string variantStateName = FormatAttackVariantStateName(baseStateName, variantIndex);
            return HasAnimatorState(variantStateName) ? variantStateName : baseStateName;
        }

        public static string FormatAttackVariantStateName(string baseStateName, int variantIndex)
        {
            int clampedIndex = Mathf.Max(1, variantIndex);
            return clampedIndex < 10
                ? baseStateName + "_0" + clampedIndex
                : baseStateName + "_" + clampedIndex;
        }

        private int CountContiguousAttackVariantStates(string baseStateName)
        {
            const int MaxVariantProbeCount = 96;

            for (int i = 1; i <= MaxVariantProbeCount; i++)
            {
                if (!HasAnimatorState(FormatAttackVariantStateName(baseStateName, i)))
                {
                    return i - 1;
                }
            }

            return MaxVariantProbeCount;
        }

        private static bool IsAttackStateName(string stateName)
        {
            return !string.IsNullOrEmpty(stateName)
                && stateName.StartsWith("Attack_", System.StringComparison.Ordinal);
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

        private void UpdateCombatPoseLayer(
            string stateName,
            float groundSpeedNormalized,
            float responseReadNormalized,
            float deltaTime)
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

            float targetWeight = ResolveCombatPoseLayerTargetWeight(
                stateName,
                groundSpeedNormalized,
                responseReadNormalized);
            float blendDuration = Mathf.Max(0.01f, crossFadeSeconds);
            float currentWeight = animator.GetLayerWeight(combatPoseLayerIndex);
            float nextWeight = Mathf.MoveTowards(currentWeight, targetWeight, deltaTime / blendDuration);
            animator.SetLayerWeight(combatPoseLayerIndex, nextWeight);
        }

        public static float ResolveCombatPoseLayerTargetWeight(
            string stateName,
            float groundSpeedNormalized,
            float responseReadNormalized)
        {
            if (Mathf.Clamp01(responseReadNormalized) > 0.001f)
            {
                return Mathf.Lerp(0.35f, 0.95f, Mathf.Clamp01(responseReadNormalized));
            }

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
