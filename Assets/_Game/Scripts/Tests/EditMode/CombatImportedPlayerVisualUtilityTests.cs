using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatImportedPlayerVisualUtilityTests
    {
        private const string JcPlayerPrefabPath = "Assets/JC_LP_MedievalCharacters_LITE/Prefabs/SM_MedievalMaleLite_01.prefab";
        private const string WeaponPrefabPath = "Assets/Free medieval weapons/Prefabs/Sword_DH.prefab";
        private const string PlayerLocalPreviewMaterialFolderPath = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview/Materials/Player";

        [Test]
        public void GetSelectedPlayerVisualPrefabPath_PrefersJcPreviewWhenAvailable()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(JcPlayerPrefabPath) == null)
            {
                Assert.Ignore("JC_LP local preview player prefab is not available in this workspace.");
            }

            Assert.AreEqual(JcPlayerPrefabPath, CombatImportedPlayerVisualUtility.GetSelectedPlayerVisualPrefabPath());
        }

        [Test]
        public void TryApply_ConvertsHdrpMaterials_ToBuiltinPreviewMaterials()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(JcPlayerPrefabPath) == null)
            {
                Assert.Ignore("JC_LP local preview player prefab is not available in this workspace.");
            }

            GameObject actor = new GameObject("PlayerPreviewRoot");
            Animator animator = actor.AddComponent<Animator>();

            try
            {
                bool applied = CombatImportedPlayerVisualUtility.TryApply(actor, animator);
                Assert.IsTrue(applied);

                Transform importedRoot = actor.transform.Find("ImportedVisualRoot");
                Assert.IsNotNull(importedRoot);

                Renderer[] renderers = importedRoot.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers, Is.Not.Empty);

                for (int i = 0; i < renderers.Length; i++)
                {
                    Material[] sharedMaterials = renderers[i].sharedMaterials;

                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        Material material = sharedMaterials[j];

                        if (material == null || material.shader == null)
                        {
                            continue;
                        }

                        StringAssert.DoesNotContain("HDRP", material.shader.name);
                        StringAssert.DoesNotContain("High Definition", material.shader.name);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(actor);
                AssetDatabase.DeleteAsset(PlayerLocalPreviewMaterialFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void SyncImportedWeaponPreview_AttachesRealWeaponAndHidesProxyWeaponGrip()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(JcPlayerPrefabPath) == null)
            {
                Assert.Ignore("JC_LP local preview player prefab is not available in this workspace.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPath) == null)
            {
                Assert.Ignore("Local preview weapon prefab is not available in this workspace.");
            }

            GameObject actor = new GameObject("PlayerPreviewRoot");
            Animator animator = actor.AddComponent<Animator>();

            try
            {
                bool applied = CombatImportedPlayerVisualUtility.TryApply(actor, animator);
                Assert.IsTrue(applied);

                CombatProxyVisualUtility.Apply(actor, CombatProxyVisualKind.Player);

                bool weaponApplied = CombatImportedPlayerVisualUtility.SyncImportedWeaponPreview(actor, animator);
                Assert.IsTrue(weaponApplied);

                Transform proxyWeaponGrip = actor.transform.Find("CombatProxyVisualRoot/WeaponGrip");
                Renderer[] proxyWeaponRenderers = proxyWeaponGrip != null
                    ? proxyWeaponGrip.GetComponentsInChildren<Renderer>(true)
                    : new Renderer[0];
                Transform importedWeaponRoot = FindDeepChild(actor.transform, "ImportedWeaponVisualRoot");

                Assert.IsNotNull(importedWeaponRoot);
                Assert.That(proxyWeaponRenderers, Is.Not.Empty);
                Assert.That(proxyWeaponRenderers, Has.All.Matches<Renderer>(renderer => !renderer.enabled));
                Assert.IsNotNull(importedWeaponRoot.GetComponentInChildren<MeshRenderer>(true));
                StringAssert.Contains("Sword", importedWeaponRoot.GetChild(0).name);

                bool removed = CombatImportedPlayerVisualUtility.RemoveImportedVisual(actor, animator);

                Assert.IsTrue(removed);
                Assert.IsNull(FindDeepChild(actor.transform, "ImportedWeaponVisualRoot"));
                Assert.That(proxyWeaponRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled));
            }
            finally
            {
                Object.DestroyImmediate(actor);
                AssetDatabase.DeleteAsset(PlayerLocalPreviewMaterialFolderPath);
                AssetDatabase.Refresh();
            }
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeepChild(root.GetChild(i), targetName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
