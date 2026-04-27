using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    public sealed class PlayerStateMachine : MonoBehaviour
    {
        private PlayerCharacter owner;
        private PlayerLocomotionState locomotionState;
        private PlayerBlockState blockState;
        private PlayerDodgeState dodgeState;
        private PlayerMantleState mantleState;
        private PlayerHitState hitState;
        private PlayerDeathState deathState;
        private PlayerCombatAnimationRelay animationRelay;
        private bool isInitialized;
        private bool isSubscribed;
        private bool hasConsumedAirDodge;

        public PlayerState CurrentState { get; private set; }

        public bool AllowsMovement => CurrentState == null || CurrentState.AllowsMovement;

        public bool AllowsJump => CurrentState == null || CurrentState.AllowsJump;

        public float MovementSpeedScale => CurrentState != null ? CurrentState.MovementSpeedScale : 1f;

        public bool IsBlocking => CurrentState is PlayerBlockState;

        public bool HasActiveGuard => CurrentState is PlayerBlockState blockState && blockState.IsGuardActive;

        public bool IsInvulnerable => CurrentState is PlayerDodgeState dodgeState && dodgeState.IsInvulnerable;

        public PlayerEvasiveActionType CurrentEvasiveActionType =>
            CurrentState is PlayerDodgeState dodgeState ? dodgeState.ActionType : PlayerEvasiveActionType.GroundDodge;

        public bool CanStartMantle => CurrentState is PlayerLocomotionState;

        public PlayerCombatAnimationRelay AnimationRelay => animationRelay;

        public PlayerHitReactionType CurrentHitReactionType =>
            CurrentState is PlayerHitState hitState ? hitState.ReactionType : PlayerHitReactionType.Standard;

        public void Initialize(PlayerCharacter player)
        {
            if (isInitialized)
            {
                Unsubscribe(owner);
            }

            owner = player;
            animationRelay = owner != null ? owner.GetComponent<PlayerCombatAnimationRelay>() : null;
            locomotionState = new PlayerLocomotionState(owner, this);
            blockState = new PlayerBlockState(owner, this);
            dodgeState = new PlayerDodgeState(owner, this);
            mantleState = new PlayerMantleState(owner, this);
            hitState = new PlayerHitState(owner, this);
            deathState = new PlayerDeathState(owner, this);
            Subscribe(owner);
            isInitialized = true;
            SwitchState(locomotionState);
        }

        public void Tick(float deltaTime)
        {
            if (owner?.SkillController != null)
            {
                owner.SkillController.Tick(deltaTime);
            }

            if (owner?.CombatController != null)
            {
                owner.CombatController.Tick(deltaTime);
            }

            if (owner?.Motor != null && owner.Motor.IsGrounded)
            {
                hasConsumedAirDodge = false;
            }

            CurrentState?.Tick(deltaTime);
        }

        public void SwitchState(PlayerState nextState)
        {
            if (owner == null)
            {
                Debug.LogWarning("PlayerStateMachine has not been initialized.", this);
                return;
            }

            PlayerState previousState = CurrentState;
            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState?.Enter();
            animationRelay?.NotifyStateChanged(previousState, CurrentState);
        }

        public void SwitchToLocomotion()
        {
            SwitchState(locomotionState);
        }

        public void SwitchToBlock()
        {
            if (IsDeathLocked())
            {
                return;
            }

            SwitchState(blockState);
        }

        public void ApplyBlockStun(float durationSeconds)
        {
            if (CurrentState is PlayerBlockState blockState)
            {
                blockState.ApplyBlockStun(durationSeconds);
            }
        }

        public void SwitchToDodge(PlayerEvasiveActionType actionType = PlayerEvasiveActionType.GroundDodge)
        {
            if (IsDeathLocked())
            {
                return;
            }

            if (actionType == PlayerEvasiveActionType.AirDodge)
            {
                if (!CanStartAirDodge())
                {
                    return;
                }

                hasConsumedAirDodge = true;
            }

            dodgeState.Configure(actionType);
            SwitchState(dodgeState);
        }

        public void SwitchToAirDodge()
        {
            SwitchToDodge(PlayerEvasiveActionType.AirDodge);
        }

        public void SwitchToMantle(Vector3 targetPosition, float durationSeconds)
        {
            if (IsDeathLocked())
            {
                return;
            }

            mantleState.Configure(targetPosition, durationSeconds);
            SwitchState(mantleState);
        }

        public void SwitchToHit(float duration, PlayerHitReactionType reactionType = PlayerHitReactionType.Standard)
        {
            if (IsDeathLocked())
            {
                return;
            }

            hitState.SetDuration(duration, reactionType);
            SwitchState(hitState);
        }

        public void SwitchToDeath()
        {
            SwitchState(deathState);
        }

        public void SwitchToAttack(PlayerAttackRequest request)
        {
            if (IsDeathLocked())
            {
                return;
            }

            SwitchState(new PlayerAttackState(owner, this, request));
        }

        public void SwitchToSkill(int skillSlotIndex)
        {
            if (IsDeathLocked())
            {
                return;
            }

            SwitchState(new PlayerSkillState(owner, this, skillSlotIndex));
        }

        private void OnDisable()
        {
            if (owner != null)
            {
                Unsubscribe(owner);
            }
        }

        private void OnEnable()
        {
            if (!isInitialized || owner == null)
            {
                return;
            }

            Subscribe(owner);
        }

        private void Subscribe(PlayerCharacter player)
        {
            if (player == null || isSubscribed)
            {
                return;
            }

            if (player.InputReader != null)
            {
                player.InputReader.LightAttackPressed += OnLightAttackPressed;
                player.InputReader.HeavyAttackPressed += OnHeavyAttackPressed;
                player.InputReader.DodgePressed += OnDodgePressed;
                player.InputReader.Skill1Pressed += OnSkill1Pressed;
                player.InputReader.Skill2Pressed += OnSkill2Pressed;
            }

            if (player.Health != null)
            {
                player.Health.Died += OnOwnerDied;
            }

            isSubscribed = true;
        }

        private void Unsubscribe(PlayerCharacter player)
        {
            if (player == null || !isSubscribed)
            {
                return;
            }

            if (player.InputReader != null)
            {
                player.InputReader.LightAttackPressed -= OnLightAttackPressed;
                player.InputReader.HeavyAttackPressed -= OnHeavyAttackPressed;
                player.InputReader.DodgePressed -= OnDodgePressed;
                player.InputReader.Skill1Pressed -= OnSkill1Pressed;
                player.InputReader.Skill2Pressed -= OnSkill2Pressed;
            }

            if (player.Health != null)
            {
                player.Health.Died -= OnOwnerDied;
            }

            isSubscribed = false;
        }

        private void OnLightAttackPressed()
        {
            RecordSwordArtInputPreview(SwordArtTriggerAction.LightAttack);
            CurrentState?.HandleLightAttack();
        }

        private void OnHeavyAttackPressed()
        {
            RecordSwordArtInputPreview(SwordArtTriggerAction.HeavyAttack);
            CurrentState?.HandleHeavyAttack();
        }

        private void OnDodgePressed()
        {
            CurrentState?.HandleDodge();
        }

        private void OnSkill1Pressed()
        {
            CurrentState?.HandleSkill1();
        }

        private void OnSkill2Pressed()
        {
            CurrentState?.HandleSkill2();
        }

        private void OnOwnerDied()
        {
            SwitchToDeath();
        }

        private void RecordSwordArtInputPreview(SwordArtTriggerAction triggerAction)
        {
            if (owner?.CombatController == null)
            {
                return;
            }

            owner.CombatController.TryRecordSwordArtPreviewCommand(
                triggerAction,
                ResolveSwordArtInputDirection(),
                ResolveSwordArtContextTags());
        }

        private SwordArtInputDirection ResolveSwordArtInputDirection()
        {
            Vector2 move = owner?.InputReader != null ? owner.InputReader.MoveValue : Vector2.zero;

            if (move.sqrMagnitude < 0.04f)
            {
                return SwordArtInputDirection.Neutral;
            }

            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            {
                return move.x < 0f ? SwordArtInputDirection.Left : SwordArtInputDirection.Right;
            }

            return move.y >= 0f ? SwordArtInputDirection.Forward : SwordArtInputDirection.Backward;
        }

        private SwordArtContextTags ResolveSwordArtContextTags()
        {
            SwordArtContextTags contextTags = SwordArtContextTags.None;
            PlayerCombatController combatController = owner?.CombatController;

            if (combatController != null && combatController.HasDodgeFollowUpWindow)
            {
                contextTags |= SwordArtContextTags.AfterDodge;
            }

            if (CurrentState is PlayerDodgeState dodgeState
                && dodgeState.ActionType == PlayerEvasiveActionType.CombatRoll)
            {
                contextTags |= SwordArtContextTags.AfterDodge;
                contextTags |= SwordArtContextTags.AfterCombatRoll;
            }

            if (CurrentState is PlayerDodgeState airDodgeState
                && airDodgeState.ActionType == PlayerEvasiveActionType.AirDodge)
            {
                contextTags |= SwordArtContextTags.AfterDodge;
                contextTags |= SwordArtContextTags.AfterAirDodge;
                contextTags |= SwordArtContextTags.Airborne;
            }

            if (combatController != null && combatController.HasCounterWindow)
            {
                contextTags |= SwordArtContextTags.AfterBlock;
            }

            if (owner?.Motor != null && !owner.Motor.IsGrounded)
            {
                contextTags |= SwordArtContextTags.Airborne;
            }

            if (combatController != null
                && combatController.CurrentAttackAnimationStateName.StartsWith("Heavy_", System.StringComparison.Ordinal))
            {
                contextTags |= SwordArtContextTags.AfterHeavy;
            }

            return contextTags;
        }

        public bool TryNotifySuccessfulDodge()
        {
            if (CurrentState is not PlayerDodgeState dodgeState)
            {
                return false;
            }

            return dodgeState.TryRegisterSuccessfulDodge();
        }

        private bool CanStartAirDodge()
        {
            return !hasConsumedAirDodge
                && owner?.Motor != null
                && !owner.Motor.IsGrounded;
        }

        private bool IsDeathLocked()
        {
            return CurrentState is PlayerDeathState;
        }
    }
}
