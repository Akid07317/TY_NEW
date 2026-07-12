using System.Reflection;
using CampusRPG.AI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyCombatAnimationRelayTests
    {
        [Test]
        public void ResolveCombatPoseLayerTargetWeight_DisablesHoldOverlayWhileMoving()
        {
            float idleWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.LocomotionStateName,
                0f,
                0f);
            float walkWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.LocomotionStateName,
                0.4f,
                0f);
            float attackWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.MeleeAttackStateName,
                0f,
                0f);

            Assert.That(idleWeight, Is.GreaterThan(0f).And.LessThan(0.25f));
            Assert.AreEqual(0f, walkWeight, 0.001f);
            Assert.AreEqual(0f, attackWeight, 0.001f);
        }

        [Test]
        public void ResolveCombatPoseLayerTargetWeight_BoostsReadOverlayDuringResponseAttack()
        {
            float earlyReadWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.AntiAirAttackStateName,
                0f,
                0.25f);
            float lateReadWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName,
                0f,
                0.9f);

            Assert.That(earlyReadWeight, Is.GreaterThan(0.35f).And.LessThan(0.6f));
            Assert.That(lateReadWeight, Is.GreaterThan(0.85f).And.LessThanOrEqualTo(0.95f));
            Assert.Greater(lateReadWeight, earlyReadWeight);
        }

        [Test]
        public void FormatAttackVariantStateName_UsesStableTwoDigitSuffix()
        {
            Assert.AreEqual("Attack_Melee_01", EnemyCombatAnimationRelay.FormatAttackVariantStateName("Attack_Melee", 1));
            Assert.AreEqual("Attack_Melee_12", EnemyCombatAnimationRelay.FormatAttackVariantStateName("Attack_Melee", 12));
        }

        [Test]
        public void ResolveNextAttackVariantStateName_RotatesThroughAvailableVariants()
        {
            const string TempControllerPath = "Assets/_Game/Animations/Characters/CombatTest/TMP_EnemyVariantRelay.controller";
            AssetDatabase.DeleteAsset(TempControllerPath);

            GameObject enemy = null;

            try
            {
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(TempControllerPath);
                controller.layers[0].stateMachine.AddState("Attack_Melee_01");
                controller.layers[0].stateMachine.AddState("Attack_Melee_02");
                enemy = new GameObject("Enemy");
                Animator animator = enemy.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                EnemyCombatAnimationRelay relay = enemy.AddComponent<EnemyCombatAnimationRelay>();
                SetPrivateField(relay, "animator", animator);
                SetPrivateField(relay, "baseLayerIndex", 0);

                MethodInfo resolveMethod = typeof(EnemyCombatAnimationRelay).GetMethod(
                    "ResolveNextAttackVariantStateName",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(resolveMethod);
                Assert.AreEqual("Attack_Melee_01", resolveMethod.Invoke(relay, new object[] { "Attack_Melee" }));
                Assert.AreEqual("Attack_Melee_02", resolveMethod.Invoke(relay, new object[] { "Attack_Melee" }));
                Assert.AreEqual("Attack_Melee_01", resolveMethod.Invoke(relay, new object[] { "Attack_Melee" }));
                Assert.AreEqual("Locomotion", resolveMethod.Invoke(relay, new object[] { "Locomotion" }));
            }
            finally
            {
                if (enemy != null)
                {
                    Object.DestroyImmediate(enemy);
                }

                AssetDatabase.DeleteAsset(TempControllerPath);
            }
        }

        [Test]
        public void ShouldRestartClipOnStateReenter_DoesNotRestartLocomotion()
        {
            MethodInfo method = typeof(EnemyCombatAnimationRelay).GetMethod(
                "ShouldRestartClipOnStateReenter",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(method);
            Assert.IsFalse((bool)method.Invoke(null, new object[] { EnemyCombatAnimationPlanUtility.LocomotionStateName }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { EnemyCombatAnimationPlanUtility.MeleeAttackStateName }));
        }

        [Test]
        public void StabilizeImportedPreviewTransforms_RestoresVisualAndAnimatorLocalAnchors()
        {
            GameObject enemy = new GameObject("EnemyRoot");
            GameObject visualRoot = new GameObject("ImportedEnemyVisualRoot");
            GameObject animatorRoot = new GameObject("AvatarRoot");

            try
            {
                visualRoot.transform.SetParent(enemy.transform, false);
                visualRoot.transform.localPosition = new Vector3(0f, -0.08f, 0.03f);
                visualRoot.transform.localRotation = Quaternion.Euler(0f, 4f, 0f);
                visualRoot.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                animatorRoot.transform.SetParent(visualRoot.transform, false);
                animatorRoot.transform.localPosition = new Vector3(0.02f, 0f, -0.01f);
                animatorRoot.transform.localRotation = Quaternion.Euler(1f, 0f, 3f);
                animatorRoot.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
                GameObject boundsProbe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boundsProbe.name = "AnimatedBoundsProbe";
                boundsProbe.transform.SetParent(visualRoot.transform, false);
                boundsProbe.transform.localPosition = new Vector3(2f, 1.7f, -1.3f);
                boundsProbe.transform.localScale = new Vector3(0.8f, 1.2f, 0.5f);

                EnemyCombatAnimationRelay relay = enemy.AddComponent<EnemyCombatAnimationRelay>();
                Vector3 visualAnchorPosition = visualRoot.transform.localPosition;
                Quaternion visualAnchorRotation = visualRoot.transform.localRotation;
                Vector3 visualAnchorScale = visualRoot.transform.localScale;
                Vector3 animatorAnchorPosition = animatorRoot.transform.localPosition;
                Quaternion animatorAnchorRotation = animatorRoot.transform.localRotation;
                Vector3 animatorAnchorScale = animatorRoot.transform.localScale;

                SetPrivateField(relay, "importedVisualRoot", visualRoot.transform);
                SetPrivateField(relay, "hasImportedVisualRootAnchor", true);
                SetPrivateField(relay, "importedVisualRootAnchorLocalPosition", visualAnchorPosition);
                SetPrivateField(relay, "importedVisualRootAnchorLocalRotation", visualAnchorRotation);
                SetPrivateField(relay, "importedVisualRootAnchorLocalScale", visualAnchorScale);
                SetPrivateField(relay, "importedAnimatorTransform", animatorRoot.transform);
                SetPrivateField(relay, "hasImportedAnimatorTransformAnchor", true);
                SetPrivateField(relay, "importedAnimatorAnchorLocalPosition", animatorAnchorPosition);
                SetPrivateField(relay, "importedAnimatorAnchorLocalRotation", animatorAnchorRotation);
                SetPrivateField(relay, "importedAnimatorAnchorLocalScale", animatorAnchorScale);

                visualRoot.transform.localPosition += new Vector3(1.2f, 0.4f, -0.7f);
                visualRoot.transform.localRotation = Quaternion.Euler(12f, 25f, 9f);
                visualRoot.transform.localScale = Vector3.one * 1.8f;
                animatorRoot.transform.localPosition += new Vector3(-0.5f, 0.2f, 0.9f);
                animatorRoot.transform.localRotation = Quaternion.Euler(-11f, 7f, 18f);
                animatorRoot.transform.localScale = Vector3.one * 0.7f;

                MethodInfo stabilizeMethod = typeof(EnemyCombatAnimationRelay).GetMethod(
                    "StabilizeImportedPreviewTransforms",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(stabilizeMethod);
                stabilizeMethod.Invoke(relay, null);

                Assert.AreEqual(visualAnchorPosition, visualRoot.transform.localPosition);
                Assert.That(Quaternion.Angle(visualAnchorRotation, visualRoot.transform.localRotation), Is.LessThan(0.001f));
                Assert.AreEqual(visualAnchorScale, visualRoot.transform.localScale);
                Assert.AreEqual(animatorAnchorPosition, animatorRoot.transform.localPosition);
                Assert.That(Quaternion.Angle(animatorAnchorRotation, animatorRoot.transform.localRotation), Is.LessThan(0.001f));
                Assert.AreEqual(animatorAnchorScale, animatorRoot.transform.localScale);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void AddComponent_DoesNotCreateGameplayRootAnimator()
        {
            GameObject enemy = new GameObject("EnemyRoot");

            try
            {
                enemy.AddComponent<EnemyCombatAnimationRelay>();

                Assert.IsNull(enemy.GetComponent<Animator>());
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
