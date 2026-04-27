using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    [RequireComponent(typeof(AttackExecutor))]
    [RequireComponent(typeof(HitboxController))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        private const float SwordArtPreviewDisplaySeconds = 0.9f;
        private const float RecentSwordArtDisplaySeconds = 1.2f;

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
        [SerializeField] private SwordArtDefinitionSO[] swordArts = new SwordArtDefinitionSO[0];
        [SerializeField] private float comboResetSeconds = 0.8f;

        private GaugeComponent gauges;
        private int nextLightAttackIndex;
        private float comboResetTimer;
        private float counterWindowTimer;
        private float dodgeFollowUpWindowTimer;
        private float currentAttackElapsedSeconds;
        private float currentAttackDurationSeconds;
        private AttackDefinitionSO currentAttackDefinition;
        private readonly SwordArtCommandBuffer swordArtCommandBuffer = new SwordArtCommandBuffer();
        private SwordArtDefinitionSO previewSwordArt;
        private AttackDefinitionSO previewSwordArtAttack;
        private SwordArtDefinitionSO currentSwordArt;
        private AttackDefinitionSO currentSwordArtAttack;
        private SwordArtDefinitionSO recentSwordArt;
        private AttackDefinitionSO recentSwordArtAttack;
        private float previewSwordArtTimer;
        private float recentSwordArtTimer;

        public bool HasCounterWindow => counterWindowTimer > 0f;

        public bool HasDodgeFollowUpWindow => dodgeFollowUpWindowTimer > 0f;

        public bool CanQueueNextLightAttack => lightAttackCombo != null
            && lightAttackCombo.Length > 0
            && nextLightAttackIndex < lightAttackCombo.Length - 1;

        public bool HasBufferedSwordArtCommand => swordArtCommandBuffer.HasCommand;

        public SwordArtCommand CurrentBufferedSwordArtCommand => swordArtCommandBuffer.CurrentCommand;

        public bool HasSwordArtPreview => previewSwordArtTimer > 0f
            && previewSwordArt != null
            && previewSwordArtAttack != null;

        public SwordArtDefinitionSO PreviewSwordArt => HasSwordArtPreview ? previewSwordArt : null;

        public AttackDefinitionSO PreviewSwordArtAttack => HasSwordArtPreview ? previewSwordArtAttack : null;

        public bool HasCurrentSwordArt => currentSwordArt != null
            && currentSwordArtAttack != null
            && currentAttackDefinition == currentSwordArtAttack;

        public SwordArtDefinitionSO CurrentSwordArt => HasCurrentSwordArt ? currentSwordArt : null;

        public AttackDefinitionSO CurrentSwordArtAttack => HasCurrentSwordArt ? currentSwordArtAttack : null;

        public bool HasRecentSwordArt => recentSwordArtTimer > 0f
            && recentSwordArt != null
            && recentSwordArtAttack != null;

        public SwordArtDefinitionSO RecentSwordArt => HasRecentSwordArt ? recentSwordArt : null;

        public AttackDefinitionSO RecentSwordArtAttack => HasRecentSwordArt ? recentSwordArtAttack : null;

        public float RecentSwordArtDisplayRemainingSeconds => HasRecentSwordArt ? recentSwordArtTimer : 0f;

        public CombatBalanceSO Balance => balance;

        public AttackExecutor AttackExecutor => attackExecutor;

        public HitboxController HitboxController => hitboxController;

        public AttackDefinitionSO CurrentAttackDefinition => currentAttackDefinition;

        public bool HasCurrentAttackTiming => currentAttackDefinition != null && currentAttackDurationSeconds > 0f;

        public float CurrentAttackElapsedSeconds => HasCurrentAttackTiming ? currentAttackElapsedSeconds : 0f;

        public float CurrentAttackDurationSeconds => HasCurrentAttackTiming ? currentAttackDurationSeconds : 0f;

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
            swordArtCommandBuffer.Tick(deltaTime);
            TickSwordArtPreview(deltaTime);
            TickRecentSwordArt(deltaTime);
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

        public bool TryPreviewSwordArt(
            SwordArtCommand command,
            out SwordArtDefinitionSO swordArt,
            out AttackDefinitionSO attackDefinition)
        {
            attackDefinition = null;

            if (!SwordArtResolver.TryResolve(swordArts, command, out swordArt))
            {
                return false;
            }

            attackDefinition = swordArt.AttackDefinition;
            return attackDefinition != null;
        }

        public void BufferSwordArtCommand(
            SwordArtTriggerAction triggerAction,
            SwordArtInputDirection direction,
            SwordArtContextTags contextTags = SwordArtContextTags.None)
        {
            swordArtCommandBuffer.Buffer(triggerAction, direction, contextTags);
        }

        public bool TryRecordSwordArtPreviewCommand(
            SwordArtTriggerAction triggerAction,
            SwordArtInputDirection direction,
            SwordArtContextTags contextTags = SwordArtContextTags.None)
        {
            BufferSwordArtCommand(triggerAction, direction, contextTags);

            if (!TryPreviewBufferedSwordArt(out SwordArtDefinitionSO swordArt, out AttackDefinitionSO attackDefinition))
            {
                swordArtCommandBuffer.Clear();
                ClearSwordArtPreview();
                return false;
            }

            previewSwordArt = swordArt;
            previewSwordArtAttack = attackDefinition;
            previewSwordArtTimer = SwordArtPreviewDisplaySeconds;
            return true;
        }

        public bool TryPreviewBufferedSwordArt(out SwordArtDefinitionSO swordArt, out AttackDefinitionSO attackDefinition)
        {
            return TryResolveBufferedSwordArt(consumeOnSuccess: false, out swordArt, out attackDefinition);
        }

        public bool TryConsumeBufferedSwordArt(out SwordArtDefinitionSO swordArt, out AttackDefinitionSO attackDefinition)
        {
            return TryResolveBufferedSwordArt(consumeOnSuccess: true, out swordArt, out attackDefinition);
        }

        public void NotifyAttackFinished(PlayerAttackRequest request)
        {
            RecordRecentSwordArt();
            currentAttackDefinition = null;
            ClearCurrentAttackTiming();
            ClearCurrentSwordArt();
            PlayerCombatComboState comboState = PlayerCombatRuntimeUtility.ResolveComboStateAfterAttackFinished(
                request,
                nextLightAttackIndex,
                lightAttackCombo != null ? lightAttackCombo.Length : 0,
                comboResetSeconds);
            nextLightAttackIndex = comboState.NextLightAttackIndex;
            comboResetTimer = comboState.ComboResetTimer;
        }

        public void NotifyAttackCanceled()
        {
            RecordRecentSwordArt();
            currentAttackDefinition = null;
            ClearCurrentAttackTiming();
            ClearCurrentSwordArt();
            hitboxController?.Clear();
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

            if (currentSwordArtAttack != attackDefinition)
            {
                ClearCurrentSwordArt();
            }

            animationRelay?.PlayAttack(attackDefinition);
        }

        public void NotifyAttackTiming(float elapsedSeconds, float durationSeconds)
        {
            currentAttackElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            currentAttackDurationSeconds = Mathf.Max(0f, durationSeconds);
        }

        public void NotifySwordArtStarted(SwordArtDefinitionSO swordArt, AttackDefinitionSO attackDefinition)
        {
            if (swordArt == null || attackDefinition == null)
            {
                ClearCurrentSwordArt();
                return;
            }

            currentSwordArt = swordArt;
            currentSwordArtAttack = attackDefinition;
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
            ClearCurrentAttackTiming();
            ClearCurrentSwordArt();
            ClearRecentSwordArt();
            swordArtCommandBuffer.Clear();
            ClearSwordArtPreview();
            hitboxController?.Clear();
            ResetCombo();
        }

        public bool TryGetBufferedSwordArtCancelWindowStatus(
            out SwordArtDefinitionSO swordArt,
            out AttackDefinitionSO attackDefinition,
            out bool isOpen,
            out float secondsUntilOpen)
        {
            swordArt = null;
            attackDefinition = null;
            isOpen = false;
            secondsUntilOpen = 0f;

            if (!HasCurrentAttackTiming
                || currentAttackDefinition == null
                || !currentAttackDefinition.AnimationStateName.StartsWith("Heavy_", System.StringComparison.Ordinal)
                || !TryPreviewBufferedSwordArt(out swordArt, out attackDefinition)
                || swordArt.CancelWindowSeconds <= 0f)
            {
                return false;
            }

            float windowStartSeconds = Mathf.Max(0f, currentAttackDurationSeconds - swordArt.CancelWindowSeconds);
            secondsUntilOpen = Mathf.Max(0f, windowStartSeconds - currentAttackElapsedSeconds);
            isOpen = secondsUntilOpen <= 0f;
            return true;
        }

        private bool TryResolveBufferedSwordArt(
            bool consumeOnSuccess,
            out SwordArtDefinitionSO swordArt,
            out AttackDefinitionSO attackDefinition)
        {
            attackDefinition = null;

            if (!swordArtCommandBuffer.TryResolve(swordArts, out swordArt, consumeOnSuccess))
            {
                return false;
            }

            attackDefinition = swordArt.AttackDefinition;
            return attackDefinition != null;
        }

        private void TickSwordArtPreview(float deltaTime)
        {
            if (previewSwordArtTimer <= 0f)
            {
                return;
            }

            previewSwordArtTimer = Mathf.Max(0f, previewSwordArtTimer - Mathf.Max(0f, deltaTime));

            if (previewSwordArtTimer <= 0f)
            {
                ClearSwordArtPreview();
            }
        }

        private void TickRecentSwordArt(float deltaTime)
        {
            if (recentSwordArtTimer <= 0f)
            {
                return;
            }

            recentSwordArtTimer = Mathf.Max(0f, recentSwordArtTimer - Mathf.Max(0f, deltaTime));

            if (recentSwordArtTimer <= 0f)
            {
                ClearRecentSwordArt();
            }
        }

        private void RecordRecentSwordArt()
        {
            if (!HasCurrentSwordArt)
            {
                return;
            }

            recentSwordArt = currentSwordArt;
            recentSwordArtAttack = currentSwordArtAttack;
            recentSwordArtTimer = RecentSwordArtDisplaySeconds;
        }

        private void ClearSwordArtPreview()
        {
            previewSwordArt = null;
            previewSwordArtAttack = null;
            previewSwordArtTimer = 0f;
        }

        private void ClearRecentSwordArt()
        {
            recentSwordArt = null;
            recentSwordArtAttack = null;
            recentSwordArtTimer = 0f;
        }

        private void ClearCurrentSwordArt()
        {
            currentSwordArt = null;
            currentSwordArtAttack = null;
        }

        private void ClearCurrentAttackTiming()
        {
            currentAttackElapsedSeconds = 0f;
            currentAttackDurationSeconds = 0f;
        }

        private void ResetCombo()
        {
            nextLightAttackIndex = 0;
            comboResetTimer = 0f;
        }
    }
}
