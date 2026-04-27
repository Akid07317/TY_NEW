using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatTestSceneLightingTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/CombatTest.unity";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void SceneLighting_IsTunedForReadableCombatPreview()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            GameObject lightObject = GameObject.Find("Directional Light");
            Assert.IsNotNull(lightObject, "Directional Light");

            Light directionalLight = lightObject.GetComponent<Light>();
            Assert.IsNotNull(directionalLight, "Directional Light component");
            Assert.AreEqual(LightType.Directional, directionalLight.type);
            Assert.AreEqual(0.85f, directionalLight.intensity, 0.001f);
            Assert.AreEqual(0.7f, RenderSettings.ambientIntensity, 0.001f);
            Assert.AreEqual(0.75f, RenderSettings.reflectionIntensity, 0.001f);
            Assert.AreEqual(DefaultReflectionMode.Skybox, RenderSettings.defaultReflectionMode);
        }
    }
}
