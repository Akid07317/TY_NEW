using System.Collections.Generic;
using System.Reflection;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatImportedPlayerAnimationSelectionTests
    {
        [Test]
        public void ImportedAttackCandidatePaths_PreferOneHandedSources_ForLocalPreview()
        {
            Dictionary<string, string> expectedFirstPaths = new Dictionary<string, string>
            {
                { "Light_01", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim" },
                { "Light_02", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim" },
                { "Light_03", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" },
                { "Heavy_01", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" },
                { "DodgeFollowUp", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim" },
                { "DodgeFollowUp_Enhanced", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim" },
                { "Counter", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_1_InPlace.anim" },
                { "Counter_Enhanced", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" }
            };

            MethodInfo resolveMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedAttackClipCandidatePaths",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod);

            foreach (KeyValuePair<string, string> pair in expectedFirstPaths)
            {
                string[] candidatePaths = resolveMethod.Invoke(null, new object[] { pair.Key }) as string[];

                Assert.IsNotNull(candidatePaths, pair.Key);
                Assert.That(candidatePaths, Is.Not.Empty, pair.Key);
                Assert.AreEqual(pair.Value, candidatePaths[0], pair.Key);
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(candidatePaths[0]), candidatePaths[0]);
                StringAssert.Contains("OneHand", candidatePaths[0], pair.Key);
            }

            Assert.AreNotEqual(
                expectedFirstPaths["Light_02"],
                expectedFirstPaths["Counter"],
                "Counter should not keep sharing Light_02's primary imported source.");
        }

        [Test]
        public void ImportedPreviewAttackDurationOverrides_KeepRetimedStatesShort()
        {
            Dictionary<string, float> expectedDurations = new Dictionary<string, float>
            {
                { "Light_01", 0.58f },
                { "Light_02", 0.7f },
                { "Light_03", 0.82f },
                { "Heavy_01", 1.02f },
                { "DodgeFollowUp", 0.6f },
                { "DodgeFollowUp_Enhanced", 0.68f },
                { "Counter", 0.56640005f },
                { "Counter_Enhanced", 0.78f },
                { "SwordArt_SidewindCut", 0.6f },
                { "SwordArt_CrossStep", 0.62f },
                { "SwordArt_RisingCleave", 1f },
                { "SwordArt_IronGateBreak", 0.9f },
                { "SwordArt_FallingStar", 1.05f },
                { "SwordArt_MoonSever", 0.72f }
            };

            MethodInfo resolveMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedPreviewAttackDurationOverride",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod);

            foreach (KeyValuePair<string, float> pair in expectedDurations)
            {
                Assert.AreEqual(
                    pair.Value,
                    (float)resolveMethod.Invoke(null, new object[] { pair.Key }),
                    0.0001f,
                    pair.Key);
            }

            Assert.AreEqual(0f, (float)resolveMethod.Invoke(null, new object[] { "UnknownAttack" }), 0.0001f);
            Assert.Less(
                expectedDurations["SwordArt_CrossStep"],
                expectedDurations["Heavy_01"],
                "Roll follow-up should stay snappier than committed heavy attacks in local preview.");
            Assert.Greater(
                expectedDurations["SwordArt_FallingStar"],
                expectedDurations["Light_03"],
                "Falling Star should keep a larger readable follow-through than the light combo finisher.");
        }

        [Test]
        public void ImportedPreviewClipRefresh_RemovesStaleProxyCurves()
        {
            const string tempClipPath = "Assets/_Game/Animations/Characters/CombatTest/TMP_ImportedPreviewClipRefresh.anim";
            AssetDatabase.DeleteAsset(tempClipPath);

            try
            {
                AnimationClip staleClip = new AnimationClip();
                AnimationUtility.SetEditorCurve(
                    staleClip,
                    EditorCurveBinding.FloatCurve("CombatProxyVisualRoot/Torso", typeof(Transform), "m_LocalPosition.x"),
                    AnimationCurve.Linear(0f, 0f, 0.1f, 1f));
                AssetDatabase.CreateAsset(staleClip, tempClipPath);

                AnimationClip sourceClip = new AnimationClip();
                AnimationUtility.SetEditorCurve(
                    sourceClip,
                    EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootT.x"),
                    AnimationCurve.Linear(0f, 0f, 0.1f, 0.25f));

                MethodInfo createMethod = typeof(CombatTestAssetGenerator).GetMethod(
                    "CreateOrUpdateImportedClip",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.IsNotNull(createMethod);
                Assert.IsNotNull(createMethod.Invoke(
                    null,
                    new object[] { tempClipPath, sourceClip, 0.1f, false, System.Array.Empty<AnimationEvent>() }));

                AnimationClip refreshedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(tempClipPath);
                EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(refreshedClip);

                Assert.IsNotNull(refreshedClip);
                Assert.That(
                    curveBindings,
                    Has.None.Matches<EditorCurveBinding>(binding => binding.path.StartsWith("CombatProxyVisualRoot")),
                    "Imported preview refresh must not keep hidden proxy curves on the visible imported model.");
                Assert.That(
                    curveBindings,
                    Has.Some.Matches<EditorCurveBinding>(binding => binding.type == typeof(Animator) && binding.propertyName == "RootT.x"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(tempClipPath);
            }
        }
    }
}
