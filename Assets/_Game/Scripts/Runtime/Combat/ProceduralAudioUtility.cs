using System.Collections.Generic;
using CampusRPG.Composition;
using CampusRPG.Core;
using UnityEngine;

namespace CampusRPG.Combat
{
    public static class ProceduralAudioUtility
    {
        private const int SampleRate = 44100;
        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

        public static float ResolveSfxVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            AudioSettingsSO audioSettings = SceneRuntimeReferenceUtility.ResolveAudioSettings();
            return audioSettings != null
                ? audioSettings.ResolveSfxVolume(clampedVolume)
                : clampedVolume;
        }

        public static void PlayChirp(
            Vector3 position,
            float startFrequency,
            float endFrequency,
            float durationSeconds,
            float volume)
        {
            float resolvedVolume = ResolveSfxVolume(volume);

            if (Application.isBatchMode || resolvedVolume <= 0f || durationSeconds <= 0f)
            {
                return;
            }

            AudioClip clip = GetOrCreateChirpClip(startFrequency, endFrequency, durationSeconds);

            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, resolvedVolume);
            }
        }

        private static AudioClip GetOrCreateChirpClip(float startFrequency, float endFrequency, float durationSeconds)
        {
            float clampedDuration = Mathf.Clamp(durationSeconds, 0.02f, 1f);
            float clampedStart = Mathf.Clamp(startFrequency, 40f, 4000f);
            float clampedEnd = Mathf.Clamp(endFrequency, 40f, 4000f);
            string cacheKey =
                Mathf.RoundToInt(clampedStart).ToString() + "_" +
                Mathf.RoundToInt(clampedEnd).ToString() + "_" +
                Mathf.RoundToInt(clampedDuration * 1000f).ToString();

            if (ClipCache.TryGetValue(cacheKey, out AudioClip existingClip))
            {
                return existingClip;
            }

            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(clampedDuration * SampleRate));
            float[] samples = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount > 1 ? i / (float)(sampleCount - 1) : 0f;
                float frequency = Mathf.Lerp(clampedStart, clampedEnd, t);
                float amplitude = Mathf.Pow(1f - t, 2.2f);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                samples[i] = Mathf.Sin(phase) * amplitude * 0.45f;
            }

            AudioClip clip = AudioClip.Create(
                "Chirp_" + cacheKey,
                sampleCount,
                1,
                SampleRate,
                false);
            clip.SetData(samples, 0);
            ClipCache[cacheKey] = clip;
            return clip;
        }
    }
}
