using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerPresentationBridge : MonoBehaviour
    {
        private const float DefaultAuthoritativeAttackPresentationSeconds = 0.35f;
        private const float DefaultAuthoritativeHitReactionSeconds = 0.26f;

        [SerializeField] private NetworkPlayerAvatar avatar;
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerStateMachine stateMachine;

        private PlayerMotor motor;
        private bool stateMachineChecked;
        private bool localPlayerDriverSuppressed;
        private bool hasAuthoritativeHealthSample;
        private int lastAuthoritativeHealth;
        private bool hasAuthoritativeAttackPresentationSample;
        private uint lastAuthoritativeAttackPresentationSequence;
        private bool hasObservedFormalAttackPresentation;
        private bool hasObservedFormalHitReaction;
        private float formalAttackPresentationTimer;
        private float formalHitReactionTimer;

        public bool LocalPlayerDriverSuppressed => localPlayerDriverSuppressed;

        public bool IsFormalAttackStateActive =>
            health != null
            && !health.IsDead
            && stateMachine != null
            && stateMachine.CurrentState is PlayerAttackState;

        public bool IsFormalHitStateActive =>
            health != null
            && !health.IsDead
            && stateMachine != null
            && stateMachine.CurrentState is PlayerHitState;

        public bool IsFormalDeathStateActive =>
            health != null
            && health.IsDead
            && stateMachine != null
            && stateMachine.CurrentState is PlayerDeathState;

        public bool HasObservedFormalAttackPresentation => hasObservedFormalAttackPresentation;

        public bool HasObservedFormalHitReaction => hasObservedFormalHitReaction;

        public void Configure(
            NetworkPlayerAvatar networkAvatar,
            PlayerCharacter formalPlayer,
            HealthComponent healthComponent,
            PlayerStateMachine playerStateMachine)
        {
            avatar = networkAvatar;
            player = formalPlayer;
            health = healthComponent;
            stateMachine = playerStateMachine;
            motor = player != null ? player.Motor : null;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            EnsureStateMachineInitialized();
            SuppressLocalPlayerDriver();
            ConstrainPresentationTransform(transform);
        }

        private void Update()
        {
            ResolveReferences();
            EnsureStateMachineInitialized();
            SuppressLocalPlayerDriver();

            if (avatar == null)
            {
                return;
            }

            bool applyHitReaction = ShouldApplyAuthoritativeHitReaction(avatar.CurrentHealth, avatar.IsDead);
            ApplyAuthoritativeState(avatar.CurrentHealth, avatar.IsDead, applyHitReaction);
            ApplyAuthoritativeAttackPresentation(
                avatar.AttackPresentationSequence,
                avatar.AttackPresentationCode,
                avatar.IsDead);
            TickFormalAttackPresentation(Time.deltaTime, avatar.IsDead);
            TickFormalHitReaction(Time.deltaTime, avatar.IsDead);
        }

        private void LateUpdate()
        {
            SuppressLocalPlayerDriver();
            ConstrainPresentationTransform(transform);
            motor?.ResetMotion();
        }

        public bool ApplyAuthoritativeState(int authoritativeHealth, bool authoritativeDead)
        {
            return ApplyAuthoritativeState(authoritativeHealth, authoritativeDead, false);
        }

        public bool ApplyAuthoritativeState(int authoritativeHealth, bool authoritativeDead, bool applyHitReaction)
        {
            bool applied = ApplyAuthoritativePresentationState(
                health,
                stateMachine,
                transform.position,
                avatar != null ? avatar.gameObject : gameObject,
                authoritativeHealth,
                authoritativeDead,
                applyHitReaction,
                DefaultAuthoritativeHitReactionSeconds,
                out bool appliedHitReaction);

            RecordAppliedHitReaction(appliedHitReaction);
            return applied;
        }

        public bool ApplyAuthoritativeAttackPresentation(
            uint attackPresentationSequence,
            int attackPresentationCode,
            bool authoritativeDead)
        {
            bool isNewPresentation =
                !hasAuthoritativeAttackPresentationSample
                || attackPresentationSequence != lastAuthoritativeAttackPresentationSequence;

            hasAuthoritativeAttackPresentationSample = true;
            lastAuthoritativeAttackPresentationSequence = attackPresentationSequence;

            if (!isNewPresentation || attackPresentationSequence == 0u)
            {
                return false;
            }

            bool applied = ApplyAuthoritativeAttackPresentationState(
                player,
                health,
                stateMachine,
                attackPresentationCode,
                authoritativeDead);

            RecordAppliedAttackPresentation(applied);
            return applied;
        }

        public static bool ApplyAuthoritativePresentationState(
            HealthComponent health,
            PlayerStateMachine stateMachine,
            Vector3 hitPoint,
            GameObject source,
            int authoritativeHealth,
            bool authoritativeDead)
        {
            return ApplyAuthoritativePresentationState(
                health,
                stateMachine,
                hitPoint,
                source,
                authoritativeHealth,
                authoritativeDead,
                false,
                DefaultAuthoritativeHitReactionSeconds,
                out _);
        }

        public static bool ApplyAuthoritativePresentationState(
            HealthComponent health,
            PlayerStateMachine stateMachine,
            Vector3 hitPoint,
            GameObject source,
            int authoritativeHealth,
            bool authoritativeDead,
            bool applyHitReaction,
            float hitReactionSeconds)
        {
            return ApplyAuthoritativePresentationState(
                health,
                stateMachine,
                hitPoint,
                source,
                authoritativeHealth,
                authoritativeDead,
                applyHitReaction,
                hitReactionSeconds,
                out _);
        }

        public static bool ApplyAuthoritativePresentationState(
            HealthComponent health,
            PlayerStateMachine stateMachine,
            Vector3 hitPoint,
            GameObject source,
            int authoritativeHealth,
            bool authoritativeDead,
            bool applyHitReaction,
            float hitReactionSeconds,
            out bool appliedHitReaction)
        {
            bool applied = false;
            int clampedHealth = Mathf.Max(0, authoritativeHealth);
            appliedHitReaction = false;

            if (authoritativeDead)
            {
                applied |= NetworkPlayerDeathStateBridge.ApplyAuthoritativeDeath(
                    health,
                    stateMachine,
                    hitPoint,
                    source);

                if (health != null && !Mathf.Approximately(health.CurrentValue, 0f))
                {
                    health.SetCurrent(0f);
                    applied = true;
                }

                return applied;
            }

            if (health != null && !Mathf.Approximately(health.CurrentValue, clampedHealth))
            {
                health.SetCurrent(clampedHealth);
                applied = true;
            }

            if (stateMachine != null
                && clampedHealth > 0
                && stateMachine.CurrentState is PlayerDeathState)
            {
                stateMachine.SwitchToLocomotion();
                applied = true;
            }

            if (applyHitReaction
                && stateMachine != null
                && clampedHealth > 0
                && stateMachine.CurrentState is not PlayerDeathState)
            {
                stateMachine.SwitchToHit(Mathf.Max(0.01f, hitReactionSeconds), PlayerHitReactionType.Standard);
                applied = true;
                appliedHitReaction = stateMachine.CurrentState is PlayerHitState;
            }

            return applied;
        }

        public static bool ApplyAuthoritativeAttackPresentationState(
            PlayerCharacter player,
            HealthComponent health,
            PlayerStateMachine stateMachine,
            int attackPresentationCode,
            bool authoritativeDead)
        {
            if (player == null
                || stateMachine == null
                || authoritativeDead
                || (health != null && health.IsDead)
                || stateMachine.CurrentState is PlayerDeathState
                || stateMachine.CurrentState is PlayerHitState
                || !NetworkPlayerAvatar.TryResolveAttackPresentationRequest(
                    attackPresentationCode,
                    out PlayerAttackRequest request))
            {
                return false;
            }

            stateMachine.SwitchToAttack(request);
            player.CombatController?.HitboxController?.Clear();
            return stateMachine.CurrentState is PlayerAttackState;
        }

        public static void ConstrainPresentationTransform(Transform presentationRoot)
        {
            if (presentationRoot == null)
            {
                return;
            }

            presentationRoot.localPosition = Vector3.zero;
            presentationRoot.localRotation = Quaternion.identity;
            presentationRoot.localScale = Vector3.one;
        }

        private void SuppressLocalPlayerDriver()
        {
            if (player == null)
            {
                localPlayerDriverSuppressed = false;
                return;
            }

            if (player.enabled)
            {
                player.enabled = false;
            }

            localPlayerDriverSuppressed = !player.enabled;
        }

        private void RecordAppliedAttackPresentation(bool appliedAttackPresentation)
        {
            if (!appliedAttackPresentation)
            {
                return;
            }

            hasObservedFormalAttackPresentation = true;
            formalAttackPresentationTimer = DefaultAuthoritativeAttackPresentationSeconds;
        }

        private void RecordAppliedHitReaction(bool appliedHitReaction)
        {
            if (!appliedHitReaction)
            {
                return;
            }

            hasObservedFormalHitReaction = true;
            formalHitReactionTimer = DefaultAuthoritativeHitReactionSeconds;
        }

        private bool ShouldApplyAuthoritativeHitReaction(int authoritativeHealth, bool authoritativeDead)
        {
            int clampedHealth = Mathf.Max(0, authoritativeHealth);
            bool applyHitReaction =
                hasAuthoritativeHealthSample
                && clampedHealth < lastAuthoritativeHealth
                && clampedHealth > 0
                && !authoritativeDead;

            hasAuthoritativeHealthSample = true;
            lastAuthoritativeHealth = clampedHealth;
            return applyHitReaction;
        }

        private void TickFormalAttackPresentation(float deltaTime, bool authoritativeDead)
        {
            if (authoritativeDead || !IsFormalAttackStateActive)
            {
                formalAttackPresentationTimer = 0f;
                return;
            }

            formalAttackPresentationTimer -= Mathf.Max(0f, deltaTime);
            if (formalAttackPresentationTimer <= 0f)
            {
                stateMachine.SwitchToLocomotion();
            }
        }

        private void TickFormalHitReaction(float deltaTime, bool authoritativeDead)
        {
            if (authoritativeDead || !IsFormalHitStateActive)
            {
                formalHitReactionTimer = 0f;
                return;
            }

            formalHitReactionTimer -= Mathf.Max(0f, deltaTime);
            if (formalHitReactionTimer <= 0f)
            {
                stateMachine.SwitchToLocomotion();
            }
        }

        private void EnsureStateMachineInitialized()
        {
            if (stateMachineChecked)
            {
                return;
            }

            stateMachineChecked = true;

            if (player != null && stateMachine != null && stateMachine.CurrentState == null)
            {
                stateMachine.Initialize(player);
            }
        }

        private void ResolveReferences()
        {
            if (avatar == null)
            {
                avatar = GetComponentInParent<NetworkPlayerAvatar>();
            }

            if (avatar == null)
            {
                avatar = GetComponentInChildren<NetworkPlayerAvatar>(true);
            }

            if (player == null)
            {
                player = GetComponent<PlayerCharacter>();
            }

            if (player == null)
            {
                player = GetComponentInParent<PlayerCharacter>();
            }

            if (player == null)
            {
                player = GetComponentInChildren<PlayerCharacter>(true);
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            if (health == null)
            {
                health = GetComponentInParent<HealthComponent>();
            }

            if (health == null)
            {
                health = GetComponentInChildren<HealthComponent>(true);
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponentInParent<PlayerStateMachine>();
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponentInChildren<PlayerStateMachine>(true);
            }

            if (motor == null && player != null)
            {
                motor = player.Motor;
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }
        }
    }
}
