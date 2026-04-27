using UnityEngine;

namespace CampusRPG.Character
{
    public enum PlayerHitReactionType
    {
        Standard,
        GuardBreak
    }

    public sealed class PlayerHitState : PlayerState
    {
        private const float MinimumHitFeedbackSeconds = 0.04f;
        private const float MaximumStandardHitFeedbackSeconds = 0.12f;
        private const float MinimumGuardBreakFeedbackSeconds = 0.10f;
        private const float MaximumGuardBreakFeedbackSeconds = 0.24f;

        private readonly PlayerStateMachine stateMachine;
        private float remainingTime;

        public PlayerHitState(PlayerCharacter owner, PlayerStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override bool AllowsMovement => ReactionType != PlayerHitReactionType.GuardBreak;

        public override bool AllowsJump => ReactionType != PlayerHitReactionType.GuardBreak;

        public PlayerHitReactionType ReactionType { get; private set; } = PlayerHitReactionType.Standard;

        public void SetDuration(float duration, PlayerHitReactionType reactionType = PlayerHitReactionType.Standard)
        {
            ReactionType = reactionType;
            float minimumDuration = reactionType == PlayerHitReactionType.GuardBreak
                ? MinimumGuardBreakFeedbackSeconds
                : MinimumHitFeedbackSeconds;
            float maximumDuration = reactionType == PlayerHitReactionType.GuardBreak
                ? MaximumGuardBreakFeedbackSeconds
                : MaximumStandardHitFeedbackSeconds;
            remainingTime = Mathf.Clamp(duration, minimumDuration, maximumDuration);
        }

        public override void Tick(float deltaTime)
        {
            if (ReactionType != PlayerHitReactionType.GuardBreak
                && Owner.InputReader != null
                && Owner.InputReader.IsBlockHeld)
            {
                stateMachine.SwitchToBlock();
                return;
            }

            remainingTime -= deltaTime;

            if (remainingTime <= 0f)
            {
                stateMachine.SwitchToLocomotion();
            }
        }

        public override void HandleLightAttack()
        {
            if (ReactionType == PlayerHitReactionType.GuardBreak)
            {
                return;
            }

            if (Owner.CombatController != null && Owner.CombatController.HasDodgeFollowUpWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.DodgeFollowUp);
                return;
            }

            stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
        }

        public override void HandleHeavyAttack()
        {
            if (ReactionType == PlayerHitReactionType.GuardBreak)
            {
                return;
            }

            if (Owner.CombatController != null && Owner.CombatController.HasCounterWindow)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Counter);
                return;
            }

            stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
        }

        public override void HandleDodge()
        {
            if (ReactionType == PlayerHitReactionType.GuardBreak)
            {
                return;
            }

            stateMachine.SwitchToDodge();
        }

        public override void HandleSkill1()
        {
            if (ReactionType == PlayerHitReactionType.GuardBreak)
            {
                return;
            }

            stateMachine.SwitchToSkill(0);
        }

        public override void HandleSkill2()
        {
            if (ReactionType == PlayerHitReactionType.GuardBreak)
            {
                return;
            }

            stateMachine.SwitchToSkill(1);
        }
    }
}
