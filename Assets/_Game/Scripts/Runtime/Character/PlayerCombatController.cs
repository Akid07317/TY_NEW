using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    [RequireComponent(typeof(AttackExecutor))]
    [RequireComponent(typeof(HitboxController))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private CombatBalanceSO balance;
        [SerializeField] private bool prototypeGrantDodgeFollowUpOnAnyDodge;
        [SerializeField] private AttackExecutor attackExecutor;
        [SerializeField] private HitboxController hitboxController;
        [SerializeField] private PlayerCombatAnimationRelay animationRelay;
        [SerializeField] private AttackDefinitionSO[] lightAttackCombo = new AttackDefinitionSO[0];
        [SerializeField] private AttackDefinitionSO heavyAttack;
        [SerializeField] private AttackDefinitionSO dodgeFollowUpAttack;
        [SerializeField] private AttackDefinitionSO empoweredDodgeFollowUpAttack;
        [SerializeField] private AttackDefinitionSO counterAttack;
        [SerializeField] private AttackDefinitionSO empoweredCounterAttack;
        [SerializeField] private float comboResetSeconds = 0.8f;

        private GaugeComponent gauges;
        private int nextLightAttackIndex;
        private float comboResetTimer;
        private float counterWindowTimer;
        private float dodgeFollowUpWindowTimer;
        private AttackDefinitionSO currentAttackDefinition;

        public bool HasCounterWindow => counterWindowTimer > 0f;

        public bool HasDodgeFollowUpWindow => dodgeFollowUpWindowTimer > 0f;

        public bool CanQueueNextLightAttack => lightAttackCombo != null
            && lightAttackCombo.Length > 0
            && nextLightAttackIndex < lightAttackCombo.Length - 1;

        public CombatBalanceSO Balance => balance;

        public AttackExecutor AttackExecutor => attackExecutor;

        public HitboxController HitboxController => hitboxController;

        public AttackDefinitionSO CurrentAttackDefinition => currentAttackDefinition;

        public string CurrentAttackAnimationStateName => currentAttackDefinition != null
            ? currentAttackDefinition.AnimationStateName
            : string.Empty;

        private void Awake()
        {
            gauges = GetComponent<GaugeComponent>();

            if (attackExecutor == null)
            {
                attackExecutor = GetComponent<AttackExecutor>();
            }

            if (hitboxController == null)
            {
                hitboxController = GetComponent<HitboxController>();
            }

            if (animationRelay == null)
            {
                animationRelay = GetComponent<PlayerCombatAnimationRelay>();
            }
        }

        public void Tick(float deltaTime)
        {
            PlayerCombatComboState comboState = PlayerCombatRuntimeUtility.TickComboState(nextLightAttackIndex, comboResetTimer, deltaTime);
            nextLightAttackIndex = comboState.NextLightAttackIndex;
            comboResetTimer = comboState.ComboResetTimer;
            counterWindowTimer = PlayerCombatRuntimeUtility.TickWindow(counterWindowTimer, deltaTime);
            dodgeFollowUpWindowTimer = PlayerCombatRuntimeUtility.TickWindow(dodgeFollowUpWindowTimer, deltaTime);
        }

        public AttackDefinitionSO ResolveAttack(PlayerAttackRequest request)
        {
            switch (request)
            {
                case PlayerAttackRequest.Light:
                    return PlayerCombatRuntimeUtility.ResolveLightAttack(lightAttackCombo, nextLightAttackIndex, comboResetTimer);
                case PlayerAttackRequest.Heavy:
                    ResetCombo();
                    return heavyAttack;
                case PlayerAttackRequest.DodgeFollowUp:
                    dodgeFollowUpWindowTimer = 0f;
                    ResetCombo();
                    return PlayerCombatRuntimeUtility.ResolveDodgeFollowUpAttack(gauges, dodgeFollowUpAttack, empoweredDodgeFollowUpAttack);
                case PlayerAttackRequest.Counter:
                    counterWindowTimer = 0f;
                    ResetCombo();
                    return PlayerCombatRuntimeUtility.ResolveCounterAttack(gauges, counterAttack, empoweredCounterAttack);
                default:
                    return null;
            }
        }

        public void NotifyAttackFinished(PlayerAttackRequest request)
        {
            currentAttackDefinition = null;
            PlayerCombatComboState comboState = PlayerCombatRuntimeUtility.ResolveComboStateAfterAttackFinished(
                request,
                nextLightAttackIndex,
                lightAttackCombo != null ? lightAttackCombo.Length : 0,
                comboResetSeconds);
            nextLightAttackIndex = comboState.NextLightAttackIndex;
            comboResetTimer = comboState.ComboResetTimer;
        }

        public void NotifySuccessfulBlock()
        {
            if (balance != null)
            {
                gauges?.AddCounter(balance.GuardCounterGaugeGain);
                OpenCounterWindow(balance.CounterWindowSeconds);
            }
        }

        public void NotifySuccessfulDodge()
        {
            if (balance != null)
            {
                gauges?.AddAgility(balance.DodgeAgilityGaugeGain);
                OpenDodgeFollowUpWindow(balance.DodgeFollowUpWindowSeconds);
            }
        }

        public void HandleDodgeStarted()
        {
            if (!prototypeGrantDodgeFollowUpOnAnyDodge || balance == null)
            {
                return;
            }

            OpenDodgeFollowUpWindow(balance.DodgeFollowUpWindowSeconds);
        }

        public void NotifyAttackStarted(AttackDefinitionSO attackDefinition)
        {
            currentAttackDefinition = attackDefinition;
            animationRelay?.PlayAttack(attackDefinition);
        }

        public bool ActivatePreparedHitboxFromAnimationEvent()
        {
            return hitboxController != null && hitboxController.Activate();
        }

        public void ClearPreparedHitboxFromAnimationEvent()
        {
            hitboxController?.Clear();
        }

        public void OpenCounterWindow(float duration)
        {
            counterWindowTimer = PlayerCombatRuntimeUtility.OpenWindow(counterWindowTimer, duration);
        }

        public void OpenDodgeFollowUpWindow(float duration)
        {
            dodgeFollowUpWindowTimer = PlayerCombatRuntimeUtility.OpenWindow(dodgeFollowUpWindowTimer, duration);
        }

        public void ResetRuntimeState()
        {
            counterWindowTimer = 0f;
            dodgeFollowUpWindowTimer = 0f;
            currentAttackDefinition = null;
            hitboxController?.Clear();
            ResetCombo();
        }
        private void ResetCombo()
        {
            nextLightAttackIndex = 0;
            comboResetTimer = 0f;
        }
    }
}
