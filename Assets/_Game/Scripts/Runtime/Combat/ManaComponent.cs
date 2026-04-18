using System;
using UnityEngine;

namespace CampusRPG.Combat
{
    public sealed class ManaComponent : MonoBehaviour
    {
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private float currentValue = 100f;

        public event Action<float, float> Changed;

        public float MaxValue => maxValue;

        public float CurrentValue => currentValue;

        public void SetMax(float value, bool refillCurrent)
        {
            maxValue = Mathf.Max(1f, value);
            currentValue = refillCurrent ? maxValue : Mathf.Min(currentValue, maxValue);
            Changed?.Invoke(currentValue, maxValue);
        }

        public bool TrySpend(float amount)
        {
            if (currentValue < amount)
            {
                return false;
            }

            currentValue -= Mathf.Max(0f, amount);
            Changed?.Invoke(currentValue, maxValue);
            return true;
        }

        public void Restore(float amount)
        {
            currentValue = Mathf.Clamp(currentValue + Mathf.Max(0f, amount), 0f, maxValue);
            Changed?.Invoke(currentValue, maxValue);
        }

        public void SetCurrent(float value)
        {
            currentValue = Mathf.Clamp(value, 0f, maxValue);
            Changed?.Invoke(currentValue, maxValue);
        }

        public void RestoreFull()
        {
            currentValue = maxValue;
            Changed?.Invoke(currentValue, maxValue);
        }
    }
}
