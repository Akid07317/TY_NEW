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

        public PlayerState CurrentState { get; private set; }

        public bool AllowsMovement => CurrentState == null || CurrentState.AllowsMovement;

        public bool AllowsJump => CurrentState == null || CurrentState.AllowsJump;

        public bool IsBlocking => CurrentState is PlayerBlockState;

        public bool IsInvulnerable => CurrentState is PlayerDodgeState dodgeState && dodgeState.IsInvulnerable;

        public bool CanStartMantle => CurrentState is PlayerLocomotionState;

        public PlayerCombatAnimationRelay AnimationRelay => animationRelay;

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
            SwitchState(blockState);
        }

        public void SwitchToDodge()
        {
            SwitchState(dodgeState);
        }

        public void SwitchToMantle(Vector3 targetPosition, float durationSeconds)
        {
            mantleState.Configure(targetPosition, durationSeconds);
            SwitchState(mantleState);
        }

        public void SwitchToHit(float duration)
        {
            hitState.SetDuration(duration);
            SwitchState(hitState);
        }

        public void SwitchToDeath()
        {
            SwitchState(deathState);
        }

        public void SwitchToAttack(PlayerAttackRequest request)
        {
            SwitchState(new PlayerAttackState(owner, this, request));
        }

        public void SwitchToSkill(int skillSlotIndex)
        {
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
            CurrentState?.HandleLightAttack();
        }

        private void OnHeavyAttackPressed()
        {
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

        public bool TryNotifySuccessfulDodge()
        {
            if (CurrentState is not PlayerDodgeState dodgeState)
            {
                return false;
            }

            return dodgeState.TryRegisterSuccessfulDodge();
        }
    }
}
