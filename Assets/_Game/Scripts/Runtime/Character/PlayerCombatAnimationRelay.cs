using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatAnimationRelay : MonoBehaviour
    {
        private const string LocomotionStateName = "Locomotion";
        private const string BlockStateName = "Block";
        private const string AirborneStateName = "Airborne";
        private const string DodgeStateName = "Dodge";
        private const string HitStateName = "Hit";
        private const string DeathStateName = "Death";

        private static readonly int GroundSpeedHash = Animator.StringToHash("GroundSpeed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private PlayerStateMachine stateMachine;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private int baseLayerIndex;
        [SerializeField] private float crossFadeSeconds = 0.05f;
        [SerializeField] private float locomotionDampSeconds = 0.08f;
        [SerializeField] private float dodgeAnimationDurationSeconds = 0.4f;
        [SerializeField] private float hitAnimationDurationSeconds = 0.35f;

        public float DodgeAnimationDurationSeconds => Mathf.Max(0.01f, dodgeAnimationDurationSeconds);

        public float HitAnimationDurationSeconds => Mathf.Max(0.01f, hitAnimationDurationSeconds);

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
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            float targetGroundSpeed = 0f;

            if (playerCharacter != null && playerCharacter.InputReader != null && (stateMachine == null || stateMachine.AllowsMovement))
            {
                targetGroundSpeed = Mathf.Clamp01(playerCharacter.InputReader.MoveValue.magnitude);
            }

            animator.SetFloat(GroundSpeedHash, targetGroundSpeed, locomotionDampSeconds, Time.deltaTime);
            animator.SetBool(IsGroundedHash, motor == null || motor.IsGrounded);
            animator.SetBool(IsBlockingHash, stateMachine != null && stateMachine.IsBlocking);
            animator.SetFloat(VerticalSpeedHash, motor != null ? motor.VerticalVelocity : 0f);
        }

        public void PlayAttack(AttackDefinitionSO attackDefinition)
        {
            if (animator == null || attackDefinition == null || string.IsNullOrWhiteSpace(attackDefinition.AnimationStateName))
            {
                return;
            }

            animator.CrossFadeInFixedTime(attackDefinition.AnimationStateName, Mathf.Max(0f, crossFadeSeconds), baseLayerIndex);
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
            if (animator == null || currentState == null)
            {
                return;
            }

            if (currentState is PlayerDodgeState)
            {
                CrossFadeState(DodgeStateName);
                return;
            }

            if (currentState is PlayerHitState)
            {
                CrossFadeState(HitStateName);
                return;
            }

            if (currentState is PlayerDeathState)
            {
                CrossFadeState(DeathStateName);
                return;
            }

            bool isRecoveringFromAction = previousState is PlayerDodgeState
                || previousState is PlayerHitState
                || previousState is PlayerSkillState;

            if (currentState is PlayerBlockState && isRecoveringFromAction)
            {
                CrossFadeState(BlockStateName);
                return;
            }

            if (currentState is PlayerLocomotionState && isRecoveringFromAction)
            {
                CrossFadeState(motor != null && !motor.IsGrounded ? AirborneStateName : LocomotionStateName);
            }
        }

        private void CrossFadeState(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            animator.CrossFadeInFixedTime(stateName, Mathf.Max(0f, crossFadeSeconds), baseLayerIndex);
        }
    }
}
