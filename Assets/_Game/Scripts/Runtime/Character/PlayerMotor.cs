using CampusRPG.Camera;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.Character
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float groundAcceleration = 24f;
        [SerializeField] private float groundDeceleration = 20f;
        [SerializeField] private float lockOnStrafeSpeedScale = 0.92f;
        [SerializeField] private float lockOnBackwardSpeedScale = 0.82f;
        [SerializeField] private LockOnTargetSelector lockOnTargetSelector;

        private CharacterController characterController;
        private Vector3 planarVelocity;
        private Vector2 animationMoveAxes;
        private float verticalVelocity;
        private float dodgeRemainingSeconds;
        private float dodgePlanarSpeed;
        private Vector3 dodgeDirection;
        private bool faceLockTargetDuringDodge;
        private float mantleRemainingSeconds;
        private float mantleDurationSeconds;
        private Vector3 mantleStartPosition;
        private Vector3 mantleTargetPosition;
        private Quaternion mantleStartRotation;
        private bool mantleControllerWasEnabled;

        public bool IsGrounded => characterController != null && characterController.isGrounded;

        public float VerticalVelocity => verticalVelocity;

        public Vector3 PlanarVelocity => planarVelocity;

        public Vector2 AnimationMoveAxes => animationMoveAxes;

        public float NormalizedGroundSpeed => moveSpeed > Mathf.Epsilon
            ? Mathf.Clamp01(planarVelocity.magnitude / moveSpeed)
            : 0f;

        public bool IsMantling => mantleRemainingSeconds > 0f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ResolveLockOnTargetSelector();
        }

        public void ApplyMovementStats(float newMoveSpeed, float newRotationSpeed, float newJumpHeight)
        {
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
            rotationSpeed = Mathf.Max(0f, newRotationSpeed);
            jumpHeight = Mathf.Max(0f, newJumpHeight);
        }

        public void ApplyMovementTuning(
            float newGroundAcceleration,
            float newGroundDeceleration,
            float newLockOnStrafeSpeedScale,
            float newLockOnBackwardSpeedScale)
        {
            groundAcceleration = Mathf.Max(0f, newGroundAcceleration);
            groundDeceleration = Mathf.Max(0f, newGroundDeceleration);
            lockOnStrafeSpeedScale = Mathf.Clamp(newLockOnStrafeSpeedScale, 0.1f, 1f);
            lockOnBackwardSpeedScale = Mathf.Clamp(newLockOnBackwardSpeedScale, 0.1f, 1f);
        }

        public void ResetMotion()
        {
            planarVelocity = Vector3.zero;
            animationMoveAxes = Vector2.zero;
            verticalVelocity = 0f;
            dodgeRemainingSeconds = 0f;
            dodgePlanarSpeed = 0f;
            dodgeDirection = Vector3.zero;
            faceLockTargetDuringDodge = false;
            FinishMantleInternal();
        }

        public void WarpTo(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (characterController == null)
            {
                transform.SetPositionAndRotation(worldPosition, worldRotation);
                planarVelocity = Vector3.zero;
                animationMoveAxes = Vector2.zero;
                verticalVelocity = 0f;
                return;
            }

            bool wasEnabled = characterController.enabled;

            if (wasEnabled)
            {
                characterController.enabled = false;
            }

            transform.SetPositionAndRotation(worldPosition, worldRotation);
            planarVelocity = Vector3.zero;
            animationMoveAxes = Vector2.zero;
            verticalVelocity = 0f;

            if (wasEnabled)
            {
                characterController.enabled = true;
            }
        }

        public void AdvanceFacingDirection(float distance)
        {
            if (distance <= 0f)
            {
                return;
            }

            Vector3 displacement = transform.forward;
            displacement.y = 0f;

            if (displacement.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            displacement = displacement.normalized * distance;

            if (characterController == null || !characterController.enabled)
            {
                transform.position += displacement;
                return;
            }

            characterController.Move(displacement);
        }

        public bool BeginDirectionalDodge(Vector3 direction, float distance, float durationSeconds, bool keepFacingLockOnTarget)
        {
            Vector3 planarDirection = direction;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                planarDirection = transform.forward;
                planarDirection.y = 0f;
            }

            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            float resolvedDuration = Mathf.Max(0.01f, durationSeconds);
            dodgeDirection = planarDirection.normalized;
            dodgePlanarSpeed = Mathf.Max(0f, distance) / resolvedDuration;
            dodgeRemainingSeconds = resolvedDuration;
            faceLockTargetDuringDodge = keepFacingLockOnTarget;
            planarVelocity = Vector3.zero;
            return true;
        }

        public bool BeginMantle(Vector3 targetPosition, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                return false;
            }

            mantleStartPosition = transform.position;
            mantleTargetPosition = targetPosition;
            mantleStartRotation = transform.rotation;
            mantleDurationSeconds = durationSeconds;
            mantleRemainingSeconds = durationSeconds;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            animationMoveAxes = Vector2.zero;

            mantleControllerWasEnabled = characterController != null && characterController.enabled;

            if (characterController != null && mantleControllerWasEnabled)
            {
                characterController.enabled = false;
            }

            return true;
        }

        public void Tick(Vector2 moveInput, bool jumpPressed, Transform cameraTransform, bool movementAllowed = true)
        {
            if (characterController == null)
            {
                return;
            }

            ResolveLockOnTargetSelector();

            if (UpdateMantle(Time.deltaTime))
            {
                return;
            }

            Transform lockOnTarget = lockOnTargetSelector != null ? lockOnTargetSelector.CurrentTarget : null;
            bool isLockOnActive = lockOnTarget != null;

            if (UpdateDodge(Time.deltaTime, lockOnTarget))
            {
                animationMoveAxes = Vector2.zero;
                return;
            }

            Vector3 moveDirection = PlayerMovementRuntimeUtility.BuildCameraRelativeMoveDirection(moveInput, cameraTransform);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (jumpPressed && movementAllowed && characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            if (!movementAllowed)
            {
                planarVelocity = Vector3.zero;
                animationMoveAxes = Vector2.zero;
            }
            else
            {
                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = ResolveTargetRotation(moveDirection, lockOnTarget, isLockOnActive);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime);
                }
                else if (isLockOnActive)
                {
                    Quaternion targetRotation = ResolveTargetRotation(Vector3.zero, lockOnTarget, true);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime);
                }

                float speedScale = isLockOnActive
                    ? PlayerMovementRuntimeUtility.ResolveLockOnMoveSpeedScale(
                        transform,
                        moveDirection,
                        lockOnStrafeSpeedScale,
                        lockOnBackwardSpeedScale)
                    : 1f;
                Vector3 desiredPlanarVelocity = moveDirection * moveSpeed * speedScale;
                float acceleration = desiredPlanarVelocity.sqrMagnitude > Mathf.Epsilon ? groundAcceleration : groundDeceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, desiredPlanarVelocity, acceleration * Time.deltaTime);
                animationMoveAxes = PlayerMovementRuntimeUtility.ResolveAnimationMoveAxes(transform, moveInput, moveDirection, isLockOnActive);
            }

            Vector3 velocity = planarVelocity;
            velocity.y = verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }

        private bool UpdateDodge(float deltaTime, Transform lockOnTarget)
        {
            if (dodgeRemainingSeconds <= 0f)
            {
                return false;
            }

            dodgeRemainingSeconds = Mathf.Max(0f, dodgeRemainingSeconds - deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * deltaTime;
            planarVelocity = dodgeDirection * dodgePlanarSpeed;

            if (faceLockTargetDuringDodge && lockOnTarget != null)
            {
                Quaternion targetRotation = ResolveTargetRotation(Vector3.zero, lockOnTarget, true);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
            else if (dodgeDirection.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dodgeDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }

            Vector3 velocity = planarVelocity;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * deltaTime);

            if (dodgeRemainingSeconds <= 0f)
            {
                planarVelocity = Vector3.zero;
            }

            return true;
        }

        private bool UpdateMantle(float deltaTime)
        {
            if (mantleRemainingSeconds <= 0f)
            {
                return false;
            }

            mantleRemainingSeconds = Mathf.Max(0f, mantleRemainingSeconds - deltaTime);
            float elapsed = mantleDurationSeconds - mantleRemainingSeconds;
            float normalizedTime = mantleDurationSeconds > 0f
                ? Mathf.Clamp01(elapsed / mantleDurationSeconds)
                : 1f;
            Vector3 position = Vector3.Lerp(mantleStartPosition, mantleTargetPosition, normalizedTime);
            float arcHeight = Mathf.Clamp(mantleTargetPosition.y - mantleStartPosition.y, 0.12f, 0.35f);
            position.y += Mathf.Sin(normalizedTime * Mathf.PI) * arcHeight;
            transform.SetPositionAndRotation(position, mantleStartRotation);

            if (mantleRemainingSeconds <= 0f)
            {
                transform.SetPositionAndRotation(mantleTargetPosition, mantleStartRotation);
                FinishMantleInternal();
            }

            return true;
        }

        private void FinishMantleInternal()
        {
            mantleRemainingSeconds = 0f;
            mantleDurationSeconds = 0f;

            if (characterController != null && mantleControllerWasEnabled && !characterController.enabled)
            {
                characterController.enabled = true;
            }

            mantleControllerWasEnabled = false;
        }

        private Quaternion ResolveTargetRotation(Vector3 moveDirection, Transform lockOnTarget, bool isLockOnActive)
        {
            Vector3 facingDirection = moveDirection;

            if (isLockOnActive && lockOnTarget != null)
            {
                facingDirection = lockOnTarget.position - transform.position;
            }

            facingDirection.y = 0f;

            if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
        }

        private void ResolveLockOnTargetSelector()
        {
            lockOnTargetSelector = SceneRuntimeReferenceUtility.ResolveLockOnTargetSelector(lockOnTargetSelector, GetComponent<PlayerCharacter>());
        }
    }
}
