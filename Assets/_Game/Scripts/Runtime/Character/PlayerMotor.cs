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

        private CharacterController characterController;
        private float verticalVelocity;

        public bool IsGrounded => characterController != null && characterController.isGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public void ApplyMovementStats(float newMoveSpeed, float newRotationSpeed, float newJumpHeight)
        {
            moveSpeed = Mathf.Max(0f, newMoveSpeed);
            rotationSpeed = Mathf.Max(0f, newRotationSpeed);
            jumpHeight = Mathf.Max(0f, newJumpHeight);
        }

        public void ResetMotion()
        {
            verticalVelocity = 0f;
        }

        public void WarpTo(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (characterController == null)
            {
                transform.SetPositionAndRotation(worldPosition, worldRotation);
                verticalVelocity = 0f;
                return;
            }

            bool wasEnabled = characterController.enabled;

            if (wasEnabled)
            {
                characterController.enabled = false;
            }

            transform.SetPositionAndRotation(worldPosition, worldRotation);
            verticalVelocity = 0f;

            if (wasEnabled)
            {
                characterController.enabled = true;
            }
        }

        public void Tick(Vector2 moveInput, bool jumpPressed, Transform cameraTransform)
        {
            if (characterController == null)
            {
                return;
            }

            Vector3 moveDirection = BuildMoveDirection(moveInput, cameraTransform);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (jumpPressed && characterController.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }

        private static Vector3 BuildMoveDirection(Vector2 input, Transform cameraTransform)
        {
            if (input.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            if (cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y).normalized;
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = forward * input.y + right * input.x;
            return moveDirection.normalized;
        }
    }
}
