using System.IO;
using CampusRPG.AI;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatImportedEnemyAvatarPreviewTests
    {
        private const string LocalPreviewFolderPath = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview";
        private const string EnemyImportedPreviewControllerPath = LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyMelee.controller";
        private static readonly string[] CommittedPreviewControllerPaths =
        {
            LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyMelee.controller",
            LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyMobile.controller",
            LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyRanged.controller"
        };

        [Test]
        public void CommittedImportedPreviewControllers_ExposeCurrentResponseReadStates()
        {
            bool foundController = false;

            for (int i = 0; i < CommittedPreviewControllerPaths.Length; i++)
            {
                string controllerPath = CommittedPreviewControllerPaths[i];

                if (!File.Exists(controllerPath))
                {
                    continue;
                }

                foundController = true;
                AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

                Assert.IsNotNull(animatorController, controllerPath);
                AssertImportedResponseReadStates(animatorController, controllerPath);
            }

            if (!foundController)
            {
                Assert.Ignore("No imported local-preview enemy AnimatorController assets are present in this workspace.");
            }
        }

        [Test]
        public void EnsureImportedAvatarPreviewController_UsesReadableLocomotionThresholds()
        {
            if (!CombatImportedEnemyVisualUtility.HasHumanoidVisualSource(CombatProxyVisualKind.EnemyMelee))
            {
                Assert.Ignore("No compatible imported enemy humanoid preview source is available in this workspace.");
            }

            try
            {
                RuntimeAnimatorController controller = CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyMelee);
                AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyImportedPreviewControllerPath);

                Assert.IsNotNull(controller);
                Assert.IsNotNull(animatorController);

                AnimatorState locomotionState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.LocomotionStateName);

                Assert.IsNotNull(locomotionState);
                Assert.IsInstanceOf<BlendTree>(locomotionState.motion);

                BlendTree blendTree = (BlendTree)locomotionState.motion;

                Assert.That(blendTree.children, Has.Length.EqualTo(3));
                Assert.AreEqual(0f, blendTree.children[0].threshold, 0.001f);
                Assert.Less(blendTree.children[1].threshold, 0.25f);
                Assert.Less(blendTree.children[2].threshold, 0.8f);
                Assert.That(blendTree.children[1].motion.name, Does.Contain("1Hand_Up_Walk"));
                Assert.That(blendTree.children[2].motion.name, Does.Contain("1Hand_Up_Run"));
                Assert.That(animatorController.layers, Has.Length.GreaterThanOrEqualTo(2));
                Assert.AreEqual("CombatPose", animatorController.layers[1].name);
                Assert.IsNotNull(animatorController.layers[1].avatarMask);
                Assert.AreEqual(0f, animatorController.layers[1].defaultWeight, 0.001f);
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.ResponseReadParameterName));
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.AntiAirReadParameterName));
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName));
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName));
                AnimatorState rangedAttackState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.RangedAttackStateName);
                AnimatorState mobileAttackState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.MobileAttackStateName);
                AnimatorState meleeAttackState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.MeleeAttackStateName);
                AnimatorState antiAirState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.AntiAirAttackStateName);
                AnimatorState chaseRollState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName);
                AnimatorState guardBreakState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName);

                Assert.IsNotNull(rangedAttackState);
                Assert.IsNotNull(mobileAttackState);
                Assert.IsNotNull(meleeAttackState);
                Assert.IsNotNull(antiAirState);
                Assert.IsNotNull(chaseRollState);
                Assert.IsNotNull(guardBreakState);
                Assert.AreSame(rangedAttackState.motion, antiAirState.motion);
                Assert.AreSame(mobileAttackState.motion, chaseRollState.motion);
                Assert.AreSame(meleeAttackState.motion, guardBreakState.motion);
                Assert.Greater(antiAirState.speed, rangedAttackState.speed);
                Assert.Less(chaseRollState.speed, mobileAttackState.speed);
                Assert.Less(guardBreakState.speed, 1f);
            }
            finally
            {
                AssetDatabase.DeleteAsset(EnemyImportedPreviewControllerPath);
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void TryApplyHumanoidAvatarPreview_ConfiguresAnimatorAndRestoresProxyBaseline()
        {
            if (!CombatImportedEnemyVisualUtility.HasHumanoidVisualSource(CombatProxyVisualKind.EnemyMelee))
            {
                Assert.Ignore("No compatible imported enemy humanoid preview source is available in this workspace.");
            }

            RuntimeAnimatorController controller = CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyMelee);
            Assert.IsNotNull(controller);

            GameObject enemy = new GameObject("EnemyPreviewRoot");

            try
            {
                CombatProxyVisualUtility.Apply(enemy, CombatProxyVisualKind.EnemyMelee);
                Animator animator = enemy.AddComponent<Animator>();

                bool applied = CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(
                    enemy,
                    CombatProxyVisualKind.EnemyMelee,
                    animator);

                Assert.IsTrue(applied);
                animator.runtimeAnimatorController = controller;

                Transform importedRoot = enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName);
                Transform proxyRoot = enemy.transform.Find("CombatProxyVisualRoot");
                Renderer[] proxyRenderers = proxyRoot != null ? proxyRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];

                Assert.IsNotNull(importedRoot);
                Assert.IsNotNull(proxyRoot);
                Assert.That(proxyRenderers, Is.Not.Empty);
                Assert.IsNotNull(importedRoot.GetComponentInChildren<SkinnedMeshRenderer>(true));
                Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(enemy);
                Assert.IsNotNull(importedAnimator);
                importedAnimator.runtimeAnimatorController = controller;
                Assert.IsNull(animator.avatar);
                Assert.IsFalse(animator.enabled);
                Assert.IsNotNull(importedAnimator.avatar);
                Assert.AreSame(controller, importedAnimator.runtimeAnimatorController);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => !renderer.enabled));
                Assert.That(ResolveLowestRendererBoundsY(importedRoot), Is.GreaterThanOrEqualTo(-0.08f));

                bool removed = CombatImportedEnemyVisualUtility.RemoveImportedVisual(enemy, animator);

                Assert.IsTrue(removed);
                Assert.IsNull(enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName));
                Assert.IsTrue(animator.enabled);
                Assert.IsNull(animator.avatar);
                Assert.IsNull(animator.runtimeAnimatorController);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                AssetDatabase.DeleteAsset(EnemyImportedPreviewControllerPath);
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        private static void AssertImportedResponseReadStates(AnimatorController animatorController, string context)
        {
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.ResponseReadParameterName), context);
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.AntiAirReadParameterName), context);
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName), context);
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName), context);

            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            AnimatorState rangedAttackState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.RangedAttackStateName);
            AnimatorState mobileAttackState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.MobileAttackStateName);
            AnimatorState meleeAttackState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.MeleeAttackStateName);
            AnimatorState antiAirState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.AntiAirAttackStateName);
            AnimatorState chaseRollState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName);
            AnimatorState guardBreakState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName);

            Assert.IsNotNull(rangedAttackState, context);
            Assert.IsNotNull(mobileAttackState, context);
            Assert.IsNotNull(meleeAttackState, context);
            Assert.IsNotNull(antiAirState, context);
            Assert.IsNotNull(chaseRollState, context);
            Assert.IsNotNull(guardBreakState, context);
            Assert.AreSame(rangedAttackState.motion, antiAirState.motion, context);
            Assert.AreSame(mobileAttackState.motion, chaseRollState.motion, context);
            Assert.AreSame(meleeAttackState.motion, guardBreakState.motion, context);
            Assert.Greater(antiAirState.speed, rangedAttackState.speed, context);
            Assert.Less(chaseRollState.speed, mobileAttackState.speed, context);
            Assert.Less(guardBreakState.speed, meleeAttackState.speed, context);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return null;
        }

        private static bool HasFloatParameter(AnimatorController controller, string parameterName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];

                if (parameter.type == AnimatorControllerParameterType.Float
                    && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private static float ResolveLowestRendererBoundsY(Transform root)
        {
            Renderer[] renderers = root != null ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new Renderer[0];

            if (renderers.Length == 0 && root != null)
            {
                renderers = root.GetComponentsInChildren<Renderer>(true);
            }

            float minY = float.PositiveInfinity;

            for (int i = 0; i < renderers.Length; i++)
            {
                minY = Mathf.Min(minY, renderers[i].bounds.min.y);
            }

            return minY;
        }
    }
}
