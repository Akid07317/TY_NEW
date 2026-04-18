using CampusRPG.Combat;

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
        private bool queuedNextLightAttack;
        private bool isActiveWindowStarted;

        public PlayerAttackState(PlayerCharacter owner, PlayerStateMachine stateMachine, PlayerAttackRequest request) : base(owner)
        {
            this.stateMachine = stateMachine;
            this.request = request;
        }

        public override bool AllowsMovement => false;

        public override bool AllowsJump => false;

        public override void Enter()
        {
            definition = Owner.CombatController != null ? Owner.CombatController.ResolveAttack(request) : null;

            if (definition == null)
            {
                stateMachine.SwitchToLocomotion();
                return;
            }

            startupRemaining = definition.StartupSeconds;
            activeRemaining = definition.ActiveSeconds;
            recoveryRemaining = definition.RecoverySeconds;

            if (Owner.CombatController?.HitboxController != null && Owner.BaseStats != null)
            {
                Owner.CombatController.HitboxController.Prepare(definition, Owner.BaseStats.Attack, Owner.gameObject);
            }

            Owner.CombatController?.NotifyAttackStarted(definition);

            if (startupRemaining <= 0f)
            {
                BeginActiveWindow();
            }
        }

        public override void Tick(float deltaTime)
        {
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

            if (queuedNextLightAttack && request == PlayerAttackRequest.Light)
            {
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                return;
            }

            stateMachine.SwitchToLocomotion();
        }

        public override void HandleLightAttack()
        {
            if (request == PlayerAttackRequest.Light && Owner.CombatController != null && Owner.CombatController.CanQueueNextLightAttack)
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
            }
        }

        public override void Exit()
        {
            Owner.CombatController?.HitboxController?.Clear();
        }

        private void BeginActiveWindow()
        {
            isActiveWindowStarted = true;
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
    }
}
