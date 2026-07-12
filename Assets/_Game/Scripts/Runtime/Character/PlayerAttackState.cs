using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    public sealed class PlayerAttackState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAttackRequest request;

        private AttackDefinitionSO definition;
        private float startupRemaining;
        private float activeRemaining;
        private float recoveryRemaining;
        private float elapsedAttackSeconds;
        private float totalAttackDurationSeconds;
        private bool queuedNextLightAttack;
        private bool isActiveWindowStarted;
        private bool hasAppliedForwardMovement;
        private bool hasNotifiedAttackFinished;

        public PlayerAttackState(PlayerCharacter owner, PlayerStateMachine stateMachine, PlayerAttackRequest request) : base(owner)
        {
            this.stateMachine = stateMachine;
            this.request = request;
        }

        public override bool AllowsMovement => true;

        public override bool AllowsJump => false;

        public override float MovementSpeedScale => definition != null ? definition.MovementSpeedScale : 0.65f;

        public override void Enter()
        {
            definition = ResolveAttackDefinition();

            if (definition == null)
            {
                stateMachine.SwitchToLocomotion();
                return;
            }

            startupRemaining = definition.StartupSeconds;
            activeRemaining = definition.ActiveSeconds;
            recoveryRemaining = ResolveRecoverySeconds(definition);
            elapsedAttackSeconds = 0f;
            totalAttackDurationSeconds = startupRemaining + activeRemaining + recoveryRemaining;
            hasAppliedForwardMovement = false;
            hasNotifiedAttackFinished = false;

            if (Owner.CombatController?.HitboxController != null && Owner.BaseStats != null)
            {
                Owner.CombatController.HitboxController.Prepare(definition, Owner.BaseStats.Attack, Owner.gameObject);
            }

            Owner.CombatController?.NotifyAttackStarted(definition);
            Owner.CombatController?.NotifyAttackTiming(elapsedAttackSeconds, totalAttackDurationSeconds);

            if (startupRemaining <= 0f)
            {
                BeginActiveWindow();
            }
        }

        public override void Tick(float deltaTime)
        {
            float previousElapsedSeconds = elapsedAttackSeconds;
            elapsedAttackSeconds += Mathf.Max(0f, deltaTime);
            ApplyDistributedForwardMovement(previousElapsedSeconds, elapsedAttackSeconds);
            Owner.CombatController?.NotifyAttackTiming(elapsedAttackSeconds, totalAttackDurationSeconds);

            if (!isActiveWindowStarted)
            {
                startupRemaining -= deltaTime;

                if (startupRemaining > 0f)
                {
                    return;
                }

                BeginActiveWindow();
            }

            if (activeRemaining > 0f)
            {
                activeRemaining -= deltaTime;

                if (activeRemaining > 0f)
                {
                    return;
                }

                EndActiveWindow();
            }

            recoveryRemaining -= deltaTime;

            if (recoveryRemaining > 0f)
            {
                return;
            }

            Owner.CombatController?.HitboxController?.Clear();
            Owner.CombatController?.NotifyAttackFinished(request);
            hasNotifiedAttackFinished = true;

            if (queuedNextLightAttack && request == PlayerAttackRequest.Light)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                return;
            }

            stateMachine.SwitchToLocomotion();
        }

        public override void HandleLightAttack()
        {
            if (request == PlayerAttackRequest.Light
                && Owner.CombatController != null
                && Owner.CombatController.CanQueueNextLightAttack
                && IsInLightComboQueueWindow())
            {
                queuedNextLightAttack = true;
                return;
            }

            if (request == PlayerAttackRequest.DodgeFollowUp)
            {
                queuedNextLightAttack = false;
            }
        }

        public override void HandleHeavyAttack()
        {
            if (Owner.CombatController != null && Owner.CombatController.HasCounterWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Counter);
                return;
            }

            if (request == PlayerAttackRequest.Heavy
                && IsCurrentHeavyAttack()
                && CanChainBufferedSwordArt())
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
            }
        }

        public override void Exit()
        {
            Owner.CombatController?.HitboxController?.Clear();

            if (!hasNotifiedAttackFinished)
            {
                Owner.CombatController?.NotifyAttackCanceled();
            }
        }

        private void BeginActiveWindow()
        {
            isActiveWindowStarted = true;
            ApplyLegacyForwardMovement();
            Owner.CombatController?.HitboxController?.OpenActivationWindow();

            if (definition == null || definition.HitboxActivationMode == AttackHitboxActivationMode.AnimationEvent)
            {
                if (activeRemaining <= 0f)
                {
                    EndActiveWindow();
                }

                return;
            }

            Owner.CombatController?.HitboxController?.Activate();

            if (activeRemaining <= 0f)
            {
                EndActiveWindow();
            }
        }

        private void EndActiveWindow()
        {
            Owner.CombatController?.HitboxController?.CloseActivationWindow();
        }

        private void ApplyLegacyForwardMovement()
        {
            if (hasAppliedForwardMovement || definition == null)
            {
                return;
            }

            if (definition.UsesDistributedForwardMovement)
            {
                hasAppliedForwardMovement = true;
                return;
            }

            hasAppliedForwardMovement = true;
            Owner.Motor?.AdvanceFacingDirection(definition.ForwardMovement);
        }

        private void ApplyDistributedForwardMovement(float previousElapsedSeconds, float currentElapsedSeconds)
        {
            float deltaDistance = PlayerCombatRuntimeUtility.ResolveAttackForwardMovementDelta(
                definition,
                previousElapsedSeconds,
                currentElapsedSeconds);

            if (deltaDistance <= 0f)
            {
                return;
            }

            Owner.Motor?.AdvanceFacingDirection(deltaDistance);
        }

        private static float ResolveRecoverySeconds(AttackDefinitionSO attackDefinition)
        {
            return PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(attackDefinition);
        }

        private AttackDefinitionSO ResolveAttackDefinition()
        {
            if (Owner.CombatController == null)
            {
                return null;
            }

            if (Owner.CombatController.TryConsumeBufferedSwordArt(
                out SwordArtDefinitionSO swordArt,
                out AttackDefinitionSO swordArtAttack))
            {
                Owner.CombatController.NotifySwordArtStarted(swordArt, swordArtAttack);
                return swordArtAttack;
            }

            return Owner.CombatController.ResolveAttack(request);
        }

        private bool IsCurrentHeavyAttack()
        {
            return definition != null
                && definition.AnimationStateName.StartsWith("Heavy_", System.StringComparison.Ordinal);
        }

        private bool CanChainBufferedSwordArt()
        {
            return Owner.CombatController != null
                && Owner.CombatController.TryPreviewBufferedSwordArt(out SwordArtDefinitionSO swordArt, out _)
                && IsInSwordArtCancelWindow(swordArt);
        }

        private bool IsInSwordArtCancelWindow(SwordArtDefinitionSO swordArt)
        {
            if (swordArt == null || swordArt.CancelWindowSeconds <= 0f)
            {
                return false;
            }

            if (totalAttackDurationSeconds <= 0f)
            {
                return true;
            }

            float windowStartSeconds = Mathf.Max(0f, totalAttackDurationSeconds - swordArt.CancelWindowSeconds);
            return elapsedAttackSeconds >= windowStartSeconds;
        }

        private bool IsInLightComboQueueWindow()
        {
            if (totalAttackDurationSeconds <= 0f)
            {
                return true;
            }

            float queueWindowSeconds = ResolveLightComboQueueWindowSeconds();
            float windowStartSeconds = Mathf.Max(0f, totalAttackDurationSeconds - queueWindowSeconds);
            return elapsedAttackSeconds >= windowStartSeconds;
        }

        private float ResolveLightComboQueueWindowSeconds()
        {
            CombatBalanceSO balance = Owner.CombatController != null ? Owner.CombatController.Balance : null;
            return balance != null ? Mathf.Max(0f, balance.InputBufferSeconds) : 0.2f;
        }
    }
}
