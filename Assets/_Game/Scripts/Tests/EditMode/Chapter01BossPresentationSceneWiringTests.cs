using System.Reflection;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusRPG.Tests.EditMode
{
    public sealed class Chapter01BossPresentationSceneWiringTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Chapter01_Combined.unity";
        private const string GatekeeperTelegraphStylePath = "Assets/_Game/Data/Enemies/SO_BossTelegraphStyle_Gatekeeper.asset";

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
        public void BossPresentationRig_UsesGatekeeperTelegraphStyleAsset()
        {
            Assert.AreEqual(ScenePath, SceneManager.GetActiveScene().path);

            BossPresentationRig rig = FindRequiredComponent<BossPresentationRig>("BossPresentationRig");
            BossAttackCuePresenter cuePresenter = FindRequiredComponent<BossAttackCuePresenter>("BossPresentationRig");
            BossCombatHintView combatHintView = FindRequiredComponent<BossCombatHintView>("BossPresentationRig");
            BossThreatPulsePresenter threatPulsePresenter = FindRequiredComponent<BossThreatPulsePresenter>("BossPresentationRig");
            BossGroundTelegraphPresenter groundTelegraphPresenter = FindRequiredComponent<BossGroundTelegraphPresenter>("BossPresentationRig");
            BossImpactMarkerPresenter impactMarkerPresenter = FindRequiredComponent<BossImpactMarkerPresenter>("BossPresentationRig");
            BossSpawnFlarePresenter spawnFlarePresenter = FindRequiredComponent<BossSpawnFlarePresenter>("BossPresentationRig");
            BossTelegraphStyleSO expectedStyle = AssetDatabase.LoadAssetAtPath<BossTelegraphStyleSO>(GatekeeperTelegraphStylePath);

            Assert.IsNotNull(expectedStyle);
            rig.ApplyConfiguration();

            Assert.AreSame(expectedStyle, GetPrivateField<BossTelegraphStyleSO>(rig, "telegraphStyle"));
            Assert.AreSame(GetPrivateField<object>(rig, "bossEncounter"), GetPrivateField<object>(combatHintView, "bossEncounter"));
            Assert.AreSame(expectedStyle, GetPrivateField<BossTelegraphStyleSO>(cuePresenter, "telegraphStyle"));
            Assert.AreSame(expectedStyle, GetPrivateField<BossTelegraphStyleSO>(threatPulsePresenter, "telegraphStyle"));
            Assert.AreSame(expectedStyle, GetPrivateField<BossTelegraphStyleSO>(groundTelegraphPresenter, "telegraphStyle"));
            Assert.AreSame(expectedStyle, GetPrivateField<BossTelegraphStyleSO>(impactMarkerPresenter, "telegraphStyle"));
            Assert.AreSame(expectedStyle, GetPrivateField<BossTelegraphStyleSO>(spawnFlarePresenter, "telegraphStyle"));
        }

        private static TComponent FindRequiredComponent<TComponent>(string objectName) where TComponent : Component
        {
            GameObject gameObject = GameObject.Find(objectName);
            Assert.IsNotNull(gameObject, objectName);

            TComponent component = gameObject.GetComponent<TComponent>();
            Assert.IsNotNull(component, typeof(TComponent).Name + " on " + objectName);
            return component;
        }

        private static TField GetPrivateField<TField>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TField)field.GetValue(instance);
        }
    }
}
