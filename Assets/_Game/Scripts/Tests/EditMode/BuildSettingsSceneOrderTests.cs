using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace CampusRPG.Tests.EditMode
{
    public sealed class BuildSettingsSceneOrderTests
    {
        [Test]
        public void BuildSettings_FirstEnabledScene_StartsAtMainMenu()
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled)
                .ToArray();

            Assert.IsNotEmpty(enabledScenes);
            Assert.AreEqual("Assets/_Game/Scenes/MainMenu.unity", enabledScenes[0].path);
            Assert.AreEqual("Assets/_Game/Scenes/Chapter01_Combined.unity", enabledScenes[1].path);
            CollectionAssert.Contains(enabledScenes.Select(scene => scene.path).ToArray(), "Assets/_Game/Scenes/Bootstrap.unity");
            CollectionAssert.Contains(enabledScenes.Select(scene => scene.path).ToArray(), "Assets/_Game/Scenes/CombatTest.unity");
            CollectionAssert.Contains(enabledScenes.Select(scene => scene.path).ToArray(), "Assets/_Game/Scenes/BossTest.unity");
        }
    }
}
