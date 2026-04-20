using UnityEngine;

namespace CampusRPG.Character
{
    public static class PlayerMovementRuntimeUtility
    {
        public static Vector3 BuildCameraRelativeMoveDirection(Vector2 input, Transform cameraTransform)
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
            return moveDirection.sqrMagnitude <= Mathf.Epsilon
                ? Vector3.zero
                : moveDirection.normalized;
        }

        public static float ResolveLockOnMoveSpeedScale(
            Transform actor,
            Vector3 moveDirection,
            float strafeSpeedScale,
            float backwardSpeedScale)
        {
            if (actor == null || moveDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return 1f;
            }

            Vector3 localDirection = actor.InverseTransformDirection(moveDirection.normalized);
            float sidewaysInfluence = Mathf.Clamp01(Mathf.Abs(localDirection.x));
            float backwardInfluence = Mathf.Clamp01(-localDirection.z);

            float sidewaysScale = Mathf.Lerp(1f, Mathf.Clamp(strafeSpeedScale, 0.1f, 1f), sidewaysInfluence);
            float backwardScale = Mathf.Lerp(1f, Mathf.Clamp(backwardSpeedScale, 0.1f, 1f), backwardInfluence);
            return Mathf.Min(sidewaysScale, backwardScale);
        }

        public static Vector2 ResolveAnimationMoveAxes(
            Transform actor,
            Vector2 moveInput,
            Vector3 moveDirection,
            bool isLockOnActive)
        {
            float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

            if (inputMagnitude <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            if (!isLockOnActive || actor == null || moveDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return new Vector2(0f, inputMagnitude);
            }

            Vector3 localDirection = actor.InverseTransformDirection(moveDirection.normalized);
            return Vector2.ClampMagnitude(new Vector2(localDirection.x, localDirection.z) * inputMagnitude, 1f);
        }

        public static bool TryResolveDodgeDirection(
            Transform actor,
            Vector2 moveInput,
            Transform cameraTransform,
            Transform lockOnTarget,
            out Vector3 dodgeDirection,
            out bool shouldFaceLockOnTarget)
        {
            shouldFaceLockOnTarget = lockOnTarget != null && actor != null;
            dodgeDirection = BuildCameraRelativeMoveDirection(moveInput, cameraTransform);

            if (dodgeDirection.sqrMagnitude > Mathf.Epsilon)
            {
                return true;
            }

            if (actor == null)
            {
                dodgeDirection = Vector3.forward;
                shouldFaceLockOnTarget = false;
                return true;
            }

            Vector3 fallbackDirection = shouldFaceLockOnTarget ? -actor.forward : actor.forward;
            fallbackDirection.y = 0f;

            if (fallbackDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                dodgeDirection = Vector3.forward;
                shouldFaceLockOnTarget = false;
                return true;
            }

            dodgeDirection = fallbackDirection.normalized;
            return true;
        }

        public static float ResolveDodgeDistanceMultiplier(
            Transform actor,
            Vector3 dodgeDirection,
            float backwardDistanceScale)
        {
            if (actor == null || dodgeDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return 1f;
            }

            Vector3 localDirection = actor.InverseTransformDirection(dodgeDirection.normalized);
            return localDirection.z < -0.1f
                ? Mathf.Clamp(backwardDistanceScale, 0.1f, 1f)
                : 1f;
        }
    }
}
