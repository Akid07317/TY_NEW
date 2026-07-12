using System;
using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Input;
using CampusRPG.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class PlayerStateMachineLifecycleTests
    {
        [Test]
        public void PlayerStateMachine_Reenable_RestoresInputAndDeathSubscriptionsWithoutDuplication()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                InputReader inputReader = gameObject.AddComponent<InputReader>();
                HealthComponent health = gameObject.AddComponent<HealthComponent>();

                SetPrivateField(player, "inputReader", inputReader);
                SetPrivateField(player, "health", health);

                stateMachine.Initialize(player);

                Assert.AreEqual(1, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(1, GetEventSubscriberCount(health, "Died"));

                InvokePrivateMethod(stateMachine, "OnDisable");

                Assert.AreEqual(0, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(0, GetEventSubscriberCount(health, "Died"));

                InvokePrivateMethod(stateMachine, "OnEnable");

                Assert.AreEqual(1, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(1, GetEventSubscriberCount(health, "Died"));

                InvokePrivateMethod(stateMachine, "OnEnable");

                Assert.AreEqual(1, GetEventSubscriberCount(inputReader, "LightAttackPressed"));
                Assert.AreEqual(1, GetEventSubscriberCount(health, "Died"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerHitState_AllowsMovement_AndCanBeInterruptedByDodge()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "stateMachine", stateMachine);

                stateMachine.Initialize(player);
                stateMachine.SwitchToHit(0.5f);

                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.IsTrue(stateMachine.AllowsMovement);
                Assert.IsTrue(stateMachine.AllowsJump);

                stateMachine.Tick(0.13f);

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);

                stateMachine.SwitchToHit(0.08f);
                stateMachine.CurrentState.HandleDodge();

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerHitState_TracksGuardBreakReactionUntilHitEnds()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "stateMachine", stateMachine);

                stateMachine.Initialize(player);
                stateMachine.SwitchToHit(0.08f, PlayerHitReactionType.GuardBreak);

                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerHitReactionType.GuardBreak, stateMachine.CurrentHitReactionType);

                stateMachine.Tick(0.13f);

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerHitReactionType.Standard, stateMachine.CurrentHitReactionType);

                stateMachine.SwitchToHit(0.08f);

                Assert.AreEqual(PlayerHitReactionType.Standard, stateMachine.CurrentHitReactionType);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerHitState_GuardBreakHonorsConfiguredStunBeyondStandardHitCap()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "stateMachine", stateMachine);

                stateMachine.Initialize(player);
                stateMachine.SwitchToHit(0.16f, PlayerHitReactionType.GuardBreak);

                stateMachine.Tick(0.13f);

                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerHitReactionType.GuardBreak, stateMachine.CurrentHitReactionType);

                stateMachine.Tick(0.04f);

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerHitReactionType.Standard, stateMachine.CurrentHitReactionType);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerHitState_GuardBreakLocksMovementAndImmediateCancels()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "stateMachine", stateMachine);

                stateMachine.Initialize(player);
                stateMachine.SwitchToHit(0.16f, PlayerHitReactionType.GuardBreak);

                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.IsFalse(stateMachine.AllowsMovement);
                Assert.IsFalse(stateMachine.AllowsJump);

                stateMachine.CurrentState.HandleDodge();
                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);

                stateMachine.CurrentState.HandleLightAttack();
                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);

                stateMachine.CurrentState.HandleHeavyAttack();
                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);

                stateMachine.Tick(0.17f);

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsTrue(stateMachine.AllowsMovement);
                Assert.IsTrue(stateMachine.AllowsJump);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerAttackState_AllowsGroundMovement_ButKeepsJumpLocked()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.IsTrue(stateMachine.AllowsMovement);
                Assert.IsFalse(stateMachine.AllowsJump);
                Assert.AreEqual(0.62f, stateMachine.MovementSpeedScale, 0.001f);
                Assert.AreSame(lightAttack, combatController.CurrentAttackDefinition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerAttackState_ClearsCurrentAttack_WhenInterrupted()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                Assert.AreSame(lightAttack, combatController.CurrentAttackDefinition);

                stateMachine.SwitchToHit(0.08f);

                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerAttackState_LightInputBeforeQueueWindow_DoesNotAutoChain()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack1 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO lightAttack2 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack1);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(combatController, "balance", balance);
                SetPrivateField(combatController, "lightAttackCombo", new[] { lightAttack1, lightAttack2 });
                SetPrivateField(balance, "inputBufferSeconds", 0.2f);
                ConfigureAttackTiming(lightAttack1, 0.1f, 0.1f, 0.2f);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                stateMachine.Tick(0.05f);

                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                for (int i = 0; i < 7; i++)
                {
                    stateMachine.Tick(0.05f);
                }

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(balance);
                UnityEngine.Object.DestroyImmediate(lightAttack2);
                UnityEngine.Object.DestroyImmediate(lightAttack1);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerAttackState_LightInputInsideQueueWindow_ChainsAtAttackEnd()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack1 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO lightAttack2 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            CombatBalanceSO balance = ScriptableObject.CreateInstance<CombatBalanceSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack1);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(combatController, "balance", balance);
                SetPrivateField(combatController, "lightAttackCombo", new[] { lightAttack1, lightAttack2 });
                SetPrivateField(balance, "inputBufferSeconds", 0.2f);
                ConfigureAttackTiming(lightAttack1, 0.1f, 0.1f, 0.2f);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);

                for (int i = 0; i < 5; i++)
                {
                    stateMachine.Tick(0.05f);
                }

                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                for (int i = 0; i < 2; i++)
                {
                    stateMachine.Tick(0.05f);
                }

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(lightAttack2, combatController.CurrentAttackDefinition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(balance);
                UnityEngine.Object.DestroyImmediate(lightAttack2);
                UnityEngine.Object.DestroyImmediate(lightAttack1);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerCharacter_ConsumesJumpInput_WhenCurrentStateDisallowsJump()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                InputReader inputReader = gameObject.AddComponent<InputReader>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "inputReader", inputReader);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                InvokePrivateMethod(player, "QueueJump");
                Assert.IsTrue(GetPrivateField<bool>(player, "jumpQueued"));

                InvokePrivateMethod(player, "Update");

                Assert.IsFalse(GetPrivateField<bool>(player, "jumpQueued"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerDeathState_BlocksActiveStateTransitions_UntilRestore()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);

                stateMachine.Initialize(player);
                stateMachine.SwitchToDeath();
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);

                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);

                stateMachine.SwitchToDodge();
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);

                stateMachine.SwitchToAirDodge();
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);

                stateMachine.SwitchToSkill(0);
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);

                stateMachine.SwitchToHit(0.08f);
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);

                stateMachine.SwitchToLocomotion();
                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_GroundedDodgeInput_EntersDodge()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "stateMachine", stateMachine);

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnDodgePressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirborneDodgeInput_EntersOneAirDodgePerAirtime()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnDodgePressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerEvasiveActionType.AirDodge, stateMachine.CurrentEvasiveActionType);

                stateMachine.SwitchToLocomotion();
                InvokePrivateMethod(stateMachine, "OnDodgePressed");

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_BlockDodgeInput_CombatRollsOutOfGuard()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "stateMachine", stateMachine);

                stateMachine.Initialize(player);
                stateMachine.SwitchToBlock();
                InvokePrivateMethod(stateMachine, "OnDodgePressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.AreEqual(PlayerEvasiveActionType.CombatRoll, stateMachine.CurrentEvasiveActionType);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_CombatRollLightInput_WaitsForRecoveryThenStartsLight()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO dodgeFollowUpAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(combatController, "dodgeFollowUpAttack", dodgeFollowUpAttack);

                stateMachine.Initialize(player);
                stateMachine.SwitchToDodge(PlayerEvasiveActionType.CombatRoll);
                combatController.OpenDodgeFollowUpWindow(0.8f);
                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);

                stateMachine.Tick(0.2f);

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);

                stateMachine.Tick(0.3f);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(lightAttack, combatController.CurrentAttackDefinition);
                Assert.AreNotSame(dodgeFollowUpAttack, combatController.CurrentAttackDefinition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dodgeFollowUpAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_CombatRollSideLightInput_QueuesSidewindAfterRecovery()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(sidewindCut, "artId", "Sidewind_Cut");
                SetPrivateField(sidewindCut, "displayName", "Sidewind Cut");
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left | SwordArtDirectionMask.Right);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(combatController, "swordArts", new[] { sidewindCut });

                stateMachine.Initialize(player);
                stateMachine.SwitchToDodge(PlayerEvasiveActionType.CombatRoll);
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge);
                stateMachine.CurrentState.HandleLightAttack();

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);

                stateMachine.Tick(0.5f);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(sidewindAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(sidewindCut, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sidewindCut);
                UnityEngine.Object.DestroyImmediate(sidewindAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_CombatRollLightInput_QueuesCrossStepAfterRecovery()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO crossStepAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();
            SwordArtDefinitionSO crossStep = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(sidewindCut, "artId", "Sidewind_Cut");
                SetPrivateField(sidewindCut, "displayName", "Sidewind Cut");
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left | SwordArtDirectionMask.Right);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(crossStep, "artId", "Cross_Step");
                SetPrivateField(crossStep, "displayName", "Cross Step");
                SetPrivateField(crossStep, "attackDefinition", crossStepAttack);
                SetPrivateField(crossStep, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(crossStep, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(crossStep, "requiredContextTags", SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterCombatRoll);
                SetPrivateField(crossStep, "triggerWindowSeconds", 0.3f);
                SetPrivateField(combatController, "swordArts", new[] { sidewindCut, crossStep });

                stateMachine.Initialize(player);
                stateMachine.SwitchToDodge(PlayerEvasiveActionType.CombatRoll);
                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);

                stateMachine.Tick(0.5f);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(crossStepAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(crossStep, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(crossStep);
                UnityEngine.Object.DestroyImmediate(sidewindCut);
                UnityEngine.Object.DestroyImmediate(crossStepAttack);
                UnityEngine.Object.DestroyImmediate(sidewindAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerMotor_ApplyActionVerticalVelocity_LiftsAirDodgeWithoutLoweringHigherVelocity()
        {
            GameObject gameObject = new GameObject("Player");

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();

                motor.ApplyActionVerticalVelocity(3.2f);
                Assert.AreEqual(3.2f, motor.VerticalVelocity, 0.001f);

                motor.ApplyActionVerticalVelocity(1.4f);
                Assert.AreEqual(3.2f, motor.VerticalVelocity, 0.001f);

                motor.ApplyActionVerticalVelocity(1.4f, onlyIfHigher: false);
                Assert.AreEqual(1.4f, motor.VerticalVelocity, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirborneSkillInput_RemainsAllowedAsCommittedAction()
        {
            GameObject gameObject = new GameObject("Player");
            SkillDefinitionSO skillDefinition = ScriptableObject.CreateInstance<SkillDefinitionSO>();

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                ManaComponent mana = gameObject.AddComponent<ManaComponent>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                SkillController skillController = gameObject.AddComponent<SkillController>();
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "skillController", skillController);
                SetPrivateField(skillController, "skill1", skillDefinition);
                SetPrivateField(skillDefinition, "manaCost", 20f);
                SetPrivateField(skillDefinition, "cooldownSeconds", 2f);
                SetPrivateField(skillDefinition, "castDurationSeconds", 0.1f);
                SetPrivateField(skillDefinition, "allowsMovementDuringCast", true);
                SetPrivateField(skillDefinition, "movementSpeedScale", 0.5f);
                InvokePrivateMethod(skillController, "Awake");
                mana.SetMax(100f, refillCurrent: true);

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnSkill1Pressed");

                Assert.IsInstanceOf<PlayerSkillState>(stateMachine.CurrentState);
                Assert.IsTrue(skillController.HasPendingCast);
                Assert.IsTrue(stateMachine.AllowsMovement);
                Assert.IsFalse(stateMachine.AllowsJump);

                stateMachine.Tick(0.11f);

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsFalse(skillController.HasPendingCast);
                Assert.AreEqual(80f, mana.CurrentValue, 0.001f);
                Assert.AreEqual(2f, skillController.GetRemainingCooldown(0), 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(skillDefinition);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerAttackState_ConsumesBufferedSwordArt_WhenAttackStarts()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO sidewindAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO sidewindCut = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(sidewindCut, "artId", "Sidewind_Cut");
                SetPrivateField(sidewindCut, "displayName", "Sidewind Cut");
                SetPrivateField(sidewindCut, "attackDefinition", sidewindAttack);
                SetPrivateField(sidewindCut, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(sidewindCut, "acceptedDirections", SwordArtDirectionMask.Left | SwordArtDirectionMask.Right);
                SetPrivateField(sidewindCut, "requiredContextTags", SwordArtContextTags.AfterDodge);
                SetPrivateField(sidewindCut, "triggerWindowSeconds", 0.25f);
                SetPrivateField(combatController, "swordArts", new[] { sidewindCut });
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(sidewindAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(sidewindCut, combatController.CurrentSwordArt);
                Assert.AreSame(sidewindAttack, combatController.CurrentSwordArtAttack);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);

                stateMachine.SwitchToLocomotion();

                Assert.IsFalse(combatController.HasCurrentSwordArt);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sidewindCut);
                UnityEngine.Object.DestroyImmediate(sidewindAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_CounterWindowHeavyInput_ExecutesSwordArt()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO gateBreakAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO gateBreak = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(gateBreak, "artId", "Iron_Gate_Break");
                SetPrivateField(gateBreak, "displayName", "Iron Gate Break");
                SetPrivateField(gateBreak, "attackDefinition", gateBreakAttack);
                SetPrivateField(gateBreak, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(gateBreak, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(gateBreak, "anyContextTags", SwordArtContextTags.AfterBlock | SwordArtContextTags.AfterHeavy);
                SetPrivateField(gateBreak, "triggerWindowSeconds", 0.35f);
                SetPrivateField(combatController, "swordArts", new[] { gateBreak });
                combatController.OpenCounterWindow(0.8f);

                stateMachine.Initialize(player);
                stateMachine.SwitchToBlock();
                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.IsTrue(combatController.HasSwordArtPreview);
                Assert.AreSame(gateBreak, combatController.PreviewSwordArt);
                Assert.AreSame(gateBreakAttack, combatController.PreviewSwordArtAttack);
                Assert.AreSame(gateBreakAttack, combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gateBreak);
                UnityEngine.Object.DestroyImmediate(gateBreakAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirborneHeavyInput_ExecutesRisingCleaveSwordArt()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO risingAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO risingCleave = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(heavyAttack, "startupSeconds", 0.1f);
                SetPrivateField(heavyAttack, "activeSeconds", 0.1f);
                SetPrivateField(heavyAttack, "recoverySeconds", 0.2f);
                SetPrivateField(combatController, "heavyAttack", heavyAttack);
                SetPrivateField(risingCleave, "artId", "Rising_Cleave");
                SetPrivateField(risingCleave, "displayName", "Rising Cleave");
                SetPrivateField(risingCleave, "attackDefinition", risingAttack);
                SetPrivateField(risingCleave, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(risingCleave, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(risingCleave, "anyContextTags", SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne);
                SetPrivateField(risingCleave, "triggerWindowSeconds", 0.25f);
                SetPrivateField(combatController, "swordArts", new[] { risingCleave });

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.IsTrue(combatController.HasSwordArtPreview);
                Assert.AreSame(risingCleave, combatController.PreviewSwordArt);
                Assert.AreSame(risingAttack, combatController.PreviewSwordArtAttack);
                Assert.AreSame(risingAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(risingCleave, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(risingCleave);
                UnityEngine.Object.DestroyImmediate(risingAttack);
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirborneNeutralHeavyInput_PrefersFallingStarSwordArt()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO risingAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO fallingAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO risingCleave = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();
            SwordArtDefinitionSO fallingStar = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(heavyAttack, "startupSeconds", 0.1f);
                SetPrivateField(heavyAttack, "activeSeconds", 0.1f);
                SetPrivateField(heavyAttack, "recoverySeconds", 0.2f);
                SetPrivateField(combatController, "heavyAttack", heavyAttack);
                SetPrivateField(risingCleave, "artId", "Rising_Cleave");
                SetPrivateField(risingCleave, "displayName", "Rising Cleave");
                SetPrivateField(risingCleave, "attackDefinition", risingAttack);
                SetPrivateField(risingCleave, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(risingCleave, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(risingCleave, "anyContextTags", SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne);
                SetPrivateField(risingCleave, "triggerWindowSeconds", 0.3f);
                SetPrivateField(fallingStar, "artId", "Falling_Star");
                SetPrivateField(fallingStar, "displayName", "Falling Star");
                SetPrivateField(fallingStar, "attackDefinition", fallingAttack);
                SetPrivateField(fallingStar, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(fallingStar, "acceptedDirections", SwordArtDirectionMask.Neutral | SwordArtDirectionMask.Backward);
                SetPrivateField(fallingStar, "requiredContextTags", SwordArtContextTags.Airborne);
                SetPrivateField(fallingStar, "triggerWindowSeconds", 0.32f);
                SetPrivateField(combatController, "swordArts", new[] { risingCleave, fallingStar });

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.IsTrue(combatController.HasSwordArtPreview);
                Assert.AreSame(fallingStar, combatController.PreviewSwordArt);
                Assert.AreSame(fallingAttack, combatController.PreviewSwordArtAttack);
                Assert.AreSame(fallingAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(fallingStar, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fallingStar);
                UnityEngine.Object.DestroyImmediate(risingCleave);
                UnityEngine.Object.DestroyImmediate(fallingAttack);
                UnityEngine.Object.DestroyImmediate(risingAttack);
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirDodgeHeavyInput_QueuesFallingStarAfterRecovery()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO fallingAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO fallingStar = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                gameObject.transform.position = Vector3.up * 3f;
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(combatController, "heavyAttack", heavyAttack);
                SetPrivateField(fallingStar, "artId", "Falling_Star");
                SetPrivateField(fallingStar, "displayName", "Falling Star");
                SetPrivateField(fallingStar, "attackDefinition", fallingAttack);
                SetPrivateField(fallingStar, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(fallingStar, "acceptedDirections", SwordArtDirectionMask.Neutral | SwordArtDirectionMask.Backward);
                SetPrivateField(fallingStar, "requiredContextTags", SwordArtContextTags.Airborne);
                SetPrivateField(fallingStar, "triggerWindowSeconds", 0.1f);
                SetPrivateField(combatController, "swordArts", new[] { fallingStar });

                stateMachine.Initialize(player);
                stateMachine.SwitchToAirDodge();
                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);

                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.IsTrue(combatController.HasSwordArtPreview);
                Assert.AreSame(fallingStar, combatController.PreviewSwordArt);

                stateMachine.Tick(0.35f);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(fallingAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(fallingStar, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fallingStar);
                UnityEngine.Object.DestroyImmediate(fallingAttack);
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirDodgeForwardHeavyInput_QueuesRisingCleaveAfterRecovery()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO risingAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO fallingAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO risingCleave = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();
            SwordArtDefinitionSO fallingStar = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                gameObject.transform.position = Vector3.up * 3f;
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(combatController, "heavyAttack", heavyAttack);
                SetPrivateField(risingCleave, "artId", "Rising_Cleave");
                SetPrivateField(risingCleave, "displayName", "Rising Cleave");
                SetPrivateField(risingCleave, "attackDefinition", risingAttack);
                SetPrivateField(risingCleave, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(risingCleave, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(risingCleave, "anyContextTags", SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne);
                SetPrivateField(risingCleave, "triggerWindowSeconds", 0.1f);
                SetPrivateField(fallingStar, "artId", "Falling_Star");
                SetPrivateField(fallingStar, "displayName", "Falling Star");
                SetPrivateField(fallingStar, "attackDefinition", fallingAttack);
                SetPrivateField(fallingStar, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(fallingStar, "acceptedDirections", SwordArtDirectionMask.Neutral | SwordArtDirectionMask.Backward);
                SetPrivateField(fallingStar, "requiredContextTags", SwordArtContextTags.Airborne);
                SetPrivateField(fallingStar, "triggerWindowSeconds", 0.1f);
                SetPrivateField(combatController, "swordArts", new[] { risingCleave, fallingStar });

                stateMachine.Initialize(player);
                stateMachine.SwitchToAirDodge();
                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);

                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Forward,
                    SwordArtContextTags.Airborne);
                stateMachine.CurrentState.HandleHeavyAttack();
                stateMachine.Tick(0.35f);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(risingAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(risingCleave, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fallingStar);
                UnityEngine.Object.DestroyImmediate(risingCleave);
                UnityEngine.Object.DestroyImmediate(fallingAttack);
                UnityEngine.Object.DestroyImmediate(risingAttack);
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirDodgeLightInput_QueuesMoonSeverAfterRecovery()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO moonSeverAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO moonSever = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                gameObject.transform.position = Vector3.up * 3f;
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(moonSever, "artId", "Moon_Sever");
                SetPrivateField(moonSever, "displayName", "Moon Sever");
                SetPrivateField(moonSever, "attackDefinition", moonSeverAttack);
                SetPrivateField(moonSever, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(moonSever, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(
                    moonSever,
                    "requiredContextTags",
                    SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge);
                SetPrivateField(moonSever, "triggerWindowSeconds", 0.28f);
                SetPrivateField(combatController, "swordArts", new[] { moonSever });

                stateMachine.Initialize(player);
                stateMachine.SwitchToAirDodge();
                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);

                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.IsTrue(combatController.HasSwordArtPreview);
                Assert.AreSame(moonSever, combatController.PreviewSwordArt);

                stateMachine.Tick(0.35f);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(moonSeverAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(moonSever, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(moonSever);
                UnityEngine.Object.DestroyImmediate(moonSeverAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirDodgeLightInput_FallsBackToLightAttack_WhenMoonSeverCannotBeAfforded()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO moonSeverAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO moonSever = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                gameObject.transform.position = Vector3.up * 3f;
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                ManaComponent mana = gameObject.AddComponent<ManaComponent>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "mana", mana);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(moonSever, "artId", "Moon_Sever");
                SetPrivateField(moonSever, "displayName", "Moon Sever");
                SetPrivateField(moonSever, "attackDefinition", moonSeverAttack);
                SetPrivateField(moonSever, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(moonSever, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(
                    moonSever,
                    "requiredContextTags",
                    SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge);
                SetPrivateField(moonSever, "triggerWindowSeconds", 0.28f);
                SetPrivateField(moonSever, "resourceCost", 12f);
                SetPrivateField(combatController, "swordArts", new[] { moonSever });
                mana.SetMax(100f, refillCurrent: true);
                mana.SetCurrent(5f);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAirDodge();
                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);

                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);
                Assert.IsTrue(combatController.HasSwordArtPreview);
                Assert.AreSame(moonSever, combatController.PreviewSwordArt);

                stateMachine.Tick(0.35f);

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(lightAttack, combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasCurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
                Assert.AreEqual(5f, mana.CurrentValue, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(moonSever);
                UnityEngine.Object.DestroyImmediate(moonSeverAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirDodgeHeavyInput_WithoutSwordArtReturnsToLocomotion()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                gameObject.transform.position = Vector3.up * 3f;
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(combatController, "heavyAttack", heavyAttack);

                stateMachine.Initialize(player);
                stateMachine.SwitchToAirDodge();
                Assert.IsInstanceOf<PlayerDodgeState>(stateMachine.CurrentState);

                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");
                stateMachine.Tick(0.35f);

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirborneBaseAttackInput_RequiresExecutableSwordArt()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(heavyAttack, "startupSeconds", 0.1f);
                SetPrivateField(heavyAttack, "activeSeconds", 0.1f);
                SetPrivateField(heavyAttack, "recoverySeconds", 0.2f);
                SetPrivateField(combatController, "heavyAttack", heavyAttack);

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);

                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsNull(combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_AirborneLightInput_CanExecuteMatchingSwordArt()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO aerialAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO aerialSlash = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                gameObject.AddComponent<CharacterController>();
                PlayerMotor motor = gameObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(aerialSlash, "artId", "Aerial_Slash");
                SetPrivateField(aerialSlash, "displayName", "Aerial Slash");
                SetPrivateField(aerialSlash, "attackDefinition", aerialAttack);
                SetPrivateField(aerialSlash, "triggerAction", SwordArtTriggerAction.LightAttack);
                SetPrivateField(aerialSlash, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(aerialSlash, "anyContextTags", SwordArtContextTags.Airborne);
                SetPrivateField(aerialSlash, "triggerWindowSeconds", 0.25f);
                SetPrivateField(combatController, "swordArts", new[] { aerialSlash });

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnLightAttackPressed");

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(aerialAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasCurrentSwordArt);
                Assert.AreSame(aerialSlash, combatController.CurrentSwordArt);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aerialSlash);
                UnityEngine.Object.DestroyImmediate(aerialAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerStateMachine_GroundedNeutralHeavyInput_KeepsBaseHeavyWhenRisingCleaveRequiresAirborneOrForward()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO risingAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO risingCleave = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(heavyAttack, "startupSeconds", 0.1f);
                SetPrivateField(heavyAttack, "activeSeconds", 0.1f);
                SetPrivateField(heavyAttack, "recoverySeconds", 0.2f);
                SetPrivateField(combatController, "heavyAttack", heavyAttack);
                SetPrivateField(risingCleave, "artId", "Rising_Cleave");
                SetPrivateField(risingCleave, "displayName", "Rising Cleave");
                SetPrivateField(risingCleave, "attackDefinition", risingAttack);
                SetPrivateField(risingCleave, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(risingCleave, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(risingCleave, "anyContextTags", SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne);
                SetPrivateField(risingCleave, "triggerWindowSeconds", 0.25f);
                SetPrivateField(combatController, "swordArts", new[] { risingCleave });

                stateMachine.Initialize(player);
                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.IsFalse(combatController.HasSwordArtPreview);
                Assert.AreSame(heavyAttack, combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasCurrentSwordArt);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(risingCleave);
                UnityEngine.Object.DestroyImmediate(risingAttack);
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerAttackState_HeavyInput_WaitsForSwordArtCancelWindow()
        {
            GameObject gameObject = new GameObject("Player");
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO heavyAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO gateBreakAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO gateBreak = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCombatController combatController = BuildCombatController(gameObject, lightAttack);
                SetPrivateField(player, "stateMachine", stateMachine);
                SetPrivateField(player, "combatController", combatController);
                SetPrivateField(heavyAttack, "animationStateName", "Heavy_01");
                SetPrivateField(heavyAttack, "startupSeconds", 0.1f);
                SetPrivateField(heavyAttack, "activeSeconds", 0.1f);
                SetPrivateField(heavyAttack, "recoverySeconds", 0.6f);
                SetPrivateField(combatController, "heavyAttack", heavyAttack);
                SetPrivateField(gateBreak, "artId", "Iron_Gate_Break");
                SetPrivateField(gateBreak, "displayName", "Iron Gate Break");
                SetPrivateField(gateBreak, "attackDefinition", gateBreakAttack);
                SetPrivateField(gateBreak, "triggerAction", SwordArtTriggerAction.HeavyAttack);
                SetPrivateField(gateBreak, "acceptedDirections", SwordArtDirectionMask.Any);
                SetPrivateField(gateBreak, "anyContextTags", SwordArtContextTags.AfterHeavy);
                SetPrivateField(gateBreak, "triggerWindowSeconds", 0.35f);
                SetPrivateField(gateBreak, "cancelWindowSeconds", 0.25f);
                SetPrivateField(combatController, "swordArts", new[] { gateBreak });

                stateMachine.Initialize(player);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
                Assert.AreSame(heavyAttack, combatController.CurrentAttackDefinition);

                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(heavyAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasBufferedSwordArtCommand);

                for (int i = 0; i < 11; i++)
                {
                    stateMachine.Tick(0.05f);
                }

                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.AreSame(gateBreakAttack, combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gateBreak);
                UnityEngine.Object.DestroyImmediate(gateBreakAttack);
                UnityEngine.Object.DestroyImmediate(heavyAttack);
                UnityEngine.Object.DestroyImmediate(lightAttack);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static PlayerCombatController BuildCombatController(GameObject gameObject, AttackDefinitionSO lightAttack)
        {
            AttackExecutor attackExecutor = gameObject.AddComponent<AttackExecutor>();
            HitboxController hitboxController = gameObject.AddComponent<HitboxController>();
            PlayerCombatController combatController = gameObject.AddComponent<PlayerCombatController>();
            ManaComponent mana = gameObject.GetComponent<ManaComponent>();
            SetPrivateField(hitboxController, "attackExecutor", attackExecutor);
            SetPrivateField(combatController, "attackExecutor", attackExecutor);
            SetPrivateField(combatController, "hitboxController", hitboxController);
            SetPrivateField(combatController, "mana", mana);
            SetPrivateField(combatController, "lightAttackCombo", new[] { lightAttack });
            SetPrivateField(lightAttack, "startupSeconds", 0.1f);
            SetPrivateField(lightAttack, "activeSeconds", 0.1f);
            SetPrivateField(lightAttack, "recoverySeconds", 0.1f);
            SetPrivateField(lightAttack, "forwardMovement", 0f);
            SetPrivateField(lightAttack, "movementSpeedScale", 0.62f);
            return combatController;
        }

        private static void ConfigureAttackTiming(
            AttackDefinitionSO attack,
            float startupSeconds,
            float activeSeconds,
            float recoverySeconds)
        {
            SetPrivateField(attack, "startupSeconds", startupSeconds);
            SetPrivateField(attack, "activeSeconds", activeSeconds);
            SetPrivateField(attack, "recoverySeconds", recoverySeconds);
        }

        private static int GetEventSubscriberCount(object instance, string eventFieldName)
        {
            FieldInfo field = instance.GetType().GetField(eventFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, eventFieldName);
            MulticastDelegate multicastDelegate = field.GetValue(instance) as MulticastDelegate;
            return multicastDelegate?.GetInvocationList().Length ?? 0;
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

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
        }
    }
}
