using System.Collections.Generic;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatProxyVisualUtilityTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Apply_RemovesRootPrimitiveRenderer_AndCreatesProxyVisualRoot()
        {
            GameObject actor = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));

            bool changed = CombatProxyVisualUtility.Apply(actor, CombatProxyVisualKind.Player);

            Assert.IsTrue(changed);
            Assert.IsNull(actor.GetComponent<MeshRenderer>());
            Assert.IsNull(actor.GetComponent<MeshFilter>());

            Transform proxyRoot = actor.transform.Find("CombatProxyVisualRoot");
            Assert.IsNotNull(proxyRoot);
            Assert.IsNotNull(proxyRoot.Find("ForwardMarker"));
            Assert.That(proxyRoot.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
        }

        [Test]
        public void Apply_PlayerExternalVisuals_CreatesWeaponOverlayAndKeepsImportedRenderers()
        {
            GameObject actor = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            GameObject importedVisual = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            importedVisual.transform.SetParent(actor.transform, false);
            importedVisual.transform.localPosition = new Vector3(0f, 1f, 0f);

            bool changed = CombatProxyVisualUtility.Apply(actor, CombatProxyVisualKind.Player);

            Assert.IsTrue(changed);
            Transform proxyRoot = actor.transform.Find("CombatProxyVisualRoot");
            Assert.IsNotNull(proxyRoot);
            Assert.IsNotNull(proxyRoot.Find("ForwardMarker"));
            Assert.IsNotNull(proxyRoot.Find("WeaponGrip"));
            Assert.IsNotNull(importedVisual.GetComponent<MeshRenderer>());
        }

        [Test]
        public void Apply_EnemyExternalVisuals_SkipsProxyGenerationAndKeepsImportedRenderers()
        {
            GameObject actor = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            GameObject importedVisual = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            importedVisual.transform.SetParent(actor.transform, false);
            importedVisual.transform.localPosition = new Vector3(0f, 1f, 0f);

            bool changed = CombatProxyVisualUtility.Apply(actor, CombatProxyVisualKind.EnemyMelee);

            Assert.IsTrue(changed);
            Assert.IsNull(actor.transform.Find("CombatProxyVisualRoot"));
            Assert.IsNotNull(importedVisual.GetComponent<MeshRenderer>());
        }

        private GameObject Track(GameObject gameObject)
        {
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
