using System.Reflection;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class PlayerCombatAnimationRelayTests
    {
        [Test]
        public void ResolveActionRecoveryStateName_ReturnsAirborne_WhenGroundingSourceReportsAirborne()
        {
            string stateName = PlayerCombatAnimationRelay.ResolveActionRecoveryStateName(
                hasGroundingSource: true,
                isGrounded: false);

            Assert.AreEqual(PlayerCombatAnimationRelay.AirborneStateName, stateName);
        }

        [Test]
        public void ResolveActionRecoveryStateName_ReturnsLocomotion_WhenGroundingSourceReportsGrounded()
        {
            string stateName = PlayerCombatAnimationRelay.ResolveActionRecoveryStateName(
                hasGroundingSource: true,
                isGrounded: true);

            Assert.AreEqual(PlayerCombatAnimationRelay.LocomotionStateName, stateName);
        }

        [Test]
        public void ResolveActionRecoveryStateName_ReturnsLocomotion_WhenGroundingSourceIsMissing()
        {
            string stateName = PlayerCombatAnimationRelay.ResolveActionRecoveryStateName(
                hasGroundingSource: false,
                isGrounded: false);

            Assert.AreEqual(PlayerCombatAnimationRelay.LocomotionStateName, stateName);
        }

        [Test]
        public void ResolveHitReactionStateName_UsesDedicatedGuardBreakState()
        {
            Assert.AreEqual(
                PlayerCombatAnimationRelay.HitStateName,
                PlayerCombatAnimationRelay.ResolveHitReactionStateName(PlayerHitReactionType.Standard));
            Assert.AreEqual(
                PlayerCombatAnimationRelay.GuardBreakHitStateName,
                PlayerCombatAnimationRelay.ResolveHitReactionStateName(PlayerHitReactionType.GuardBreak));
        }

        [Test]
        public void ResolveEvasiveActionStateName_UsesDedicatedRollAndAirDodgeStates()
        {
            Assert.AreEqual(
                PlayerCombatAnimationRelay.GroundDodgeStateName,
                PlayerCombatAnimationRelay.ResolveEvasiveActionStateName(PlayerEvasiveActionType.GroundDodge));
            Assert.AreEqual(
                PlayerCombatAnimationRelay.CombatRollStateName,
                PlayerCombatAnimationRelay.ResolveEvasiveActionStateName(PlayerEvasiveActionType.CombatRoll));
            Assert.AreEqual(
                PlayerCombatAnimationRelay.AirDodgeStateName,
                PlayerCombatAnimationRelay.ResolveEvasiveActionStateName(PlayerEvasiveActionType.AirDodge));
        }

        [Test]
        public void FormatAttackVariantStateName_UsesStableTwoDigitSuffix()
        {
            Assert.AreEqual("Light_01_01", PlayerCombatAnimationRelay.FormatAttackVariantStateName("Light_01", 1));
            Assert.AreEqual("SwordArt_MoonSever_12", PlayerCombatAnimationRelay.FormatAttackVariantStateName("SwordArt_MoonSever", 12));
        }

        [Test]
        public void ResolveNextAttackVariantStateName_RotatesThroughAvailableVariants()
        {
            const string TempControllerPath = "Assets/_Game/Animations/Characters/CombatTest/TMP_PlayerVariantRelay.controller";
            AssetDatabase.DeleteAsset(TempControllerPath);

            GameObject playerObject = null;

            try
            {
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(TempControllerPath);
                controller.layers[0].stateMachine.AddState("Light_01_01");
                controller.layers[0].stateMachine.AddState("Light_01_02");
                playerObject = new GameObject("Player");
                Animator animator = playerObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                PlayerCombatAnimationRelay relay = playerObject.AddComponent<PlayerCombatAnimationRelay>();
                SetPrivateField(relay, "animator", animator);

                MethodInfo resolveMethod = typeof(PlayerCombatAnimationRelay).GetMethod(
                    "ResolveNextAttackVariantStateName",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.IsNotNull(resolveMethod);
                Assert.AreEqual("Light_01_01", resolveMethod.Invoke(relay, new object[] { "Light_01" }));
                Assert.AreEqual("Light_01_02", resolveMethod.Invoke(relay, new object[] { "Light_01" }));
                Assert.AreEqual("Light_01_01", resolveMethod.Invoke(relay, new object[] { "Light_01" }));
                Assert.AreEqual("Heavy_01", resolveMethod.Invoke(relay, new object[] { "Heavy_01" }));
            }
            finally
            {
                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }

                AssetDatabase.DeleteAsset(TempControllerPath);
            }
        }

        [Test]
        public void NotifyStateChanged_TracksGuardBreakReaction_WithoutAnimator()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerCombatAnimationRelay relay = gameObject.AddComponent<PlayerCombatAnimationRelay>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();

                stateMachine.Initialize(player);
                stateMachine.SwitchToHit(0.08f, PlayerHitReactionType.GuardBreak);

                Assert.AreEqual(PlayerHitReactionType.GuardBreak, relay.CurrentHitReactionType);

                stateMachine.SwitchToLocomotion();

                Assert.AreEqual(PlayerHitReactionType.Standard, relay.CurrentHitReactionType);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NotifyStateChanged_RequestsCameraImpulse_OnGuardBreakReaction()
        {
            GameObject playerObject = null;
            GameObject cameraObject = null;

            try
            {
                playerObject = new GameObject("Player");
                cameraObject = new GameObject("Camera");
                ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                PlayerCombatAnimationRelay relay = playerObject.AddComponent<PlayerCombatAnimationRelay>();
                PlayerStateMachine stateMachine = playerObject.AddComponent<PlayerStateMachine>();

                SetPrivateField(relay, "cameraController", cameraController);

                stateMachine.Initialize(player);
                stateMachine.SwitchToHit(0.08f, PlayerHitReactionType.GuardBreak);

                Assert.AreEqual(PlayerHitReactionType.GuardBreak, relay.CurrentHitReactionType);
                Assert.IsTrue(cameraController.HasActiveImpactImpulse);
            }
            finally
            {
                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }

                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }
            }
        }

        [Test]
        public void NotifyStateChanged_RequestsCameraImpulse_OnRollAndAirDodge()
        {
            GameObject playerObject = null;
            GameObject cameraObject = null;

            try
            {
                playerObject = new GameObject("Player");
                cameraObject = new GameObject("Camera");
                ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                PlayerCombatAnimationRelay relay = playerObject.AddComponent<PlayerCombatAnimationRelay>();
                PlayerStateMachine stateMachine = playerObject.AddComponent<PlayerStateMachine>();
                PlayerDodgeState dodgeState = new PlayerDodgeState(player, stateMachine);

                SetPrivateField(relay, "cameraController", cameraController);

                dodgeState.Configure(PlayerEvasiveActionType.CombatRoll);
                relay.NotifyStateChanged(null, dodgeState);
                Assert.IsTrue(cameraController.HasActiveImpactImpulse);

                cameraController.ResetRuntimeState();

                dodgeState.Configure(PlayerEvasiveActionType.AirDodge);
                relay.NotifyStateChanged(null, dodgeState);
                Assert.IsTrue(cameraController.HasActiveImpactImpulse);
            }
            finally
            {
                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }

                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }
            }
        }

        [Test]
        public void PlayAttack_RequestsCameraImpulse_ForSwordArtAttack()
        {
            GameObject playerObject = null;
            GameObject cameraObject = null;
            AttackDefinitionSO fallingStarAttack = null;

            try
            {
                playerObject = new GameObject("Player");
                cameraObject = new GameObject("Camera");
                ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
                PlayerCombatAnimationRelay relay = playerObject.AddComponent<PlayerCombatAnimationRelay>();
                fallingStarAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
                SetPrivateField(fallingStarAttack, "animationStateName", "SwordArt_FallingStar");
                SetPrivateField(relay, "cameraController", cameraController);

                relay.PlayAttack(fallingStarAttack);

                Assert.IsTrue(cameraController.HasActiveImpactImpulse);
            }
            finally
            {
                if (fallingStarAttack != null)
                {
                    Object.DestroyImmediate(fallingStarAttack);
                }

                if (playerObject != null)
                {
                    Object.DestroyImmediate(playerObject);
                }

                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }
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
