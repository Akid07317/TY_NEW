using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CampusRPG.Camera;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CampusRPG.Tests.PlayMode
{
    public sealed class LockOnTargetSelectorTests
    {
        [UnityTest]
        public IEnumerator AcquireTarget_SelectsNearestForwardEnemy()
        {
            GameObject player = new GameObject("Player");
            GameObject cameraObject = new GameObject("Camera");
            GameObject forwardEnemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            GameObject sideEnemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            LockOnTargetSelector selector = player.AddComponent<LockOnTargetSelector>();
            ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
            forwardEnemy.AddComponent<LockOnTarget>();
            sideEnemy.AddComponent<LockOnTarget>();

            player.transform.position = Vector3.zero;
            cameraObject.transform.position = new Vector3(0f, 2f, -4f);
            cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            forwardEnemy.transform.position = new Vector3(0f, 0f, 6f);
            sideEnemy.transform.position = new Vector3(4f, 0f, 6f);

            cameraController.SetFollowTarget(player.transform);

            SetPrivateField(selector, "cameraController", cameraController);
            SetPrivateField(selector, "cameraTransform", cameraObject.transform);
            SetPrivateField(selector, "targetMask", (LayerMask)~0);
            SetPrivateField(selector, "searchRadius", 20f);
            SetPrivateField(selector, "maxAcquireAngle", 90f);

            yield return null;

            Assert.IsTrue(selector.AcquireTarget());
            Assert.AreEqual(forwardEnemy.transform, selector.CurrentTarget);

            Object.Destroy(player);
            Object.Destroy(cameraObject);
            Object.Destroy(forwardEnemy);
            Object.Destroy(sideEnemy);
        }

        [UnityTest]
        public IEnumerator Update_ClearsTarget_WhenCurrentTargetLeavesSearchRadius()
        {
            GameObject player = new GameObject("Player");
            GameObject cameraObject = new GameObject("Camera");
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            LockOnTargetSelector selector = player.AddComponent<LockOnTargetSelector>();
            ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
            enemy.AddComponent<LockOnTarget>();

            player.transform.position = Vector3.zero;
            cameraObject.transform.position = new Vector3(0f, 2f, -4f);
            cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            enemy.transform.position = new Vector3(0f, 0f, 6f);

            cameraController.SetFollowTarget(player.transform);

            SetPrivateField(selector, "cameraController", cameraController);
            SetPrivateField(selector, "cameraTransform", cameraObject.transform);
            SetPrivateField(selector, "targetMask", (LayerMask)~0);
            SetPrivateField(selector, "searchRadius", 10f);
            SetPrivateField(selector, "maxAcquireAngle", 90f);
            SetPrivateField(selector, "clearTargetIfInvalid", true);

            yield return null;

            Assert.IsTrue(selector.AcquireTarget());
            enemy.transform.position = new Vector3(0f, 0f, 18f);

            yield return null;

            Assert.IsFalse(selector.HasTarget);
            Assert.IsNull(selector.CurrentTarget);

            Object.Destroy(player);
            Object.Destroy(cameraObject);
            Object.Destroy(enemy);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }

    public sealed class ThirdPersonCameraControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_RetreatsOutOfDynamicActor_WhenDesiredCameraPointIsOccupied()
        {
            var cleanup = new List<Object>();
            GameObject player = Track(new GameObject("Player"), cleanup);
            GameObject cameraObject = Track(new GameObject("Camera"), cleanup);
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule), cleanup);

            player.transform.position = Vector3.zero;
            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.identity;
            enemy.transform.position = new Vector3(0f, 1f, -4.5f);
            enemy.AddComponent<CharacterController>();

            ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.SetFollowTarget(player.transform);
            SetPrivateField(controller, "followSharpness", 1000f);
            SetPrivateField(controller, "obstacleMask", (LayerMask)~0);
            Physics.SyncTransforms();

            yield return null;

            Collider enemyCollider = enemy.GetComponent<Collider>();
            Assert.IsNotNull(enemyCollider);
            AssertPointOutsideCollider(enemyCollider, cameraObject.transform.position);
            Assert.Greater(Vector3.Distance(cameraObject.transform.position, new Vector3(0f, 1.8f, -4.5f)), 0.1f);

            DestroyAll(cleanup);
        }

        [UnityTest]
        public IEnumerator LateUpdate_StaysOutOfActors_WhenWallForcesCameraCloser()
        {
            var cleanup = new List<Object>();
            GameObject player = Track(new GameObject("Player"), cleanup);
            GameObject cameraObject = Track(new GameObject("Camera"), cleanup);
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);
            GameObject enemy = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule), cleanup);

            player.transform.position = Vector3.zero;
            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.identity;
            wall.transform.position = new Vector3(0f, 1.5f, -2.4f);
            wall.transform.localScale = new Vector3(4f, 3f, 0.4f);
            enemy.transform.position = new Vector3(0f, 1f, -1.95f);
            enemy.AddComponent<CharacterController>();

            ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.SetFollowTarget(player.transform);
            SetPrivateField(controller, "followSharpness", 1000f);
            SetPrivateField(controller, "obstacleMask", (LayerMask)~0);
            Physics.SyncTransforms();

            yield return null;

            Collider enemyCollider = enemy.GetComponent<Collider>();
            Collider wallCollider = wall.GetComponent<Collider>();
            Assert.IsNotNull(enemyCollider);
            Assert.IsNotNull(wallCollider);
            AssertPointOutsideCollider(enemyCollider, cameraObject.transform.position);
            AssertPointOutsideCollider(wallCollider, cameraObject.transform.position);
            Assert.Greater(Vector3.Distance(cameraObject.transform.position, Vector3.up * 1.5f), 0.6f);

            DestroyAll(cleanup);
        }

        [UnityTest]
        public IEnumerator LateUpdate_RemainsOutsidePlayerBody_WhileBackingIntoWall()
        {
            var cleanup = new List<Object>();
            GameObject player = Track(new GameObject("Player"), cleanup);
            GameObject playerBody = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule), cleanup);
            GameObject cameraObject = Track(new GameObject("Camera"), cleanup);
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);

            player.AddComponent<CharacterController>();
            playerBody.transform.SetParent(player.transform, false);
            playerBody.transform.localPosition = new Vector3(0f, 1f, 0f);
            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.identity;
            wall.transform.position = new Vector3(0f, 1.5f, -2.4f);
            wall.transform.localScale = new Vector3(4f, 3f, 0.4f);

            ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.SetFollowTarget(player.transform);
            SetPrivateField(controller, "followSharpness", 1000f);
            SetPrivateField(controller, "obstacleMask", (LayerMask)~0);

            Collider playerBodyCollider = playerBody.GetComponent<Collider>();
            Collider wallCollider = wall.GetComponent<Collider>();
            Renderer playerBodyRenderer = playerBody.GetComponent<Renderer>();
            Assert.IsNotNull(playerBodyCollider);
            Assert.IsNotNull(wallCollider);
            Assert.IsNotNull(playerBodyRenderer);

            for (int i = 0; i < 6; i++)
            {
                player.transform.position = new Vector3(0f, 0f, -0.18f * i);
                Physics.SyncTransforms();

                yield return null;

                AssertPointOutsideCollider(wallCollider, cameraObject.transform.position);
                bool cameraOutsidePlayerBody = Vector3.Distance(
                    playerBodyCollider.ClosestPoint(cameraObject.transform.position),
                    cameraObject.transform.position) > 0.0001f;
                Assert.IsTrue(cameraOutsidePlayerBody || playerBodyRenderer.forceRenderingOff);
            }

            DestroyAll(cleanup);
        }

        [UnityTest]
        public IEnumerator LateUpdate_HidesOwnerRenderers_WhenWallPushesCameraIntoSelfOcclusionRange()
        {
            var cleanup = new List<Object>();
            GameObject player = Track(new GameObject("Player"), cleanup);
            GameObject cameraObject = Track(new GameObject("Camera"), cleanup);
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);
            GameObject importedVisualRoot = Track(new GameObject("ImportedVisualRoot"), cleanup);
            GameObject importedWeaponVisualRoot = Track(new GameObject("ImportedWeaponVisualRoot"), cleanup);

            importedVisualRoot.transform.SetParent(player.transform, false);
            importedWeaponVisualRoot.transform.SetParent(player.transform, false);
            MeshRenderer bodyRenderer = CreateVisibleMeshWithoutCollider(
                "ImportedBody",
                importedVisualRoot.transform,
                cleanup,
                new Vector3(0f, 1f, 0f),
                new Vector3(0.7f, 1.8f, 0.6f));
            MeshRenderer weaponRenderer = CreateVisibleMeshWithoutCollider(
                "ImportedWeapon",
                importedWeaponVisualRoot.transform,
                cleanup,
                new Vector3(0.35f, 1.15f, 0.15f),
                new Vector3(0.12f, 0.12f, 1f));

            player.transform.position = Vector3.zero;
            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.identity;
            wall.transform.position = new Vector3(0f, 1.5f, -0.8f);
            wall.transform.localScale = new Vector3(4f, 3f, 0.4f);

            ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.SetFollowTarget(player.transform);
            SetPrivateField(controller, "followSharpness", 100000f);
            SetPrivateField(controller, "ownerRendererHideDistance", 0.55f);
            SetPrivateField(controller, "ownerRendererRestorePadding", 0.1f);
            SetPrivateField(controller, "obstacleMask", (LayerMask)~0);
            Physics.SyncTransforms();

            yield return null;

            Assert.IsTrue(bodyRenderer.forceRenderingOff);
            Assert.IsTrue(weaponRenderer.forceRenderingOff);
            bodyRenderer.enabled = false;

            wall.transform.position = new Vector3(8f, 1.5f, -0.8f);
            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.05f);

            for (int i = 0; i < 120 && (bodyRenderer.forceRenderingOff || weaponRenderer.forceRenderingOff); i++)
            {
                Physics.SyncTransforms();
                yield return null;
            }

            Assert.IsFalse(bodyRenderer.forceRenderingOff);
            Assert.IsFalse(bodyRenderer.enabled);
            Assert.IsFalse(weaponRenderer.forceRenderingOff);
            Assert.IsTrue(weaponRenderer.enabled);

            DestroyAll(cleanup);
        }

        [UnityTest]
        public IEnumerator LateUpdate_KeepsOwnerRendererStateStable_AtLiveSmoothing_WhenBackingIntoWall()
        {
            var cleanup = new List<Object>();
            GameObject player = Track(new GameObject("Player"), cleanup);
            GameObject cameraObject = Track(new GameObject("Camera"), cleanup);
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);
            GameObject importedVisualRoot = Track(new GameObject("ImportedVisualRoot"), cleanup);

            importedVisualRoot.transform.SetParent(player.transform, false);
            MeshRenderer bodyRenderer = CreateVisibleMeshWithoutCollider(
                "ImportedBody",
                importedVisualRoot.transform,
                cleanup,
                new Vector3(0f, 1f, 0f),
                new Vector3(0.7f, 1.8f, 0.6f));

            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.identity;
            wall.transform.position = new Vector3(0f, 1.5f, -1.15f);
            wall.transform.localScale = new Vector3(4f, 3f, 0.4f);

            ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.SetFollowTarget(player.transform);
            SetPrivateField(controller, "followSharpness", 14f);
            SetPrivateField(controller, "obstacleMask", (LayerMask)~0);

            bool previousVisible = !bodyRenderer.forceRenderingOff;
            int visibilityTransitions = 0;
            bool sawHidden = false;
            Vector3 previousCameraPosition = cameraObject.transform.position;
            float maxFrameMotionAfterHide = 0f;

            for (int i = 0; i < 12; i++)
            {
                player.transform.position = new Vector3(0f, 0f, -0.08f * i);
                Physics.SyncTransforms();

                yield return null;

                bool currentVisible = !bodyRenderer.forceRenderingOff;

                if (currentVisible != previousVisible)
                {
                    visibilityTransitions++;
                }

                if (!currentVisible)
                {
                    if (sawHidden)
                    {
                        maxFrameMotionAfterHide = Mathf.Max(
                            maxFrameMotionAfterHide,
                            Vector3.Distance(previousCameraPosition, cameraObject.transform.position));
                    }
                    else
                    {
                        sawHidden = true;
                    }
                }

                previousVisible = currentVisible;
                previousCameraPosition = cameraObject.transform.position;
            }

            Assert.IsTrue(sawHidden);
            Assert.LessOrEqual(visibilityTransitions, 1);
            Assert.Less(maxFrameMotionAfterHide, 0.8f);

            DestroyAll(cleanup);
        }

        [UnityTest]
        public IEnumerator LateUpdate_FollowsLateralPlayerMovement_WhenCameraIsRetractedByWall()
        {
            var cleanup = new List<Object>();
            GameObject player = Track(new GameObject("Player"), cleanup);
            GameObject cameraObject = Track(new GameObject("Camera"), cleanup);
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);

            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.identity;
            wall.transform.position = new Vector3(0f, 1.5f, -1.2f);
            wall.transform.localScale = new Vector3(8f, 3f, 0.35f);

            ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.SetFollowTarget(player.transform);
            SetPrivateField(controller, "obstructionFollowSharpness", 100000f);
            SetPrivateField(controller, "obstructionOverheadDelay", 999f);
            SetPrivateField(controller, "obstacleMask", (LayerMask)~0);

            Collider wallCollider = wall.GetComponent<Collider>();
            Assert.IsNotNull(wallCollider);

            for (int i = 0; i < 16; i++)
            {
                player.transform.position = new Vector3(0.1f * i, 0f, 0f);
                Physics.SyncTransforms();

                yield return null;

                AssertPointOutsideCollider(wallCollider, cameraObject.transform.position);
            }

            Assert.Greater(cameraObject.transform.position.x, 1f);
            Assert.Less(Mathf.Abs(cameraObject.transform.position.x - player.transform.position.x), 0.3f);

            DestroyAll(cleanup);
        }

        [UnityTest]
        public IEnumerator LateUpdate_DampsCornerAdjustment_WhenWallAndGroundBothConstrainCamera()
        {
            var cleanup = new List<Object>();
            GameObject player = Track(new GameObject("Player"), cleanup);
            GameObject cameraObject = Track(new GameObject("Camera"), cleanup);
            GameObject wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);
            GameObject ground = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);

            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
            wall.transform.position = new Vector3(0f, 1f, -1.2f);
            wall.transform.localScale = new Vector3(4f, 2f, 0.35f);
            ground.transform.position = new Vector3(0f, -0.2f, -2.4f);
            ground.transform.localScale = new Vector3(6f, 0.4f, 6f);

            ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.SetFollowTarget(player.transform);
            SetPrivateField(controller, "followSharpness", 14f);
            SetPrivateField(controller, "obstructionFollowSharpness", 10f);
            SetPrivateField(controller, "obstructionOverheadDelay", 0.01f);
            SetPrivateField(controller, "obstructionOverheadEnterSharpness", 40f);
            SetPrivateField(controller, "followOffset", new Vector3(0f, 1f, -4.5f));
            SetPrivateField(controller, "obstacleMask", (LayerMask)~0);

            Collider wallCollider = wall.GetComponent<Collider>();
            Collider groundCollider = ground.GetComponent<Collider>();
            Assert.IsNotNull(wallCollider);
            Assert.IsNotNull(groundCollider);

            Vector3 previousCameraPosition = cameraObject.transform.position;
            float maxFrameMotionAfterSettling = 0f;
            float maxAbsXAfterSettling = 0f;
            float maxYAfterSettling = cameraObject.transform.position.y;
            float earlyObstructedY = cameraObject.transform.position.y;

            for (int i = 0; i < 160; i++)
            {
                player.transform.position = new Vector3(0f, 0f, -Mathf.Min(0.02f * i, 0.75f));
                Physics.SyncTransforms();

                yield return null;

                AssertPointOutsideCollider(wallCollider, cameraObject.transform.position);
                AssertPointOutsideCollider(groundCollider, cameraObject.transform.position);

                if (i >= 2)
                {
                    maxAbsXAfterSettling = Mathf.Max(maxAbsXAfterSettling, Mathf.Abs(cameraObject.transform.position.x));
                    maxYAfterSettling = Mathf.Max(maxYAfterSettling, cameraObject.transform.position.y);
                }

                if (i >= 40)
                {
                    maxFrameMotionAfterSettling = Mathf.Max(
                        maxFrameMotionAfterSettling,
                        Vector3.Distance(previousCameraPosition, cameraObject.transform.position));
                }

                if (i == 4)
                {
                    earlyObstructedY = cameraObject.transform.position.y;
                }

                previousCameraPosition = cameraObject.transform.position;
            }

            Assert.Less(maxFrameMotionAfterSettling, 0.5f);
            Assert.Less(maxAbsXAfterSettling, 0.2f);
            Assert.Less(earlyObstructedY, 2f);
            float overheadBlend = GetPrivateField<float>(controller, "obstructionOverheadBlend");
            Assert.Greater(overheadBlend, 0.5f);
            Assert.GreaterOrEqual(maxYAfterSettling, earlyObstructedY - 0.1f);

            DestroyAll(cleanup);
        }

        private static GameObject Track(GameObject gameObject, List<Object> cleanup)
        {
            cleanup.Add(gameObject);
            return gameObject;
        }

        private static void DestroyAll(List<Object> cleanup)
        {
            for (int i = 0; i < cleanup.Count; i++)
            {
                if (cleanup[i] != null)
                {
                    Object.Destroy(cleanup[i]);
                }
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static TValue GetPrivateField<TValue>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TValue)field.GetValue(instance);
        }

        private static void AssertPointOutsideCollider(Collider collider, Vector3 point)
        {
            float distance = Vector3.Distance(collider.ClosestPoint(point), point);
            Assert.Greater(distance, 0.0001f, $"{collider.name} contains camera point {point}");
        }

        private static MeshRenderer CreateVisibleMeshWithoutCollider(
            string name,
            Transform parent,
            List<Object> cleanup,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject visual = Track(GameObject.CreatePrimitive(PrimitiveType.Cube), cleanup);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;

            Collider collider = visual.GetComponent<Collider>();

            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer);
            return renderer;
        }
    }
}
