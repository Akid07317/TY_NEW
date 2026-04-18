using UnityEngine;

namespace CampusRPG.Combat
{
    internal static class ProjectileImpactFeedbackUtility
    {
        public static void SpawnImpactFeedback(
            Vector3 hitPoint,
            Vector3 direction,
            GameObject impactEffectPrefab,
            float spawnedImpactLifetimeSeconds,
            bool playImpactSound,
            float impactSoundStartFrequency,
            float impactSoundEndFrequency,
            float impactSoundVolume)
        {
            if (impactEffectPrefab != null)
            {
                Quaternion rotation = direction.sqrMagnitude > Mathf.Epsilon
                    ? Quaternion.LookRotation(direction, Vector3.up)
                    : Quaternion.identity;
                GameObject effectInstance = Object.Instantiate(impactEffectPrefab, hitPoint, rotation);
                DestroyRuntimeObject(effectInstance, Mathf.Max(0.05f, spawnedImpactLifetimeSeconds));
            }

            if (playImpactSound)
            {
                ProceduralAudioUtility.PlayChirp(
                    hitPoint,
                    impactSoundStartFrequency,
                    impactSoundEndFrequency,
                    0.1f,
                    impactSoundVolume);
            }
        }

        public static void DestroyRuntimeObject(Object target, float delaySeconds = 0f)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (delaySeconds > 0f)
                {
                    Object.Destroy(target, delaySeconds);
                }
                else
                {
                    Object.Destroy(target);
                }

                return;
            }

            if (delaySeconds > 0f)
            {
                return;
            }

            Object.DestroyImmediate(target);
        }
    }
}
