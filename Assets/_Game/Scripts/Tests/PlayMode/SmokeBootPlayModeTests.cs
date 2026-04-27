using System.Collections;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Save;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CampusRPG.Tests.PlayMode
{
    public sealed class SmokeBootPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayModeTestAssemblyCompilesAndRuns()
        {
            yield return null;
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator MainMenuScene_LoadsWithoutConsoleErrors()
        {
            yield return LoadSceneAndWait("MainMenu");

            AssertActiveScene("MainMenu");
            Assert.IsNotNull(FindSceneObject("Main Camera"));
            Assert.IsNotNull(FindRequiredComponent<MainMenuView>());
            Assert.IsNotNull(FindRequiredComponent<SaveService>());
        }

        [UnityTest]
        public IEnumerator Chapter01CombinedScene_LoadsCoreRuntimeObjects()
        {
            yield return LoadSceneAndWait("Chapter01_Combined");

            AssertActiveScene("Chapter01_Combined");
            Assert.IsNotNull(FindSceneObject("Main Camera"));
            Assert.IsNotNull(FindSceneObject("ChapterFlow"));
            Assert.IsNotNull(FindSceneObject("Bootstrap"));
            Assert.IsNotNull(FindSceneObject("SceneRuntimeContext"));
            Assert.IsNotNull(FindRequiredComponent<PlayerCharacter>());
            Assert.IsNotNull(FindRequiredComponent<SceneRuntimeContext>());
            Assert.IsNotNull(FindRequiredComponent<ChapterProgressService>());
            Assert.IsNotNull(FindRequiredComponent<ChapterCompleteView>());
        }

        [UnityTest]
        public IEnumerator BossTestScene_LoadsIndependentBossFightCoreObjects()
        {
            yield return LoadSceneAndWait("BossTest");

            AssertActiveScene("BossTest");
            Assert.IsNotNull(FindSceneObject("BossTestRoot"));
            Assert.IsNotNull(FindSceneObject("Main Camera"));
            Assert.IsNotNull(FindSceneObject("BossTestFlow"));
            Assert.IsNotNull(FindSceneObject("Bootstrap"));
            Assert.IsNotNull(FindSceneObject("Boss_Gatekeeper"));
            Assert.IsNotNull(FindRequiredComponent<PlayerCharacter>());
            Assert.IsNotNull(FindRequiredComponent<SceneRuntimeContext>());
            Assert.IsNotNull(FindRequiredComponent<CombatDebugHUD>());
            Assert.IsNotNull(FindRequiredComponent<BossPresentationRig>());
            Assert.IsNotNull(FindRequiredComponent<EnemyBrain>());
        }

        private static IEnumerator LoadSceneAndWait(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.IsNotNull(operation, sceneName);

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static void AssertActiveScene(string sceneName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Assert.AreEqual(sceneName, activeScene.name);
            Assert.IsTrue(activeScene.isLoaded, sceneName);
        }

        private static TComponent FindRequiredComponent<TComponent>() where TComponent : Component
        {
            TComponent component = Object.FindAnyObjectByType<TComponent>(FindObjectsInactive.Include);
            Assert.IsNotNull(component, typeof(TComponent).Name);
            return component;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject gameObject = gameObjects[i];

                if (gameObject.scene == activeScene && gameObject.name == objectName)
                {
                    return gameObject;
                }
            }

            return null;
        }
    }
}
