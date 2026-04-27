using UnityEditor;

namespace CampusRPG.Editor
{
    public static class CodexLocalPreviewBatchRunner
    {
        private const string RefreshImportedCombatPreviewMenu = "CampusRPG/Setup/Local Preview/Refresh Imported Combat Preview";

        // Batch-friendly bridge for refreshing imported player preview assets from the terminal.
        public static void RefreshImportedPlayerPreview()
        {
            CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview = true;
            CombatTestAssetGenerator.RebuildPlayerCombatAnimationAssetsForLocalPreviewMenu();
            CombatTestSceneBuilder.ApplyImportedVisualsToCombatTestPlayerPrefab();
            CombatTestSceneBuilder.RepairCombatTestSceneLighting();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Keeps CombatTest local preview in a consistent state when both player and enemy visuals are needed.
        [MenuItem(RefreshImportedCombatPreviewMenu)]
        public static void RefreshImportedCombatPreview()
        {
            CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview = true;
            CombatTestAssetGenerator.RebuildPlayerCombatAnimationAssetsForLocalPreviewMenu();
            CombatTestSceneBuilder.ApplyImportedVisualsToCombatTestPlayerPrefab();
            CombatTestSceneBuilder.ApplyImportedEnemyAvatarChainToCombatTestEnemyPrefabs();
            CombatTestSceneBuilder.RepairCombatTestSceneLighting();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
