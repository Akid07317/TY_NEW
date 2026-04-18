using System;
using UnityEngine;

namespace CampusRPG.Combat
{
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField] private float maxValue = 100f;
        [SerializeField] private float currentValue = 100f;

        public event Action<float, float> Changed;
        public event Action Died;

        public float MaxValue => maxValue;

        public float CurrentValue => currentValue;

        public bool IsDead => currentValue <= 0f;

        public void SetMax(float value, bool refillCurrent)
        {
            maxValue = Mathf.Max(1f, value);
            currentValue = refillCurrent ? maxValue : Mathf.Min(currentValue, maxValue);
            Changed?.Invoke(currentValue, maxValue);
        }

        public void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source)
        {
            if (IsDead)
            {
                return;
            }

            currentValue = Mathf.Max(0f, currentValue - Mathf.Max(0f, amount));
            Changed?.Invoke(currentValue, maxValue);

            if (currentValue <= 0f)
            {
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
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
