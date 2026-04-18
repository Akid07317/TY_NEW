using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.Interaction;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class BossPresentationRigTests
    {
        [Test]
        public void BossPresentationRig_InstallsAndBindsBossCuePresenters()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            BossTelegraphStyleSO telegraphStyle = ScriptableObject.CreateInstance<BossTelegraphStyleSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");
            GameObject encounterObject = new GameObject("Encounter_EN_A04_GATEKEEPER");
            GameObject rigObject = new GameObject("BossPresentationRig");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);

                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);

                EncounterController encounter = encounterObject.AddComponent<EncounterController>();

                BossPresentationRig rig = rigObject.AddComponent<BossPresentationRig>();
                rig.Configure(bossBrain, encounter, telegraphStyle);
                InvokeMethod(rig, "ApplyConfiguration");

                Assert.IsNotNull(rigObject.GetComponent<BossBarPresenter>());
                Assert.IsNotNull(rigObject.GetComponent<BossIntroPresenter>());
                Assert.IsNotNull(rigObject.GetComponent<BossAttackCuePresenter>());
                Assert.IsNotNull(rigObject.GetComponent<BossCombatHintView>());
                Assert.IsNotNull(rigObject.GetComponent<BossArenaStatusPresenter>());
                Assert.IsNotNull(rigObject.GetComponent<BossThreatPulsePresenter>());
                Assert.IsNotNull(rigObject.GetComponent<BossGroundTelegraphPresenter>());
                Assert.IsNotNull(rigObject.GetComponent<BossImpactMarkerPresenter>());
                Assert.IsNotNull(rigObject.GetComponent<BossSpawnFlarePresenter>());

                Assert.AreSame(bossBrain, GetPrivateField<object>(rigObject.GetComponent<BossBarPresenter>(), "bossEnemy"));
                Assert.AreEqual("Campus Gatekeeper", GetPrivateField<string>(rigObject.GetComponent<BossBarPresenter>(), "bossDisplayName"));
                Assert.AreSame(bossBrain, GetPrivateField<object>(rigObject.GetComponent<BossIntroPresenter>(), "bossEnemy"));
                Assert.AreEqual("Boss Encounter", GetPrivateField<string>(rigObject.GetComponent<BossIntroPresenter>(), "encounterLabel"));
                Assert.AreSame(encounter, GetPrivateField<object>(rigObject.GetComponent<BossCombatHintView>(), "bossEncounter"));
                Assert.AreSame(encounter, GetPrivateField<object>(rigObject.GetComponent<BossArenaStatusPresenter>(), "bossEncounter"));
                Assert.AreSame(bossBrain, GetPrivateField<object>(rigObject.GetComponent<BossThreatPulsePresenter>(), "bossEnemy"));
                Assert.AreSame(telegraphStyle, GetPrivateField<object>(rigObject.GetComponent<BossThreatPulsePresenter>(), "telegraphStyle"));
                Assert.AreSame(telegraphStyle, GetPrivateField<object>(rigObject.GetComponent<BossAttackCuePresenter>(), "telegraphStyle"));
                Assert.AreSame(telegraphStyle, GetPrivateField<object>(rigObject.GetComponent<BossGroundTelegraphPresenter>(), "telegraphStyle"));
                Assert.AreSame(telegraphStyle, GetPrivateField<object>(rigObject.GetComponent<BossImpactMarkerPresenter>(), "telegraphStyle"));
                Assert.AreSame(bossBrain, GetPrivateField<object>(rigObject.GetComponent<BossSpawnFlarePresenter>(), "bossEnemy"));
                Assert.AreSame(telegraphStyle, GetPrivateField<object>(rigObject.GetComponent<BossSpawnFlarePresenter>(), "telegraphStyle"));
            }
            finally
            {
                Object.DestroyImmediate(rigObject);
                Object.DestroyImmediate(encounterObject);
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(telegraphStyle);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, arguments);
        }

        private static TValue GetPrivateField<TValue>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TValue)field.GetValue(instance);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
