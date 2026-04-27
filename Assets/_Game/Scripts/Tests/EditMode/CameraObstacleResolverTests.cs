using System.Collections.Generic;
using CampusRPG.Camera;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

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
        public void ResolveAdjustedPosition_KeepsCenteredViewInNarrowCorridor_WhenPathIsClear()
        {
            GameObject leftWall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            leftWall.transform.position = new Vector3(-0.9f, 1.5f, -2f);
            leftWall.transform.localScale = new Vector3(0.2f, 3f, 5f);
            GameObject rightWall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            rightWall.transform.position = new Vector3(0.9f, 1.5f, -2f);
            rightWall.transform.localScale = new Vector3(0.2f, 3f, 5f);
            Physics.SyncTransforms();

            Vector3 desired = new Vector3(0f, 1.5f, -4f);
            CameraObstacleResolution resolution = CameraObstacleResolver.Resolve(
                Vector3.up * 1.5f,
                desired,
                new Vector3(0f, 1.5f, -3.8f),
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.IsFalse(resolution.HasStaticObstruction);
            Assert.IsFalse(resolution.UsedNarrowObstacleSidestep);
            Assert.That(resolution.Position.x, Is.EqualTo(desired.x).Within(0.05f));
            Assert.That(resolution.Position.z, Is.EqualTo(desired.z).Within(0.05f));
        }

        [Test]
        public void ResolveAdjustedPosition_StopsBeforeWall_WhenDesiredCameraPointCrossesObstacle()
        {
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1.5f, -2f);
            wall.transform.localScale = new Vector3(3f, 3f, 0.5f);
            Physics.SyncTransforms();
            Collider wallCollider = wall.GetComponent<Collider>();

            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                new Vector3(0f, 1.5f, -4f),
                new Vector3(0f, 1.5f, -4f),
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.IsNotNull(wallCollider);
            Assert.IsFalse(wallCollider.bounds.Contains(resolved));
            Assert.Greater(Vector3.Distance(resolved, Vector3.up * 1.5f), 1.2f);
            Assert.Greater(Vector3.Distance(resolved, new Vector3(0f, 1.5f, -4f)), 0.5f);
        }

        [Test]
        public void ResolveAdjustedPosition_RetractsAlongBoom_WhenWideWallBlocksView()
        {
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1.5f, -2f);
            wall.transform.localScale = new Vector3(8f, 3f, 0.5f);
            Physics.SyncTransforms();

            Vector3 desired = new Vector3(0f, 1.5f, -4f);
            CameraObstacleResolution resolution = CameraObstacleResolver.Resolve(
                Vector3.up * 1.5f,
                desired,
                desired,
                null,
                0.25f,
                0.1f,
                ~0);
            Vector3 resolved = resolution.Position;

            Assert.IsTrue(resolution.HasStaticObstruction);
            Assert.IsFalse(resolution.UsedNarrowObstacleSidestep);
            Assert.Less(resolution.RetractionRatio, 0.55f);
            Assert.Less(Mathf.Abs(resolved.x), 0.05f);
            Assert.That(resolved.y, Is.EqualTo(desired.y).Within(0.05f));
            Assert.Greater(resolved.z, -1.75f);
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
                new Vector3(0f, 1.5f, -1f),
                player.transform,
                0.25f,
                0.1f,
                ~0);

            Assert.Less(resolved.z, -0.75f);
        }

        [Test]
        public void ResolveAdjustedPosition_IgnoresDynamicCharacterBodies_ButStillAvoidsWalls()
        {
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            enemy.name = "EnemyBody";
            enemy.transform.position = new Vector3(0f, 1f, -1.4f);
            enemy.AddComponent<NavMeshAgent>();

            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1.5f, -3f);
            wall.transform.localScale = new Vector3(3f, 3f, 0.5f);
            Physics.SyncTransforms();

            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                new Vector3(0f, 1.5f, -4f),
                new Vector3(0f, 1.5f, -4f),
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.Less(resolved.z, -2f);
            Assert.IsTrue(resolved.z > -3.5f || resolved.y > 2.5f);
        }

        [Test]
        public void ResolveAdjustedPosition_SlidesAroundNarrowObstacle_InsteadOfCollapsingIntoPlayer()
        {
            GameObject pillar = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            pillar.transform.position = new Vector3(0f, 1.5f, -2f);
            pillar.transform.localScale = new Vector3(0.6f, 3f, 0.6f);
            Physics.SyncTransforms();

            CameraObstacleResolution resolution = CameraObstacleResolver.Resolve(
                Vector3.up * 1.5f,
                new Vector3(0f, 1.5f, -4f),
                new Vector3(0f, 1.5f, -4f),
                null,
                0.25f,
                0.1f,
                ~0);
            Vector3 resolved = resolution.Position;

            Assert.IsTrue(resolution.UsedNarrowObstacleSidestep);
            Assert.Less(resolved.z, -3f);
            Assert.Greater(Mathf.Abs(resolved.x), 0.2f);
        }

        [Test]
        public void ResolveAdjustedPosition_PrefersCurrentSide_WhenAlternativesAreComparable()
        {
            GameObject pillar = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            pillar.transform.position = new Vector3(0f, 1.5f, -2f);
            pillar.transform.localScale = new Vector3(0.6f, 3f, 0.6f);
            Physics.SyncTransforms();

            Vector3 currentPosition = new Vector3(-1.3f, 1.55f, -3.6f);
            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                new Vector3(0.1f, 1.5f, -4f),
                currentPosition,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.Less(resolved.x, -0.3f);
            Assert.Less(Vector3.Distance(resolved, currentPosition), 0.9f);
        }

        [Test]
        public void ResolveAdjustedPosition_DoesNotStayStuckOnPreviousSide_WhenPathIsClear()
        {
            Vector3 desired = new Vector3(0f, 1.5f, -4f);
            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                desired,
                new Vector3(-1.4f, 1.5f, -4f),
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.That(resolved.x, Is.EqualTo(desired.x).Within(0.0001f));
            Assert.That(resolved.y, Is.EqualTo(desired.y).Within(0.0001f));
            Assert.That(resolved.z, Is.EqualTo(desired.z).Within(0.0001f));
        }

        [Test]
        public void ResolveAdjustedPosition_DoesNotAcceptDynamicActorOverlap_WhenPathHasNoStaticObstacle()
        {
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            enemy.transform.position = new Vector3(0f, 1f, -4f);
            enemy.AddComponent<NavMeshAgent>();
            Physics.SyncTransforms();
            Collider enemyCollider = enemy.GetComponent<Collider>();

            Vector3 desired = new Vector3(0f, 1.5f, -4f);
            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                desired,
                desired,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.IsNotNull(enemyCollider);
            AssertPointOutsideCollider(enemyCollider, resolved);
            Assert.Greater(Vector3.Distance(resolved, desired), 0.2f);
        }

        [Test]
        public void ResolveAdjustedPosition_RetreatsOutOfCurrentOverlap_WhenCameraStartsInsideDynamicActor()
        {
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            enemy.transform.position = new Vector3(0f, 1f, -3.2f);
            enemy.AddComponent<NavMeshAgent>();
            Physics.SyncTransforms();
            Collider enemyCollider = enemy.GetComponent<Collider>();

            Vector3 currentPosition = new Vector3(0f, 1.5f, -3.2f);
            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                new Vector3(0f, 1.5f, -4f),
                currentPosition,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.IsNotNull(enemyCollider);
            AssertPointOutsideCollider(enemyCollider, resolved);
            Assert.Greater(Vector3.Distance(resolved, currentPosition), 0.1f);
        }

        [Test]
        public void ResolveAdjustedPosition_DepentratesStaticOverlap_WhenProbeStartsInsideWall()
        {
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(0f, 1.5f, -0.2f);
            wall.transform.localScale = new Vector3(3f, 3f, 0.4f);
            Physics.SyncTransforms();
            Collider wallCollider = wall.GetComponent<Collider>();

            Vector3 origin = new Vector3(0f, 1.5f, 0.02f);
            Vector3 desired = new Vector3(0f, 1.5f, -0.02f);
            CameraObstacleResolution resolution = CameraObstacleResolver.Resolve(
                origin,
                desired,
                desired,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.IsNotNull(wallCollider);
            Assert.IsTrue(resolution.HasStaticObstruction);
            AssertPointOutsideCollider(wallCollider, resolution.Position);
            Assert.Greater(Vector3.Distance(resolution.Position, desired), 0.05f);
        }

        [Test]
        public void IsSegmentOccupiedByDynamicActor_DetectsActorBodiesIgnoredByStaticSegmentCheck()
        {
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            enemy.transform.position = new Vector3(0f, 1f, -2f);
            enemy.AddComponent<CharacterController>();
            Physics.SyncTransforms();

            Vector3 from = new Vector3(0f, 1.5f, -4f);
            Vector3 to = new Vector3(0f, 1.5f, 0f);

            Assert.IsFalse(CameraObstacleResolver.IsSegmentObstructed(from, to, null, 0.25f, ~0));
            Assert.IsTrue(CameraObstacleResolver.IsSegmentOccupiedByDynamicActor(from, to, null, 0.25f, ~0));
        }

        [Test]
        public void IsSegmentOccupiedByDynamicActor_IgnoresDeadActorBodies()
        {
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            enemy.transform.position = new Vector3(0f, 1f, -2f);
            enemy.AddComponent<CharacterController>();
            HealthComponent health = enemy.AddComponent<HealthComponent>();
            health.SetCurrent(0f);
            Physics.SyncTransforms();

            Vector3 from = new Vector3(0f, 1.5f, -4f);
            Vector3 to = new Vector3(0f, 1.5f, 0f);

            Assert.IsFalse(CameraObstacleResolver.IsSegmentOccupiedByDynamicActor(from, to, null, 0.25f, ~0));
        }

        [Test]
        public void ResolveAdjustedPosition_DoesNotRetreatForDeadDynamicActor_WhenPathIsOtherwiseClear()
        {
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            enemy.transform.position = new Vector3(0f, 1f, -4f);
            enemy.AddComponent<NavMeshAgent>();
            HealthComponent health = enemy.AddComponent<HealthComponent>();
            health.SetCurrent(0f);
            Physics.SyncTransforms();

            Vector3 desired = new Vector3(0f, 1.5f, -4f);
            Vector3 resolved = CameraObstacleResolver.ResolveAdjustedPosition(
                Vector3.up * 1.5f,
                desired,
                desired,
                null,
                0.25f,
                0.1f,
                ~0);

            Assert.That(resolved.x, Is.EqualTo(desired.x).Within(0.0001f));
            Assert.That(resolved.y, Is.EqualTo(desired.y).Within(0.0001f));
            Assert.That(resolved.z, Is.EqualTo(desired.z).Within(0.0001f));
        }

        private GameObject Track(GameObject gameObject)
        {
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void AssertPointOutsideCollider(Collider collider, Vector3 point)
        {
            Assert.IsNotNull(collider);
            Assert.Greater(Vector3.Distance(collider.ClosestPoint(point), point), 0.0001f);
        }
    }
}
