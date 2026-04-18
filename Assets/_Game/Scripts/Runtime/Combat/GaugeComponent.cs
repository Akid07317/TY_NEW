using System;
using UnityEngine;

namespace CampusRPG.Combat
{
    public sealed class GaugeComponent : MonoBehaviour
    {
        [SerializeField] private float maxCounterGauge = 100f;
        [SerializeField] private float maxAgilityGauge = 100f;
        [SerializeField] private float counterGauge;
        [SerializeField] private float agilityGauge;

        public event Action<float, float> CounterGaugeChanged;
        public event Action<float, float> AgilityGaugeChanged;

        public float CounterGauge => counterGauge;

        public float AgilityGauge => agilityGauge;

        public bool IsCounterFull => counterGauge >= maxCounterGauge;

        public bool IsAgilityFull => agilityGauge >= maxAgilityGauge;

        public void AddCounter(float amount)
        {
            counterGauge = Mathf.Clamp(counterGauge + Mathf.Max(0f, amount), 0f, maxCounterGauge);
            CounterGaugeChanged?.Invoke(counterGauge, maxCounterGauge);
        }

        public void AddAgility(float amount)
        {
            agilityGauge = Mathf.Clamp(agilityGauge + Mathf.Max(0f, amount), 0f, maxAgilityGauge);
            AgilityGaugeChanged?.Invoke(agilityGauge, maxAgilityGauge);
        }

        public bool TryConsumeCounterFull()
        {
            if (!IsCounterFull)
            {
                return false;
            }

            counterGauge = 0f;
            CounterGaugeChanged?.Invoke(counterGauge, maxCounterGauge);
            return true;
        }

        public bool TryConsumeAgilityFull()
        {
            if (!IsAgilityFull)
            {
                return false;
            }

            agilityGauge = 0f;
            AgilityGaugeChanged?.Invoke(agilityGauge, maxAgilityGauge);
            return true;
        }

        public void ResetAll()
        {
            counterGauge = 0f;
            agilityGauge = 0f;
            CounterGaugeChanged?.Invoke(counterGauge, maxCounterGauge);
            AgilityGaugeChanged?.Invoke(agilityGauge, maxAgilityGauge);
        }
    }
}
