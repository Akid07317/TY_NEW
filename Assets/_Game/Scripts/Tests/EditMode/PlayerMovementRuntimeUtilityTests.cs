using CampusRPG.Character;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class PlayerMovementRuntimeUtilityTests
    {
        [Test]
        public void ResolveLockOnMoveSpeedScale_ReturnsBackwardScale_WhenMovingBackward()
        {
            GameObject actor = new GameObject("Actor");

            try
            {
                float scale = PlayerMovementRuntimeUtility.ResolveLockOnMoveSpeedScale(
                    actor.transform,
                    Vector3.back,
                    0.92f,
                    0.82f);

                Assert.AreEqual(0.82f, scale, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void ResolveAnimationMoveAxes_UsesForwardMagnitude_WhenFreeLookIsActive()
        {
            GameObject actor = new GameObject("Actor");

            try
            {
                Vector2 axes = PlayerMovementRuntimeUtility.ResolveAnimationMoveAxes(
                    actor.transform,
                    new Vector2(0.6f, 0.8f),
                    Vector3.forward,
                    false);

                Assert.AreEqual(0f, axes.x, 0.001f);
                Assert.AreEqual(1f, axes.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void ResolveAnimationMoveAxes_UsesLocalStrafeAxes_WhenLockOnIsActive()
        {
            GameObject actor = new GameObject("Actor");

            try
            {
                Vector2 axes = PlayerMovementRuntimeUtility.ResolveAnimationMoveAxes(
                    actor.transform,
                    Vector2.right,
                    Vector3.right,
                    true);

                Assert.AreEqual(1f, axes.x, 0.001f);
                Assert.AreEqual(0f, axes.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void TryResolveDodgeDirection_UsesBackstep_WhenLockOnIsActiveWithoutInput()
        {
            GameObject actor = new GameObject("Actor");
            GameObject target = new GameObject("Target");
            target.transform.position = actor.transform.position + Vector3.forward * 3f;

            try
            {
                bool resolved = PlayerMovementRuntimeUtility.TryResolveDodgeDirection(
                    actor.transform,
                    Vector2.zero,
                    null,
                    target.transform,
                    out Vector3 direction,
                    out bool shouldFaceLockOnTarget);

                Assert.IsTrue(resolved);
                Assert.IsTrue(shouldFaceLockOnTarget);
                Assert.AreEqual(0f, direction.x, 0.001f);
                Assert.AreEqual(0f, direction.y, 0.001f);
                Assert.AreEqual(-1f, direction.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void PlayerMovementProbe_FindsMantleTarget_ForLowObstacle()
        {
            GameObject player = new GameObject("Player");
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PlayerMovementProbe probe = player.AddComponent<PlayerMovementProbe>();
            PlayerBaseStatsSO stats = ScriptableObject.CreateInstance<PlayerBaseStatsSO>();
            SerializedObject serializedStats = new SerializedObject(stats);

            serializedStats.FindProperty("mantleMinHeight").floatValue = 0.5f;
            serializedStats.FindProperty("mantleMaxHeight").floatValue = 1.25f;
            serializedStats.FindProperty("mantleForwardDistance").floatValue = 0.8f;
            serializedStats.ApplyModifiedPropertiesWithoutUndo();

            obstacle.transform.position = new Vector3(0f, 0.55f, 0.55f);
            obstacle.transform.localScale = new Vector3(1f, 1.1f, 0.35f);
            Physics.SyncTransforms();

            try
            {
                bool found = probe.TryFindMantleTarget(stats, player.transform, out Vector3 mantleTarget);

                Assert.IsTrue(found);
                Assert.Greater(mantleTarget.y, 0.45f);
                Assert.Greater(mantleTarget.z, 0.4f);
            }
            finally
            {
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(stats);
            }
        }
    }
}
