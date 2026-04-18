using UnityEngine;

namespace CampusRPG.Core
{
    [CreateAssetMenu(fileName = "SO_AudioSettings", menuName = "CampusRPG/Core/Audio Settings")]
    public sealed class AudioSettingsSO : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        public float MasterVolume => Mathf.Clamp01(masterVolume);

        public float SfxVolume => Mathf.Clamp01(sfxVolume);

        public float ResolveSfxVolume(float baseVolume)
        {
            return Mathf.Clamp01(baseVolume) * MasterVolume * SfxVolume;
        }
    }
}
