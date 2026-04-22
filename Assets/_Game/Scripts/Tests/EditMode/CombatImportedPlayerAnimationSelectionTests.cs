using System.Collections.Generic;
using System.Reflection;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;

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
                { "Counter", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim" },
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
        }
    }
}
