using CampusRPG.Camera;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ThirdPersonCameraOrbitUtilityTests
    {
        [Test]
        public void ResolveFreeLookAngles_AppliesLookInput_AndClampsPitch()
        {
            ThirdPersonCameraOrbitAngles angles = ThirdPersonCameraOrbitUtility.ResolveFreeLookAngles(
                30f,
                20f,
                new Vector2(2f, -10f),
                0.5f,
                0.5f,
                -20f,
                24f);

            Assert.AreEqual(31f, angles.Yaw, 0.0001f);
            Assert.AreEqual(24f, angles.Pitch, 0.0001f);
        }

        [Test]
        public void TryResolveLockOnAngles_RotatesTowardLockTarget_WithinSpeedLimit()
        {
            Assert.IsTrue(ThirdPersonCameraOrbitUtility.TryResolveLockOnAngles(
                Vector3.zero,
                new Vector3(4f, 0f, 4f),
                0f,
                0f,
                0f,
                0f,
                90f,
                0.25f,
                -20f,
                55f,
                out ThirdPersonCameraOrbitAngles angles));

            Assert.AreEqual(22.5f, angles.Yaw, 0.25f);
            Assert.AreEqual(0f, angles.Pitch, 0.1f);
        }

        [Test]
        public void ResolveLookPoint_ReturnsMidpoint_WhenLockOnIsActive()
        {
            Vector3 lookPoint = ThirdPersonCameraOrbitUtility.ResolveLookPoint(
                Vector3.zero,
                new Vector3(0f, 0f, 6f),
                true,
                true,
                1.5f,
                0.5f);

            AssertVectorApproximately(new Vector3(0f, 1f, 3f), lookPoint);
        }

        [Test]
        public void ResolveFollowStep_ComputesSmoothedOrbitPosition()
        {
            ThirdPersonCameraFollowStep step = ThirdPersonCameraOrbitUtility.ResolveFollowStep(
                Vector3.zero,
                Vector3.zero,
                new Vector3(0f, 0f, -4f),
                0f,
                0f,
                10f,
                0.1f);

            Assert.AreEqual(1f - Mathf.Exp(-1f), step.LerpFactor, 0.0001f);
            AssertVectorApproximately(new Vector3(0f, 0f, -4f * step.LerpFactor), step.Position);
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }
    }
}
