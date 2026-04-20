using System.Collections.Generic;
using CampusRPG.Camera;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CameraObstacleResolverTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void ResolveAdjustedPosition_ReturnsDesiredPosition_WhenNoObstacleBlocksView()
        {
            Vector3 desired = new Vector3(0f, 1.5f, -4f);
            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                desired,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.That(resolved.x, Is.EqualTo(desired.x).Within(0.0001f));
            Assert.That(resolved.y, Is.EqualTo(desired.y).Within(0.0001f));
            Assert.That(resolved.z, Is.EqualTo(desired.z).Within(0.0001f));
        }

        [Test]
        public void ResolveAdjustedPosition_StopsBeforeWall_WhenDesiredCameraPointCrossesObstacle()
        {
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1.5f, -2f);
            wall.transform.localScale = new Vector3(3f, 3f, 0.5f);
            Physics.SyncTransforms();

            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                new Vector3(0f, 1.5f, -4f),
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.Greater(resolved.z, -2f);
            Assert.Less(resolved.z, -1f);
        }

        [Test]
        public void ResolveAdjustedPosition_IgnoresCollidersOnFollowTargetHierarchy()
        {
            GameObject player = Track(new GameObject("Player"));
            GameObject body = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1.5f, -2f);
            wall.transform.localScale = new Vector3(3f, 3f, 0.5f);
            Physics.SyncTransforms();

            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                new Vector3(0f, 1.5f, -1f),
                player.transform,
                0.25f,
                0.1f,
                ~0);

            Assert.Less(resolved.z, -0.75f);
        }

        private GameObject Track(GameObject gameObject)
        {
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
