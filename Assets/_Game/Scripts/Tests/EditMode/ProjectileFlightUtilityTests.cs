using NUnit.Framework;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ProjectileFlightUtilityTests
    {
        [Test]
        public void ResolveLaunchParameters_UsesDefaultsAndTransformForward_WhenInputsAreUnset()
        {
            GameObject projectileObject = new GameObject("Projectile");

            try
            {
                projectileObject.transform.position = new Vector3(1f, 2f, 3f);
                projectileObject.transform.forward = Vector3.right;

                ProjectileLaunchParameters launchParameters = ProjectileFlightUtility.ResolveLaunchParameters(
                    projectileObject.transform,
                    Vector3.zero,
                    -4f,
                    0f,
                    0f,
                    0f,
                    12f,
                    0.25f,
                    1.5f,
                    ProjectileTrajectoryMode.Arc,
                    ProjectileTrajectoryMode.PrefabDefault,
                    0.8f,
                    -1f);

                AssertVectorApproximately(Vector3.right, launchParameters.Direction);
                AssertVectorApproximately(new Vector3(1f, 2f, 3f), launchParameters.LaunchPosition);
                Assert.AreEqual(12f, launchParameters.Speed, 0.0001f);
                Assert.AreEqual(1.5f, launchParameters.LifetimeSeconds, 0.0001f);
                Assert.AreEqual(0.25f, launchParameters.HitRadius, 0.0001f);
                Assert.AreEqual(0f, launchParameters.Damage, 0.0001f);
                Assert.AreEqual(ProjectileTrajectoryMode.Arc, launchParameters.TrajectoryMode);
                Assert.AreEqual(0.8f, launchParameters.ArcHeight, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(projectileObject);
            }
        }

        [Test]
        public void TryBuildTravelStep_UsesTrajectoryEvaluation_AndAccumulatesState()
        {
            Assert.IsTrue(ProjectileFlightUtility.TryBuildTravelStep(
                new Vector3(0f, 0f, 1f),
                10f,
                0.2f,
                0.1f,
                1f,
                Vector3.zero,
                Vector3.forward,
                ProjectileTrajectoryMode.Arc,
                1.2f,
                1.5f,
                out ProjectileTravelStep step));

            Vector3 expectedEnd = ProjectileTrajectoryUtility.EvaluatePosition(
                Vector3.zero,
                Vector3.forward,
                ProjectileTrajectoryMode.Arc,
                1.2f,
                1.5f,
                3f,
                0.3f);

            AssertVectorApproximately(expectedEnd, step.EndPosition);
            AssertVectorApproximately(expectedEnd - new Vector3(0f, 0f, 1f), step.TravelVector);
            Assert.AreEqual((expectedEnd - new Vector3(0f, 0f, 1f)).magnitude, step.PathDistance, 0.0001f);
            Assert.AreEqual(0.3f, step.NextElapsedSeconds, 0.0001f);
            Assert.AreEqual(3f, step.NextTraveledDistance, 0.0001f);
        }

        [Test]
        public void TryBuildTravelStep_ReturnsFalse_WhenNoDistanceIsTraveled()
        {
            Assert.IsFalse(ProjectileFlightUtility.TryBuildTravelStep(
                Vector3.zero,
                10f,
                0f,
                0.1f,
                1f,
                Vector3.zero,
                Vector3.forward,
                ProjectileTrajectoryMode.Straight,
                0f,
                1.5f,
                out ProjectileTravelStep _));
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }
    }
}
