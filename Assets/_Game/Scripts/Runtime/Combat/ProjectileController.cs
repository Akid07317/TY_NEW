using CampusRPG.Character;
using UnityEngine;

namespace CampusRPG.Combat
{
    public enum ProjectileTrajectoryMode
    {
        PrefabDefault,
        Straight,
        Arc
    }

    [DisallowMultipleComponent]
    public sealed class ProjectileController : MonoBehaviour
    {
        [SerializeField] private float defaultSpeed = 12f;
        [SerializeField] private float defaultLifetimeSeconds = 1.5f;
        [SerializeField] private float defaultHitRadius = 0.25f;
        [SerializeField] private ProjectileTrajectoryMode defaultTrajectoryMode = ProjectileTrajectoryMode.Straight;
        [SerializeField] private float defaultArcHeight;
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField] private float spawnedImpactLifetimeSeconds = 0.2f;
        [SerializeField] private bool playLaunchSound = true;
        [SerializeField] private float launchSoundVolume = 0.08f;
        [SerializeField] private float launchSoundStartFrequency = 1040f;
        [SerializeField] private float launchSoundEndFrequency = 760f;
        [SerializeField] private bool playImpactSound = true;
        [SerializeField] private float impactSoundVolume = 0.12f;
        [SerializeField] private float impactSoundStartFrequency = 480f;
        [SerializeField] private float impactSoundEndFrequency = 180f;

        private GameObject source;
        private Transform sourceRoot;
        private ProjectileOwnerType ownerType;
        private Vector3 direction = Vector3.forward;
        private Vector3 launchPosition;
        private float speed;
        private float elapsedSeconds;
        private float lifetimeRemaining;
        private float launchLifetimeSeconds;
        private float hitRadius;
        private float damage;
        private float arcHeight;
        private float traveledDistance;
        private ProjectileTrajectoryMode trajectoryMode;
        private bool isActive;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Launch(
            GameObject sourceObject,
            Vector3 launchDirection,
            float projectileDamage,
            float launchSpeed,
            float projectileHitRadius,
            float lifetimeSeconds,
            ProjectileTrajectoryMode trajectoryModeOverride = ProjectileTrajectoryMode.PrefabDefault,
            float arcHeightOverride = -1f)
        {
            source = sourceObject;
            sourceRoot = source != null ? source.transform.root : null;
            ownerType = ProjectileImpactResolver.ResolveOwnerType(sourceObject);
            ProjectileLaunchParameters launchParameters = ProjectileFlightUtility.ResolveLaunchParameters(
                transform,
                launchDirection,
                projectileDamage,
                launchSpeed,
                projectileHitRadius,
                lifetimeSeconds,
                defaultSpeed,
                defaultHitRadius,
                defaultLifetimeSeconds,
                defaultTrajectoryMode,
                trajectoryModeOverride,
                defaultArcHeight,
                arcHeightOverride);
            damage = launchParameters.Damage;
            speed = launchParameters.Speed;
            hitRadius = launchParameters.HitRadius;
            lifetimeRemaining = launchParameters.LifetimeSeconds;
            launchLifetimeSeconds = launchParameters.LifetimeSeconds;
            trajectoryMode = launchParameters.TrajectoryMode;
            arcHeight = launchParameters.ArcHeight;
            launchPosition = launchParameters.LaunchPosition;
            elapsedSeconds = 0f;
            traveledDistance = 0f;
            direction = launchParameters.Direction;
            ProjectileTrajectoryUtility.UpdateOrientation(transform, direction);
            isActive = true;

            if (playLaunchSound)
            {
                ProceduralAudioUtility.PlayChirp(
                    transform.position,
                    launchSoundStartFrequency,
                    launchSoundEndFrequency,
                    0.06f,
                    launchSoundVolume);
            }
        }

        public void Tick(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            lifetimeRemaining -= deltaTime;

            if (lifetimeRemaining <= 0f)
            {
                ProjectileImpactFeedbackUtility.DestroyRuntimeObject(gameObject);
                return;
            }

            if (ProjectileImpactResolver.TryHitAtPosition(
                transform.position,
                hitRadius,
                sourceRoot,
                ownerType,
                out IDamageable overlappingDamageable,
                out Vector3 overlapHitPoint))
            {
                ApplyImpact(overlappingDamageable, overlapHitPoint);
                return;
            }

            if (!ProjectileFlightUtility.TryBuildTravelStep(
                transform.position,
                speed,
                deltaTime,
                elapsedSeconds,
                traveledDistance,
                launchPosition,
                direction,
                trajectoryMode,
                arcHeight,
                launchLifetimeSeconds,
                out ProjectileTravelStep travelStep))
            {
                return;
            }

            if (travelStep.PathDistance <= Mathf.Epsilon)
            {
                elapsedSeconds = travelStep.NextElapsedSeconds;
                traveledDistance = travelStep.NextTraveledDistance;
                return;
            }

            if (ProjectileImpactResolver.TryHitOnPath(
                travelStep.StartPosition,
                travelStep.TravelVector,
                hitRadius,
                sourceRoot,
                ownerType,
                out ProjectileHitResult hitResult))
            {
                transform.position = hitResult.HitPoint;
                ProjectileTrajectoryUtility.UpdateOrientation(transform, travelStep.TravelVector / travelStep.PathDistance);
                
                if (hitResult.HitBlocker)
                {
                    StopAtImpact(hitResult.HitPoint);
                }
                else
                {
                    ApplyImpact(hitResult.Damageable, hitResult.HitPoint);
                }

                return;
            }

            elapsedSeconds = travelStep.NextElapsedSeconds;
            traveledDistance = travelStep.NextTraveledDistance;
            transform.position = travelStep.EndPosition;
            ProjectileTrajectoryUtility.UpdateOrientation(transform, travelStep.TravelVector / travelStep.PathDistance);

            if (ProjectileImpactResolver.TryHitAtPosition(
                travelStep.EndPosition,
                hitRadius,
                sourceRoot,
                ownerType,
                out IDamageable endpointDamageable,
                out Vector3 endpointHitPoint))
            {
                ApplyImpact(endpointDamageable, endpointHitPoint);
            }
        }

        private void ApplyImpact(IDamageable damageable, Vector3 hitPoint)
        {
            if (damageable == null)
            {
                return;
            }

            SpawnImpactEffect(hitPoint);
            damageable.ReceiveDamage(damage, hitPoint, source);
            isActive = false;
            ProjectileImpactFeedbackUtility.DestroyRuntimeObject(gameObject);
        }

        private void StopAtImpact(Vector3 hitPoint)
        {
            ProjectileImpactFeedbackUtility.SpawnImpactFeedback(
                hitPoint,
                direction,
                impactEffectPrefab,
                spawnedImpactLifetimeSeconds,
                playImpactSound,
                impactSoundStartFrequency,
                impactSoundEndFrequency,
                impactSoundVolume);
            isActive = false;
            ProjectileImpactFeedbackUtility.DestroyRuntimeObject(gameObject);
        }

        private void SpawnImpactEffect(Vector3 hitPoint)
        {
            ProjectileImpactFeedbackUtility.SpawnImpactFeedback(
                hitPoint,
                direction,
                impactEffectPrefab,
                spawnedImpactLifetimeSeconds,
                playImpactSound,
                impactSoundStartFrequency,
                impactSoundEndFrequency,
                impactSoundVolume);
        }
    }
}
