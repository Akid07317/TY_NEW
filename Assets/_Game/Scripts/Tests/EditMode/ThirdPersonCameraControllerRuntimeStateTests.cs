using System.Reflection;
using CampusRPG.Camera;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ThirdPersonCameraControllerRuntimeStateTests
    {
        [Test]
        public void ResetRuntimeState_ClearsLockOnAndRestoresHiddenOwnerRenderers()
        {
            GameObject cameraObject = null;
            GameObject playerObject = null;
            GameObject proxyVisualRoot = null;
            GameObject bodyObject = null;
            GameObject enemyTargetObject = null;

            try
            {
                cameraObject = new GameObject("Camera");
                playerObject = new GameObject("Player");
                proxyVisualRoot = new GameObject("CombatProxyVisualRoot");
                bodyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                proxyVisualRoot.transform.SetParent(playerObject.transform, false);
                bodyObject.transform.SetParent(proxyVisualRoot.transform, false);

                ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
                controller.SetFollowTarget(playerObject.transform);

                Renderer bodyRenderer = bodyObject.GetComponent<Renderer>();
                Assert.IsNotNull(bodyRenderer);

                enemyTargetObject = new GameObject("EnemyTarget");
                controller.SetLockOnTarget(enemyTargetObject.transform);
                controller.SetLockOnActive(true);
                SetPrivateField(controller, "isObstacleAdjustmentActive", true);
                SetPrivateField(controller, "obstructionOverheadBlend", 0.75f);
                SetPrivateField(controller, "obstructionSeconds", 0.4f);
                InvokePrivateMethod(controller, "EnsureOwnerRendererCache");
                SetPrivateField(controller, "ownerRenderersHidden", true);
                bodyRenderer.forceRenderingOff = true;

                controller.ResetRuntimeState();

                Assert.IsFalse(controller.IsLockOnActive);
                Assert.IsNull(controller.LockOnTarget);
                Assert.IsFalse(GetPrivateField<bool>(controller, "isObstacleAdjustmentActive"));
                Assert.AreEqual(0f, GetPrivateField<float>(controller, "obstructionOverheadBlend"), 0.0001f);
                Assert.AreEqual(0f, GetPrivateField<float>(controller, "obstructionSeconds"), 0.0001f);
                Assert.IsFalse(GetPrivateField<bool>(controller, "ownerRenderersHidden"));
                Assert.IsFalse(bodyRenderer.forceRenderingOff);
            }
            finally
            {
                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }

                if (enemyTargetObject != null)
                {
                    Object.DestroyImmediate(enemyTargetObject);
                }
            }
        }

        [Test]
        public void RequestImpactImpulse_ActivatesUntilRuntimeStateReset()
        {
            GameObject cameraObject = null;

            try
            {
                cameraObject = new GameObject("Camera");
                ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();

                Assert.IsFalse(controller.HasActiveImpactImpulse);

                controller.RequestImpactImpulse(Vector3.back, 0.18f, 0.16f);

                Assert.IsTrue(controller.HasActiveImpactImpulse);
                Assert.AreEqual(0, controller.CurrentImpactImpulsePriority);

                controller.ResetRuntimeState();

                Assert.IsFalse(controller.HasActiveImpactImpulse);
                Assert.AreEqual(0, controller.CurrentImpactImpulsePriority);
            }
            finally
            {
                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }
            }
        }

        [Test]
        public void TryRequestImpactImpulse_PreservesHighPriorityReadCueFromLowerPriorityMotion()
        {
            GameObject cameraObject = null;

            try
            {
                cameraObject = new GameObject("Camera");
                ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();

                Assert.IsTrue(controller.TryRequestImpactImpulse(Vector3.back, 0.18f, 0.16f, ActionCameraFeedbackUtility.ImpulsePriorityHeavyRead));
                Assert.AreEqual(ActionCameraFeedbackUtility.ImpulsePriorityHeavyRead, controller.CurrentImpactImpulsePriority);

                Assert.IsFalse(controller.TryRequestImpactImpulse(Vector3.forward, 0.06f, 0.1f, ActionCameraFeedbackUtility.ImpulsePriorityMinor));
                Assert.AreEqual(ActionCameraFeedbackUtility.ImpulsePriorityHeavyRead, controller.CurrentImpactImpulsePriority);

                Assert.IsTrue(controller.TryRequestImpactImpulse(Vector3.forward, 0.08f, 0.1f, ActionCameraFeedbackUtility.ImpulsePriorityGuardBreak));
                Assert.AreEqual(ActionCameraFeedbackUtility.ImpulsePriorityGuardBreak, controller.CurrentImpactImpulsePriority);
            }
            finally
            {
                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }
            }
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
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
    }
}
