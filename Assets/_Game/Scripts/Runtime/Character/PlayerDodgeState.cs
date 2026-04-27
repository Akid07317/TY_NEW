using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    public enum PlayerEvasiveActionType
    {
        GroundDodge = 0,
        CombatRoll = 1,
        AirDodge = 2
    }

    public sealed class PlayerDodgeState : PlayerState
    {
        private readonly PlayerStateMachine stateMachine;
        private PlayerEvasiveActionType actionType;
        private float remainingTime;
        private float invulnerableStartupRemaining;
        private float invulnerableRemaining;
        private bool hasRegisteredSuccessfulDodge;
        private bool hasQueuedAirDodgeSwordArtFollowUp;
        private SwordArtCommand queuedAirDodgeSwordArtCommand;
        private bool hasQueuedCombatRollLightFollowUp;
        private bool hasQueuedCombatRollSwordArtFollowUp;
        private SwordArtCommand queuedCombatRollSwordArtCommand;

        public PlayerDodgeState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public bool IsInvulnerable => invulnerableStartupRemaining <= 0f && invulnerableRemaining > 0f;

        public PlayerEvasiveActionType ActionType => actionType;

        public void Configure(PlayerEvasiveActionType nextActionType)
        {
            actionType = nextActionType;
        }

        public override void Enter()
        {
            Combat.CombatBalanceSO balance = Owner.CombatController != null ? Owner.CombatController.Balance : null;
            float gameplayDuration = ResolveDurationSeconds(balance);
            float animationDuration = stateMachine.AnimationRelay != null
                ? stateMachine.AnimationRelay.ResolveEvasiveAnimationDurationSeconds(actionType)
                : 0f;
            float actionDuration = Mathf.Max(gameplayDuration, animationDuration);

            remainingTime = actionDuration;
            invulnerableStartupRemaining = ResolveInvulnerableStartupSeconds(balance);
            invulnerableRemaining = ResolveInvulnerableSeconds(balance);
            hasRegisteredSuccessfulDodge = false;
            hasQueuedAirDodgeSwordArtFollowUp = false;
            queuedAirDodgeSwordArtCommand = default;
            hasQueuedCombatRollLightFollowUp = false;
            hasQueuedCombatRollSwordArtFollowUp = false;
            queuedCombatRollSwordArtCommand = default;
            float dodgeDistance = ResolveDistance(balance);

            if (PlayerMovementRuntimeUtility.TryResolveDodgeDirection(
                    Owner.transform,
                    Owner.InputReader != null ? Owner.InputReader.MoveValue : Vector2.zero,
                    Owner.CameraTransform,
                    Owner.LockOnTargetSelector != null ? Owner.LockOnTargetSelector.CurrentTarget : null,
                    out Vector3 dodgeDirection,
                    out bool faceLockTarget))
            {
                dodgeDistance *= PlayerMovementRuntimeUtility.ResolveDodgeDistanceMultiplier(
                    Owner.transform,
                    dodgeDirection,
                    balance != null ? balance.DodgeBackwardDistanceScale : 0.88f);
                Owner.Motor?.BeginDirectionalDodge(dodgeDirection, dodgeDistance, actionDuration, faceLockTarget);
            }

            if (actionType == PlayerEvasiveActionType.AirDodge)
            {
                Owner.Motor?.ApplyActionVerticalVelocity(ResolveAirDodgeVerticalVelocity(balance));
            }

            Owner.CombatController?.HandleDodgeStarted();
        }

        public override void Tick(float deltaTime)
        {
            float tickDelta = Mathf.Max(0f, deltaTime);
            remainingTime -= tickDelta;

            if (invulnerableStartupRemaining > 0f)
            {
                float startupDelta = Mathf.Min(invulnerableStartupRemaining, tickDelta);
                invulnerableStartupRemaining -= startupDelta;
                tickDelta -= startupDelta;
            }

            if (invulnerableStartupRemaining <= 0f)
            {
                invulnerableRemaining = Mathf.Max(0f, invulnerableRemaining - tickDelta);
            }

            if (remainingTime <= 0f)
            {
                if (TryStartQueuedCombatRollLightFollowUp())
                {
                    return;
                }

                if (TryStartQueuedAirDodgeSwordArtFollowUp())
                {
                    return;
                }

                stateMachine.SwitchToLocomotion();
            }
        }

        public override void HandleLightAttack()
        {
            if (actionType == PlayerEvasiveActionType.CombatRoll)
            {
                QueueCombatRollLightFollowUp();
                return;
            }

            if (actionType == PlayerEvasiveActionType.AirDodge)
            {
                QueueAirDodgeSwordArtFollowUp(SwordArtTriggerAction.LightAttack);
                return;
            }

            if (Owner.CombatController != null && Owner.CombatController.HasDodgeFollowUpWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.DodgeFollowUp);
            }
        }

        public override void HandleHeavyAttack()
        {
            QueueAirDodgeSwordArtFollowUp(SwordArtTriggerAction.HeavyAttack);
        }

        public bool TryRegisterSuccessfulDodge()
        {
            if (!IsInvulnerable || hasRegisteredSuccessfulDodge)
            {
                return false;
            }

            hasRegisteredSuccessfulDodge = true;
            Owner.CombatController?.NotifySuccessfulDodge();
            return true;
        }

        private float ResolveDurationSeconds(Combat.CombatBalanceSO balance)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => balance != null ? balance.CombatRollDurationSeconds : 0.42f,
                PlayerEvasiveActionType.AirDodge => balance != null ? balance.AirDodgeDurationSeconds : 0.28f,
                _ => balance != null ? balance.DodgeDurationSeconds : 0.25f
            };
        }

        private float ResolveInvulnerableStartupSeconds(Combat.CombatBalanceSO balance)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => balance != null ? Mathf.Max(0f, balance.CombatRollInvulnerableStartupSeconds) : 0.08f,
                PlayerEvasiveActionType.AirDodge => balance != null ? Mathf.Max(0f, balance.AirDodgeInvulnerableStartupSeconds) : 0.03f,
                _ => balance != null ? Mathf.Max(0f, balance.DodgeInvulnerableStartupSeconds) : 0.04f
            };
        }

        private float ResolveInvulnerableSeconds(Combat.CombatBalanceSO balance)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => balance != null ? balance.CombatRollInvulnerableSeconds : 0.18f,
                PlayerEvasiveActionType.AirDodge => balance != null ? balance.AirDodgeInvulnerableSeconds : 0.16f,
                _ => balance != null ? balance.DodgeInvulnerableSeconds : 0.2f
            };
        }

        private float ResolveDistance(Combat.CombatBalanceSO balance)
        {
            return actionType switch
            {
                PlayerEvasiveActionType.CombatRoll => balance != null ? balance.CombatRollDistance : 3.6f,
                PlayerEvasiveActionType.AirDodge => balance != null ? balance.AirDodgeDistance : 2.35f,
                _ => balance != null ? balance.DodgeDistance : 2.8f
            };
        }

        private float ResolveAirDodgeVerticalVelocity(Combat.CombatBalanceSO balance)
        {
            return balance != null ? balance.AirDodgeVerticalVelocity : 3.2f;
        }

        private void QueueAirDodgeSwordArtFollowUp(SwordArtTriggerAction triggerAction)
        {
            hasQueuedAirDodgeSwordArtFollowUp = false;
            queuedAirDodgeSwordArtCommand = default;

            if (actionType != PlayerEvasiveActionType.AirDodge
                || Owner.CombatController == null
                || !Owner.CombatController.HasBufferedSwordArtCommand)
            {
                return;
            }

            SwordArtCommand command = Owner.CombatController.CurrentBufferedSwordArtCommand;

            if (command.TriggerAction != triggerAction
                || !Owner.CombatController.TryPreviewBufferedSwordArt(out _, out _))
            {
                return;
            }

            hasQueuedAirDodgeSwordArtFollowUp = true;
            queuedAirDodgeSwordArtCommand = command;
        }

        private bool TryStartQueuedAirDodgeSwordArtFollowUp()
        {
            if (!hasQueuedAirDodgeSwordArtFollowUp || Owner.CombatController == null)
            {
                return false;
            }

            SwordArtCommand command = queuedAirDodgeSwordArtCommand;
            Owner.CombatController.BufferSwordArtCommand(
                command.TriggerAction,
                command.Direction,
                command.ContextTags);
            hasQueuedAirDodgeSwordArtFollowUp = false;
            queuedAirDodgeSwordArtCommand = default;
            PlayerAttackRequest request = command.TriggerAction == SwordArtTriggerAction.LightAttack
                ? PlayerAttackRequest.Light
                : PlayerAttackRequest.Heavy;
            stateMachine.SwitchToAttack(request);
            return true;
        }

        private void QueueCombatRollLightFollowUp()
        {
            hasQueuedCombatRollLightFollowUp = actionType == PlayerEvasiveActionType.CombatRoll;
            hasQueuedCombatRollSwordArtFollowUp = false;
            queuedCombatRollSwordArtCommand = default;

            if (!hasQueuedCombatRollLightFollowUp
                || Owner.CombatController == null
                || !Owner.CombatController.HasBufferedSwordArtCommand)
            {
                return;
            }

            SwordArtCommand command = Owner.CombatController.CurrentBufferedSwordArtCommand;

            if (command.TriggerAction != SwordArtTriggerAction.LightAttack
                || !Owner.CombatController.TryPreviewBufferedSwordArt(out _, out _))
            {
                return;
            }

            hasQueuedCombatRollSwordArtFollowUp = true;
            queuedCombatRollSwordArtCommand = command;
        }

        private bool TryStartQueuedCombatRollLightFollowUp()
        {
            if (!hasQueuedCombatRollLightFollowUp)
            {
                return false;
            }

            if (hasQueuedCombatRollSwordArtFollowUp && Owner.CombatController != null)
            {
                Owner.CombatController.BufferSwordArtCommand(
                    queuedCombatRollSwordArtCommand.TriggerAction,
                    queuedCombatRollSwordArtCommand.Direction,
                    queuedCombatRollSwordArtCommand.ContextTags);
            }

            hasQueuedCombatRollLightFollowUp = false;
            hasQueuedCombatRollSwordArtFollowUp = false;
            queuedCombatRollSwordArtCommand = default;
            stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
            return true;
        }
    }
}
