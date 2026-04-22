using CampusRPG.AI;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatImportedEnemyAvatarPreviewTests
    {
        private const string LocalPreviewFolderPath = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview";
        private const string EnemyImportedPreviewControllerPath = LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview.controller";

        [Test]
        public void TryApplyHumanoidAvatarPreview_ConfiguresAnimatorAndRestoresProxyBaseline()
        {
            if (!CombatImportedEnemyVisualUtility.HasHumanoidVisualSource(CombatProxyVisualKind.EnemyMelee))
            {
                Assert.Ignore("No compatible imported enemy humanoid preview source is available in this workspace.");
            }

            RuntimeAnimatorController controller = CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController();
            Assert.IsNotNull(controller);

            GameObject enemy = new GameObject("EnemyPreviewRoot");

            try
            {
                CombatProxyVisualUtility.Apply(enemy, CombatProxyVisualKind.EnemyMelee);
                Animator animator = enemy.AddComponent<Animator>();

                bool applied = CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(
                    enemy,
                    CombatProxyVisualKind.EnemyMelee,
                    animator);

                Assert.IsTrue(applied);
                animator.runtimeAnimatorController = controller;

                Transform importedRoot = enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName);
                Transform proxyRoot = enemy.transform.Find("CombatProxyVisualRoot");
                Renderer[] proxyRenderers = proxyRoot != null ? proxyRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];

                Assert.IsNotNull(importedRoot);
                Assert.IsNotNull(proxyRoot);
                Assert.That(proxyRenderers, Is.Not.Empty);
                Assert.IsNotNull(importedRoot.GetComponentInChildren<SkinnedMeshRenderer>(true));
                Assert.IsNotNull(animator.avatar);
                Assert.AreSame(controller, animator.runtimeAnimatorController);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => !renderer.enabled));
                Assert.That(ResolveLowestRendererBoundsY(importedRoot), Is.GreaterThanOrEqualTo(-0.05f));

                bool removed = CombatImportedEnemyVisualUtility.RemoveImportedVisual(enemy, animator);

                Assert.IsTrue(removed);
                Assert.IsNull(enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName));
                Assert.IsNull(animator.avatar);
                Assert.IsNull(animator.runtimeAnimatorController);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                AssetDatabase.DeleteAsset(EnemyImportedPreviewControllerPath);
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        private static float ResolveLowestRendererBoundsY(Transform root)
        {
            Renderer[] renderers = root != null ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new Renderer[0];

            if (renderers.Length == 0 && root != null)
            {
                renderers = root.GetComponentsInChildren<Renderer>(true);
            }

            float minY = float.PositiveInfinity;

            for (int i = 0; i < renderers.Length; i++)
            {
                minY = Mathf.Min(minY, renderers[i].bounds.min.y);
            }

            return minY;
        }
    }
}
