using CampusRPG.UI;
using NUnit.Framework;
using UnityEditor;

namespace CampusRPG.Tests.EditMode
{
    public sealed class BossTelegraphStyleAssetWiringTests
    {
        private const string GatekeeperTelegraphStylePath = "Assets/_Game/Data/Enemies/SO_BossTelegraphStyle_Gatekeeper.asset";

        [Test]
        public void GatekeeperBossTelegraphStyle_HasRequiredVisualReferences()
        {
            BossTelegraphStyleSO style = AssetDatabase.LoadAssetAtPath<BossTelegraphStyleSO>(GatekeeperTelegraphStylePath);

            Assert.IsNotNull(style);
            Assert.IsNotNull(style.GroundTelegraphVisualPrefab);
            Assert.IsNotNull(style.ImpactMarkerVisualPrefab);
            Assert.IsNotNull(style.SpawnFlareVisualPrefab);
            Assert.IsNotNull(style.EngageTelegraphMaterial);
            Assert.IsNotNull(style.AttackTelegraphMaterial);
            Assert.IsNotNull(style.ImpactMarkerMaterial);
            Assert.IsNotNull(style.SpawnFlareMaterial);
        }
    }
}
