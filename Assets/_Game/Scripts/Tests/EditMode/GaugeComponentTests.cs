using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class GaugeComponentTests
    {
        [Test]
        public void CounterGauge_FillsAndConsumesSuccessfully()
        {
            GameObject go = new GameObject("GaugeTest");
            GaugeComponent component = go.AddComponent<GaugeComponent>();

            component.AddCounter(100f);

            Assert.IsTrue(component.IsCounterFull);
            Assert.IsTrue(component.TryConsumeCounterFull());
            Assert.AreEqual(0f, component.CounterGauge);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AgilityGauge_DoesNotConsumeWhenNotFull()
        {
            GameObject go = new GameObject("GaugeTest");
            GaugeComponent component = go.AddComponent<GaugeComponent>();

            component.AddAgility(50f);

            Assert.IsFalse(component.IsAgilityFull);
            Assert.IsFalse(component.TryConsumeAgilityFull());
            Assert.AreEqual(50f, component.AgilityGauge);

            Object.DestroyImmediate(go);
        }
    }
}
