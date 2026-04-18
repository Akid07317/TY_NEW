using UnityEngine;

namespace CampusRPG.Combat
{
    public readonly struct ProjectileLaunchParameters
    {
        public ProjectileLaunchParameters(
            Vector3 direction,
            Vector3 launchPosition,
            float speed,
            float lifetimeSeconds,
            float hitRadius,
            float damage,
            ProjectileTrajectoryMode trajectoryMode,
            float arcHeight)
        {
            Direction = direction;
            LaunchPosition = launchPosition;
            Speed = speed;
            LifetimeSeconds = lifetimeSeconds;
            HitRadius = hitRadius;
            Damage = damage;
            TrajectoryMode = trajectoryMode;
            ArcHeight = arcHeight;
        }

        public Vector3 Direction { get; }

        public Vector3 LaunchPosition { get; }

        public float Speed { get; }

        public float LifetimeSeconds { get; }

        public float HitRadius { get; }

        public float Damage { get; }

        public ProjectileTrajectoryMode TrajectoryMode { get; }

        public float ArcHeight { get; }
    }

    public readonly struct ProjectileTravelStep
    {
        public ProjectileTravelStep(
            Vector3 startPosition,
            Vector3 endPosition,
            Vector3 travelVector,
            float pathDistance,
            float nextElapsedSeconds,
            float nextTraveledDistance)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            TravelVector = travelVector;
            PathDistance = pathDistance;
            NextElapsedSeconds = nextElapsedSeconds;
            NextTraveledDistance = nextTraveledDistance;
        }

        public Vector3 StartPosition { get; }

        public Vector3 EndPosition { get; }

        public Vector3 TravelVector { get; }

        public float PathDistance { get; }

        public float NextElapsedSeconds { get; }

        public float NextTraveledDistance { get; }
    }

    public static class ProjectileFlightUtility
    {
        public static ProjectileLaunchParameters ResolveLaunchParameters(
            Transform projectileTransform,
            Vector3 launchDirection,
            float projectileDamage,
            float launchSpeed,
            float projectileHitRadius,
            float lifetimeSeconds,
            float defaultSpeed,
            float defaultHitRadius,
            float defaultLifetimeSeconds,
            ProjectileTrajectoryMode defaultTrajectoryMode,
            ProjectileTrajectoryMode trajectoryModeOverride,
            float defaultArcHeight,
            float arcHeightOverride)
        {
            Vector3 fallbackDirection = projectileTransform != null ? projectileTransform.forward : Vector3.forward;
            Vector3 direction = launchDirection.sqrMagnitude > Mathf.Epsilon
                ? launchDirection.normalized
                : fallbackDirection;
            ProjectileTrajectoryMode trajectoryMode = trajectoryModeOverride != ProjectileTrajectoryMode.PrefabDefault
                ? trajectoryModeOverride
                : defaultTrajectoryMode;

            return new ProjectileLaunchParameters(
                direction,
                projectileTransform != null ? projectileTransform.position : Vector3.zero,
                Mathf.Max(0.1f, launchSpeed > 0f ? launchSpeed : defaultSpeed),
                Mathf.Max(0.05f, lifetimeSeconds > 0f ? lifetimeSeconds : defaultLifetimeSeconds),
                Mathf.Max(0.05f, projectileHitRadius > 0f ? projectileHitRadius : defaultHitRadius),
                Mathf.Max(0f, projectileDamage),
                trajectoryMode,
                Mathf.Max(0f, arcHeightOverride >= 0f ? arcHeightOverride : defaultArcHeight));
        }

        public static bool TryBuildTravelStep(
            Vector3 currentPosition,
            float speed,
            float deltaTime,
            float elapsedSeconds,
            float traveledDistance,
            Vector3 launchPosition,
            Vector3 direction,
            ProjectileTrajectoryMode trajectoryMode,
            float arcHeight,
            float launchLifetimeSeconds,
            out ProjectileTravelStep step)
        {
            step = default;
            float normalizedDeltaTime = Mathf.Max(0f, deltaTime);
            float travelDistance = speed * normalizedDeltaTime;

            if (travelDistance <= Mathf.Epsilon)
            {
                return false;
            }

            float nextElapsedSeconds = elapsedSeconds + normalizedDeltaTime;
            float nextTraveledDistance = traveledDistance + travelDistance;
            Vector3 endPosition = ProjectileTrajectoryUtility.EvaluatePosition(
                launchPosition,
                direction,
                trajectoryMode,
                arcHeight,
                launchLifetimeSeconds,
                nextTraveledDistance,
                nextElapsedSeconds);
            Vector3 travelVector = endPosition - currentPosition;
            float pathDistance = travelVector.magnitude;
            step = new ProjectileTravelStep(
                currentPosition,
                endPosition,
                travelVector,
                pathDistance,
                nextElapsedSeconds,
                nextTraveledDistance);
            return true;
        }
    }
}
