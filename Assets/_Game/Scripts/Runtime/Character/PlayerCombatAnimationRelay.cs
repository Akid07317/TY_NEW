using System.Collections.Generic;
using CampusRPG.Camera;
using CampusRPG.Combat;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.Character
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatAnimationRelay : MonoBehaviour
    {
        public const string LocomotionStateName = "Locomotion";
        public const string AirborneStateName = "Airborne";
        public const string HitStateName = "Hit";
        public const string GuardBreakHitStateName = "GuardBreak";
        public const string GroundDodgeStateName = "Dodge";
        public const string CombatRollStateName = "CombatRoll";
        public const string AirDodgeStateName = "AirDodge";

        private const string BlockStateName = "Block";
        private const string DeathStateName = "Death";

        private static readonly int GroundSpeedHash = Animator.StringToHash("GroundSpeed");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private PlayerStateMachine stateMachine;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private ThirdPersonCameraController cameraController;
        [SerializeField] private Transform proxyWeaponGrip;
        [SerializeField] private Transform importedWeaponAnchor;
        [SerializeField] private int baseLayerIndex;
        [SerializeField] private float crossFadeSeconds = 0.035f;
        [SerializeField] private float locomotionDampSeconds = 0.05f;
        [SerializeField] private float dodgeAnimationDurationSeconds = 0.4f;
        [SerializeField] private float combatRollAnimationDurationSeconds = 0.52f;
        [SerializeField] private float airDodgeAnimationDurationSeconds = 0.34f;
        [SerializeField] private float hitAnimationDurationSeconds = 0.26f;
        [SerializeField] private float proxyWeaponFollowSmoothing = 24f;
        [SerializeField] private float immediateProxyWeaponFollowSeconds = 0.12f;
        [SerializeField] private float snapProxyWeaponFollowAngleDegrees = 12f;
        [SerializeField] private float snapProxyWeaponFollowDistance = 0.025f;
        [SerializeField] private float guardBreakCameraImpulseDistance = 0.18f;
        [SerializeField] private float guardBreakCameraImpulseSeconds = 0.16f;

        private static readonly string[] ImportedWeaponAnchorCandidateNames =
        {
            "RightHand",
            "Hand_R",
            "R_Hand",
            "mixamorig:RightHand",
            "Bip001 R Hand"
        };

        private static readonly Vector3 ImportedWeaponAnchorLocalPosition = new Vector3(0.02f, -0.02f, 0.02f);
        private static readonly Quaternion ImportedWeaponAnchorLocalRotation = Quaternion.Euler(8f, 18f, 92f);
        private Vector3 defaultProxyWeaponGripLocalScale = Vector3.one;
        private bool proxyWeaponDefaultsCaptured;
        private bool proxyWeaponSnappedToAnchor;
        private float immediateProxyWeaponFollowTimer;
        private readonly Dictionary<string, int> attackVariantCursorByStateName = new Dictionary<string, int>();

        public float DodgeAnimationDurationSeconds => Mathf.Max(0.01f, dodgeAnimationDurationSeconds);

        public float CombatRollAnimationDurationSeconds => Mathf.Max(0.01f, combatRollAnimationDurationSeconds);

        public float AirDodgeAnimationDurationSeconds => Mathf.Max(0.01f, airDodgeAnimationDurationSeconds);

        public float HitAnimationDurationSeconds => Mathf.Max(0.01f, hitAnimationDurationSeconds);

        public PlayerHitReactionType CurrentHitReactionType { get; private set; } = PlayerHitReactionType.Standard;

        private void OnEnable()
        {
            attackVariantCursorByStateName.Clear();
        }

        private void Awake()
        {
            if (playerCharacter == null)
            {
                playerCharacter = GetComponent<PlayerCharacter>();
            }

            if (combatController == null)
            {
                combatController = GetComponent<PlayerCombatController>();
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            ResolveCameraController();
            EnsureProxyWeaponReferences();
            CaptureProxyWeaponDefaults();
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            float targetGroundSpeed = motor != null
                ? motor.NormalizedGroundSpeed
                : playerCharacter != null && playerCharacter.InputReader != null && (stateMachine == null || stateMachine.AllowsMovement)
                    ? Mathf.Clamp01(playerCharacter.InputReader.MoveValue.magnitude)
                    : 0f;
            Vector2 moveAxes = motor != null ? motor.AnimationMoveAxes : Vector2.zero;

            if (stateMachine != null && !stateMachine.AllowsMovement)
            {
                moveAxes = Vector2.zero;
                targetGroundSpeed = 0f;
            }

            animator.SetFloat(GroundSpeedHash, targetGroundSpeed, locomotionDampSeconds, Time.deltaTime);
            animator.SetFloat(MoveXHash, moveAxes.x, locomotionDampSeconds, Time.deltaTime);
            animator.SetFloat(MoveYHash, moveAxes.y, locomotionDampSeconds, Time.deltaTime);
            animator.SetBool(IsGroundedHash, motor == null || motor.IsGrounded);
            animator.SetBool(IsBlockingHash, stateMachine != null && stateMachine.IsBlocking);
            animator.SetFloat(VerticalSpeedHash, motor != null ? motor.VerticalVelocity : 0f);
        }

        private void LateUpdate()
        {
            if (!EnsureProxyWeaponReferences())
            {
                return;
            }

            if (!proxyWeaponDefaultsCaptured)
            {
                CaptureProxyWeaponDefaults();
            }

            immediateProxyWeaponFollowTimer = PlayerCombatRuntimeUtility.TickWindow(immediateProxyWeaponFollowTimer, Time.deltaTime);
            float followT = 1f - Mathf.Exp(-Mathf.Max(0f, proxyWeaponFollowSmoothing) * Mathf.Max(Time.deltaTime, 0.0001f));
            Vector3 targetPosition = importedWeaponAnchor.TransformPoint(ImportedWeaponAnchorLocalPosition);
            Quaternion targetRotation = importedWeaponAnchor.rotation * ImportedWeaponAnchorLocalRotation;
            bool shouldSnapToAnchor = PlayerCombatRuntimeUtility.ShouldSnapProxyWeaponFollow(
                proxyWeaponSnappedToAnchor,
                RequiresImmediateProxyWeaponFollow(stateMachine != null ? stateMachine.CurrentState : null),
                immediateProxyWeaponFollowTimer,
                proxyWeaponGrip != null ? Quaternion.Angle(proxyWeaponGrip.rotation, targetRotation) : 0f,
                proxyWeaponGrip != null ? Vector3.Distance(proxyWeaponGrip.position, targetPosition) : 0f,
                snapProxyWeaponFollowAngleDegrees,
                snapProxyWeaponFollowDistance);

            if (shouldSnapToAnchor)
            {
                proxyWeaponGrip.position = targetPosition;
                proxyWeaponGrip.rotation = targetRotation;
                proxyWeaponSnappedToAnchor = true;
            }
            else
            {
                proxyWeaponGrip.position = Vector3.Lerp(proxyWeaponGrip.position, targetPosition, followT);
                proxyWeaponGrip.rotation = Quaternion.Slerp(proxyWeaponGrip.rotation, targetRotation, followT);
            }

            proxyWeaponGrip.localScale = defaultProxyWeaponGripLocalScale;
        }

        public void PlayAttack(AttackDefinitionSO attackDefinition)
        {
            if (attackDefinition == null)
            {
                return;
            }

            TriggerAttackCameraFeedback(attackDefinition);
            TriggerAttackAudioFeedback(attackDefinition);

            if (animator == null || string.IsNullOrWhiteSpace(attackDefinition.AnimationStateName))
            {
                return;
            }

            string stateName = ResolveNextAttackVariantStateName(attackDefinition.AnimationStateName);
            animator.CrossFadeInFixedTime(stateName, Mathf.Max(0f, crossFadeSeconds), baseLayerIndex);
        }

        public void AnimationEvent_OpenAttackHitbox()
        {
            combatController?.ActivatePreparedHitboxFromAnimationEvent();
        }

        public void AnimationEvent_CloseAttackHitbox()
        {
            combatController?.ClearPreparedHitboxFromAnimationEvent();
        }

        public void NotifyStateChanged(PlayerState previousState, PlayerState currentState)
        {
            if (currentState == null)
            {
                return;
            }

            CurrentHitReactionType = currentState is PlayerHitState hitState
                ? hitState.ReactionType
                : PlayerHitReactionType.Standard;

            if (currentState is PlayerHitState)
            {
                TriggerHitReactionFeedback(CurrentHitReactionType);
            }

            if (currentState is PlayerDodgeState dodgeStateForFeedback)
            {
                TriggerEvasiveActionFeedback(dodgeStateForFeedback.ActionType);
            }

            if (animator == null)
            {
                return;
            }

            if (RequiresImmediateProxyWeaponFollow(previousState) || RequiresImmediateProxyWeaponFollow(currentState))
            {
                immediateProxyWeaponFollowTimer = PlayerCombatRuntimeUtility.OpenWindow(
                    immediateProxyWeaponFollowTimer,
                    immediateProxyWeaponFollowSeconds);
            }

            if (currentState is PlayerDodgeState dodgeState)
            {
                CrossFadeState(ResolveEvasiveActionStateName(dodgeState.ActionType));
                return;
            }

            if (currentState is PlayerHitState)
            {
                CrossFadeState(ResolveHitReactionStateName(CurrentHitReactionType));
                return;
            }

            if (currentState is PlayerMantleState)
            {
                CrossFadeState(AirborneStateName);
                return;
            }

            if (currentState is PlayerDeathState)
            {
                CrossFadeState(DeathStateName);
                return;
            }

            bool isRecoveringFromAction = previousState is PlayerAttackState
                || previousState is PlayerDodgeState
                || previousState is PlayerMantleState
                || previousState is PlayerHitState
                || previousState is PlayerSkillState;

            if (currentState is PlayerBlockState && isRecoveringFromAction)
            {
                CrossFadeState(BlockStateName);
                return;
            }

            if (currentState is PlayerLocomotionState && isRecoveringFromAction)
            {
                CrossFadeState(ResolveActionRecoveryStateName(motor != null, motor == null || motor.IsGrounded));
            }
        }

        public static string ResolveActionRecoveryStateName(bool hasGroundingSource, bool isGrounded)
        {
            return hasGroundingSource && !isGrounded ? AirborneStateName : LocomotionStateName;
        }

        public static string ResolveHitReactionStateName(PlayerHitReactionType reactionType)
        {
            return reactionType == PlayerHitReactionType.GuardBreak ? GuardBreakHitStateName : HitStateName;
        }

        public static string ResolveEvasiveActionStateName(PlayerEvasiveActionType actionType)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => CombatRollStateName,
                PlayerEvasiveActionType.AirDodge => AirDodgeStateName,
                _ => GroundDodgeStateName
            };
        }

        public static string FormatAttackVariantStateName(string baseStateName, int variantIndex)
        {
            int clampedIndex = Mathf.Max(1, variantIndex);
            return clampedIndex < 10
                ? baseStateName + "_0" + clampedIndex
                : baseStateName + "_" + clampedIndex;
        }

        private string ResolveNextAttackVariantStateName(string baseStateName)
        {
            if (string.IsNullOrWhiteSpace(baseStateName))
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

        private int CountContiguousAttackVariantStates(string baseStateName)
        {
            const int MaxVariantProbeCount = 32;

            for (int i = 1; i <= MaxVariantProbeCount; i++)
            {
                if (!HasAnimatorState(FormatAttackVariantStateName(baseStateName, i)))
                {
                    return i - 1;
                }
            }

            return MaxVariantProbeCount;
        }

        private bool HasAnimatorState(string stateName)
        {
            if (animator == null
                || string.IsNullOrWhiteSpace(stateName)
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

        public float ResolveEvasiveAnimationDurationSeconds(PlayerEvasiveActionType actionType)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => CombatRollAnimationDurationSeconds,
                PlayerEvasiveActionType.AirDodge => AirDodgeAnimationDurationSeconds,
                _ => DodgeAnimationDurationSeconds
            };
        }

        private void TriggerHitReactionFeedback(PlayerHitReactionType reactionType)
        {
            if (reactionType != PlayerHitReactionType.GuardBreak)
            {
                return;
            }

            TriggerActionAudioFeedback(ProceduralAudioUtility.ResolveHitReactionCue(reactionType));
            ResolveCameraController();
            ActionCameraFeedbackUtility.TryRequestImpulse(
                cameraController,
                transform,
                ActionCameraFeedbackUtility.ResolveGuardBreakImpulse(
                    guardBreakCameraImpulseDistance,
                    guardBreakCameraImpulseSeconds));
        }

        private void TriggerEvasiveActionFeedback(PlayerEvasiveActionType actionType)
        {
            TriggerActionAudioFeedback(ProceduralAudioUtility.ResolveEvasiveActionCue(actionType));
            ResolveCameraController();
            ActionCameraFeedbackUtility.TryRequestImpulse(
                cameraController,
                transform,
                ActionCameraFeedbackUtility.ResolveEvasiveImpulse(actionType));
        }

        private void TriggerAttackCameraFeedback(AttackDefinitionSO attackDefinition)
        {
            ResolveCameraController();
            ActionCameraFeedbackUtility.TryRequestImpulse(
                cameraController,
                transform,
                ActionCameraFeedbackUtility.ResolvePlayerAttackImpulse(attackDefinition));
        }

        private void TriggerAttackAudioFeedback(AttackDefinitionSO attackDefinition)
        {
            TriggerActionAudioFeedback(ProceduralAudioUtility.ResolvePlayerAttackCue(attackDefinition));
        }

        private void TriggerActionAudioFeedback(ProceduralActionAudioPlan plan)
        {
            ProceduralAudioUtility.TryPlayActionCue(transform.position, plan);
        }

        private void CrossFadeState(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            animator.CrossFadeInFixedTime(stateName, Mathf.Max(0f, crossFadeSeconds), baseLayerIndex);
        }

        private void ResolveCameraController()
        {
            cameraController = SceneRuntimeReferenceUtility.ResolveCameraController(cameraController);
        }

        private static bool RequiresImmediateProxyWeaponFollow(PlayerState state)
        {
            return state is PlayerAttackState
                || state is PlayerSkillState
                || state is PlayerDodgeState
                || state is PlayerHitState
                || state is PlayerMantleState
                || state is PlayerBlockState;
        }

        public static Transform FindDefaultProxyWeaponGrip(Transform actorRoot)
        {
            return actorRoot != null ? actorRoot.Find("CombatProxyVisualRoot/WeaponGrip") : null;
        }

        public static Transform FindDefaultImportedWeaponAnchor(Transform actorRoot)
        {
            Transform importedRoot = actorRoot != null ? actorRoot.Find("ImportedVisualRoot") : null;

            if (importedRoot == null)
            {
                return null;
            }

            for (int i = 0; i < ImportedWeaponAnchorCandidateNames.Length; i++)
            {
                Transform candidate = FindDeepChild(importedRoot, ImportedWeaponAnchorCandidateNames[i]);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool EnsureProxyWeaponReferences()
        {
            if (proxyWeaponGrip == null)
            {
                proxyWeaponGrip = FindDefaultProxyWeaponGrip(transform);
            }

            if (importedWeaponAnchor == null)
            {
                if (animator != null && animator.avatar != null && animator.isHuman)
                {
                    importedWeaponAnchor = animator.GetBoneTransform(HumanBodyBones.RightHand);
                }

                if (importedWeaponAnchor == null)
                {
                    importedWeaponAnchor = FindDefaultImportedWeaponAnchor(transform);
                }
            }

            return proxyWeaponGrip != null && importedWeaponAnchor != null;
        }

        private void CaptureProxyWeaponDefaults()
        {
            if (proxyWeaponGrip == null)
            {
                proxyWeaponDefaultsCaptured = false;
                proxyWeaponSnappedToAnchor = false;
                return;
            }

            defaultProxyWeaponGripLocalScale = proxyWeaponGrip.localScale;
            proxyWeaponDefaultsCaptured = true;
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeepChild(root.GetChild(i), targetName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
