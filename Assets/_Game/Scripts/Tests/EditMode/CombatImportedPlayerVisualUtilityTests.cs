using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatImportedPlayerVisualUtilityTests
    {
        private const string JcPlayerPrefabPath = "Assets/JC_LP_MedievalCharacters_LITE/Prefabs/SM_MedievalMaleLite_01.prefab";
        private const string WeaponPrefabPath = "Assets/Free medieval weapons/Prefabs/Sword_OH.prefab";
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
        public void GetSelectedPlayerWeaponPrefabPath_PrefersOneHandSwordWhenAvailable()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPath) == null)
            {
                Assert.Ignore("Local preview one-hand sword prefab is not available in this workspace.");
            }

            Assert.AreEqual(WeaponPrefabPath, CombatImportedPlayerVisualUtility.GetSelectedPlayerWeaponPrefabPath());
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

                string[] previewMaterialGuids = AssetDatabase.FindAssets("t:Material", new[] { PlayerLocalPreviewMaterialFolderPath });
                Assert.That(previewMaterialGuids, Is.Not.Empty);

                bool foundTexturedPreviewMaterial = false;

                for (int i = 0; i < previewMaterialGuids.Length; i++)
                {
                    string materialPath = AssetDatabase.GUIDToAssetPath(previewMaterialGuids[i]);
                    Material previewMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                    if (previewMaterial != null && previewMaterial.mainTexture != null)
                    {
                        foundTexturedPreviewMaterial = true;
                        break;
                    }
                }

                Assert.IsTrue(foundTexturedPreviewMaterial, "Expected at least one built-in preview material to retain its main texture.");
            }
            finally
            {
                Object.DestroyImmediate(actor);
                AssetDatabase.DeleteAsset(PlayerLocalPreviewMaterialFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void TryApply_UserOwnedGhostSamurai_UsesDeterministicBuiltinPalette()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CombatImportedPlayerVisualUtility.GhostSamuraiPlayerModelPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(CombatImportedPlayerVisualUtility.GhostSamuraiPlayerWeaponPath) == null)
            {
                Assert.Ignore("GhostSamurai user-owned art sources are not available in this workspace.");
            }

            ImportedPlayerSourceProfile previousProfile = CombatImportedPlayerVisualUtility.SourceProfile;
            bool paletteFolderExisted = AssetDatabase.IsValidFolder(
                CombatImportedPlayerVisualUtility.UserOwnedGhostSamuraiPaletteFolder);
            GameObject actor = new GameObject("UserOwnedGhostSamuraiPreviewRoot");
            Animator animator = actor.AddComponent<Animator>();

            try
            {
                CombatImportedPlayerVisualUtility.SourceProfile = ImportedPlayerSourceProfile.UserOwnedGhostSamurai;

                Assert.IsTrue(CombatImportedPlayerVisualUtility.TryApply(actor, animator));

                Transform importedRoot = actor.transform.Find("ImportedVisualRoot");
                Assert.IsNotNull(importedRoot);
                Assert.IsNotNull(animator.avatar);
                Assert.IsTrue(animator.avatar.isValid);
                Assert.IsTrue(animator.avatar.isHuman);

                Transform weaponRoot = FindDeepChild(actor.transform, "ImportedWeaponVisualRoot");
                Assert.IsNotNull(weaponRoot);
                Assert.IsNotNull(weaponRoot.parent);
                Assert.AreEqual(
                    CombatImportedPlayerVisualUtility.GhostSamuraiRightHandWeaponAnchorName,
                    weaponRoot.parent.name,
                    "The GhostSamurai katana must use the source-authored right-hand weapon socket, not the generic Humanoid hand transform.");
                Assert.IsTrue(
                    CombatImportedPlayerVisualUtility.HasUserOwnedGhostSamuraiWeaponGripContract(actor),
                    "The source-authored Weapon_r socket expects both attachment transforms to stay at identity.");

                Renderer[] renderers = importedRoot.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers, Is.Not.Empty);

                HashSet<string> materialPaths = new HashSet<string>();
                List<Color> colors = new List<Color>();

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Material[] materials = renderers[rendererIndex].sharedMaterials;
                    Assert.That(materials, Is.Not.Empty, renderers[rendererIndex].name);

                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        Material material = materials[materialIndex];
                        Assert.IsNotNull(material, $"{renderers[rendererIndex].name} slot {materialIndex}");
                        Assert.IsTrue(
                            CombatImportedPlayerVisualUtility.IsUserOwnedGhostSamuraiPaletteMaterial(material),
                            $"{renderers[rendererIndex].name} slot {materialIndex}: {AssetDatabase.GetAssetPath(material)}");
                        Assert.IsNotNull(material.shader);
                        Assert.IsTrue(material.shader.isSupported, material.shader.name);
                        Assert.AreEqual("Standard", material.shader.name);
                        Assert.IsNull(material.mainTexture, "The GhostSamurai source package contains no character textures; the internal palette must not fabricate a texture dependency.");

                        materialPaths.Add(AssetDatabase.GetAssetPath(material));
                        colors.Add(material.color);
                    }
                }

                Assert.AreEqual(
                    CombatImportedPlayerVisualUtility.UserOwnedGhostSamuraiExpectedPaletteMaterialCount,
                    materialPaths.Count);
                Assert.GreaterOrEqual(
                    colors.Distinct().Count(),
                    6,
                    "The user-owned palette should visibly separate armor, cloth, eye accent, grip, guard, and blade roles.");
            }
            finally
            {
                CombatImportedPlayerVisualUtility.SourceProfile = previousProfile;
                Object.DestroyImmediate(actor);

                if (!paletteFolderExisted)
                {
                    AssetDatabase.DeleteAsset(CombatImportedPlayerVisualUtility.UserOwnedGhostSamuraiPaletteFolder);
                }

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
                Transform forwardMarker = actor.transform.Find("CombatProxyVisualRoot/ForwardMarker");
                Renderer[] proxyWeaponRenderers = proxyWeaponGrip != null
                    ? proxyWeaponGrip.GetComponentsInChildren<Renderer>(true)
                    : new Renderer[0];
                Renderer[] forwardMarkerRenderers = forwardMarker != null
                    ? forwardMarker.GetComponentsInChildren<Renderer>(true)
                    : new Renderer[0];
                Transform importedWeaponRoot = FindDeepChild(actor.transform, "ImportedWeaponVisualRoot");

                Assert.IsNotNull(importedWeaponRoot);
                Assert.That(proxyWeaponRenderers, Is.Not.Empty);
                Assert.That(forwardMarkerRenderers, Is.Not.Empty);
                Assert.That(proxyWeaponRenderers, Has.All.Matches<Renderer>(renderer => !renderer.enabled));
                Assert.That(forwardMarkerRenderers, Has.All.Matches<Renderer>(renderer => !renderer.enabled));
                Assert.IsNotNull(importedWeaponRoot.GetComponentInChildren<MeshRenderer>(true));
                StringAssert.Contains("Sword", importedWeaponRoot.GetChild(0).name);

                bool removed = CombatImportedPlayerVisualUtility.RemoveImportedVisual(actor, animator);

                Assert.IsTrue(removed);
                Assert.IsNull(FindDeepChild(actor.transform, "ImportedWeaponVisualRoot"));
                Assert.That(proxyWeaponRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled));
                Assert.That(forwardMarkerRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled));
            }
            finally
            {
                Object.DestroyImmediate(actor);
                AssetDatabase.DeleteAsset(PlayerLocalPreviewMaterialFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void SwordOneHandPreview_UsesDedicatedVisualRotationOverride()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPath) == null)
            {
                Assert.Ignore("Local preview one-hand sword prefab is not available in this workspace.");
            }

            MethodInfo resolveRotationMethod = typeof(CombatImportedPlayerVisualUtility).GetMethod(
                "ResolveImportedWeaponVisualLocalRotation",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(resolveRotationMethod);

            Quaternion resolvedRotation = (Quaternion)resolveRotationMethod.Invoke(
                null,
                new object[] { AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPath) });

            Assert.AreEqual(new Quaternion(0.5f, 0.5f, 0.5f, 0.5f), resolvedRotation);
        }

        [Test]
        public void SwordOneHandPreview_UsesDedicatedVisualPositionOverride()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPath) == null)
            {
                Assert.Ignore("Local preview one-hand sword prefab is not available in this workspace.");
            }

            MethodInfo resolvePositionMethod = typeof(CombatImportedPlayerVisualUtility).GetMethod(
                "ResolveImportedWeaponVisualLocalPosition",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(resolvePositionMethod);

            Vector3 resolvedPosition = (Vector3)resolvePositionMethod.Invoke(
                null,
                new object[] { AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabPath) });

            Assert.AreEqual(new Vector3(0.1f, 0.01f, -0.04f), resolvedPosition);
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
