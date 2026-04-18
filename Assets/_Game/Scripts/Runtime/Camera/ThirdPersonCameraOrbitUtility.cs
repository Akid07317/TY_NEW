using UnityEngine;

namespace CampusRPG.Camera
{
    public readonly struct ThirdPersonCameraOrbitAngles
    {
        public ThirdPersonCameraOrbitAngles(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }

        public float Yaw { get; }

        public float Pitch { get; }
    }

    public readonly struct ThirdPersonCameraFollowStep
    {
        public ThirdPersonCameraFollowStep(Vector3 position, float lerpFactor)
        {
            Position = position;
            LerpFactor = lerpFactor;
        }

        public Vector3 Position { get; }

        public float LerpFactor { get; }
    }

    public static class ThirdPersonCameraOrbitUtility
    {
        public static ThirdPersonCameraOrbitAngles ResolveInitialAngles(Quaternion rotation, float minPitch, float maxPitch)
        {
            Vector3 euler = rotation.eulerAngles;
            return new ThirdPersonCameraOrbitAngles(euler.y, ClampPitch(euler.x, minPitch, maxPitch));
        }

        public static ThirdPersonCameraOrbitAngles ResolveFreeLookAngles(
            float yaw,
            float pitch,
            Vector2 lookInput,
            float horizontalLookSensitivity,
            float verticalLookSensitivity,
            float minPitch,
            float maxPitch)
        {
            return new ThirdPersonCameraOrbitAngles(
                yaw + lookInput.x * horizontalLookSensitivity,
                ClampPitch(pitch - lookInput.y * verticalLookSensitivity, minPitch, maxPitch));
        }

        public static bool TryResolveLockOnAngles(
            Vector3 followPosition,
            Vector3 lockOnPosition,
            float followLookHeight,
            float lockOnLookHeight,
            float currentYaw,
            float currentPitch,
            float lockOnRotationSpeed,
            float deltaTime,
            float minPitch,
            float maxPitch,
            out ThirdPersonCameraOrbitAngles angles)
        {
            angles = default;

            Vector3 fromPoint = followPosition + Vector3.up * followLookHeight;
            Vector3 toPoint = lockOnPosition + Vector3.up * lockOnLookHeight;
            Vector3 direction = toPoint - fromPoint;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Quaternion currentRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Quaternion blendedRotation = Quaternion.RotateTowards(
                currentRotation,
                targetRotation,
                Mathf.Max(0f, lockOnRotationSpeed) * Mathf.Max(0f, deltaTime));
            Vector3 blendedEuler = blendedRotation.eulerAngles;
            angles = new ThirdPersonCameraOrbitAngles(
                blendedEuler.y,
                ClampPitch(blendedEuler.x, minPitch, maxPitch));
            return true;
        }

        public static Vector3 ResolveLookPoint(
            Vector3 followPosition,
            Vector3 lockOnPosition,
            bool isLockOnActive,
            bool hasLockOnTarget,
            float followLookHeight,
            float lockOnLookHeight)
        {
            Vector3 playerLookPoint = followPosition + Vector3.up * followLookHeight;

            if (!isLockOnActive || !hasLockOnTarget)
            {
                return playerLookPoint;
            }

            Vector3 targetLookPoint = lockOnPosition + Vector3.up * lockOnLookHeight;
            return (playerLookPoint + targetLookPoint) * 0.5f;
        }

        public static ThirdPersonCameraFollowStep ResolveFollowStep(
            Vector3 currentPosition,
            Vector3 focusPoint,
            Vector3 followOffset,
            float yaw,
            float pitch,
            float followSharpness,
            float deltaTime)
        {
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focusPoint + orbitRotation * followOffset;
            float lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0f, followSharpness) * Mathf.Max(0f, deltaTime));
            return new ThirdPersonCameraFollowStep(
                Vector3.Lerp(currentPosition, desiredPosition, lerpFactor),
                lerpFactor);
        }

        public static float ClampPitch(float angle, float minPitch, float maxPitch)
        {
            angle = NormalizeAngle(angle);
            return Mathf.Clamp(angle, minPitch, maxPitch);
        }

        public static float NormalizeAngle(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }
    }
}
