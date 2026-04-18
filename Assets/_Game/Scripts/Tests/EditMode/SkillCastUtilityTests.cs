using System.Reflection;
using CampusRPG.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class SkillCastUtilityTests
    {
        [Test]
        public void ResolveAimDirection_PrefersLockedTarget_OnHorizontalPlane()
        {
            GameObject ownerObject = new GameObject("Owner");
            GameObject cameraObject = new GameObject("Camera");
            GameObject targetObject = new GameObject("Target");
            SkillDefinitionSO skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();

            try
            {
                ownerObject.transform.position = Vector3.zero;
                cameraObject.transform.forward = Vector3.right;
                targetObject.transform.position = new Vector3(0f, 4f, 6f);
                SetPrivateField(skillDefinition, "targetMode", SkillTargetMode.LockedTarget);

                Vector3 aimDirection = SkillCastUtility.ResolveAimDirection(
                    skillDefinition,
                    ownerObject.transform,
                    cameraObject.transform,
                    targetObject.transform);

                AssertVectorApproximately(Vector3.forward, aimDirection);
            }
            finally
            {
                Object.DestroyImmediate(skillDefinition);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(ownerObject);
            }
        }

        [Test]
        public void ResolveImpactPoint_ReturnsOwnerPosition_ForSelfTarget()
        {
            GameObject ownerObject = new GameObject("Owner");
            SkillDefinitionSO skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();

            try
            {
                ownerObject.transform.position = new Vector3(3f, 0f, 5f);
                SetPrivateField(skillDefinition, "targetMode", SkillTargetMode.Self);

                Vector3 impactPoint = SkillCastUtility.ResolveImpactPoint(
                    skillDefinition,
                    ownerObject.transform,
                    ownerObject.transform,
                    null,
                    Vector3.forward);

                AssertVectorApproximately(new Vector3(3f, 0f, 5f), impactPoint);
            }
            finally
            {
                Object.DestroyImmediate(skillDefinition);
                Object.DestroyImmediate(ownerObject);
            }
        }

        [Test]
        public void TryBuildProjectileLaunchPlan_UsesOriginForward_WhenAimDirectionIsZero()
        {
            GameObject originObject = new GameObject("Origin");
            GameObject projectilePrefab = new GameObject("ProjectilePrefab");
            SkillDefinitionSO skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();

            try
            {
                originObject.transform.forward = Vector3.right;
                SetPrivateField(skillDefinition, "projectilePrefab", projectilePrefab);
                SetPrivateField(skillDefinition, "projectileSpawnOffset", 0.25f);
                SetPrivateField(skillDefinition, "projectileSpeed", 18f);
                SetPrivateField(skillDefinition, "projectileLifetimeSeconds", 1.5f);
                SetPrivateField(skillDefinition, "impactRadius", 0.3f);

                Assert.IsTrue(SkillCastUtility.TryBuildProjectileLaunchPlan(
                    originObject.transform,
                    skillDefinition,
                    Vector3.zero,
                    12f,
                    out SkillProjectileCastPlan plan));

                AssertVectorApproximately(Vector3.right, plan.Direction);
                AssertVectorApproximately(new Vector3(0.25f, 0f, 0f), plan.SpawnPosition);
                Assert.AreEqual(12f, plan.Damage, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(skillDefinition);
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(originObject);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }
    }
}
