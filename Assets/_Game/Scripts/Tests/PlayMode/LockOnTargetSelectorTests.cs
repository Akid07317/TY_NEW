using System.Collections;
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
}
