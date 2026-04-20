using CampusRPG.Combat;
using CampusRPG.Composition;
using CampusRPG.Input;
using CampusRPG.Skills;
using UnityEngine;
using CampusRPG.Camera;

namespace CampusRPG.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DamageableReceiver))]
    public sealed class PlayerCharacter : MonoBehaviour
    {
        [SerializeField] private PlayerBaseStatsSO baseStats;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerStateMachine stateMachine;
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private SkillController skillController;
        [SerializeField] private PlayerMovementProbe movementProbe;
        [SerializeField] private HealthComponent health;
        [SerializeField] private ManaComponent mana;
        [SerializeField] private GaugeComponent gauges;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private LockOnTargetSelector lockOnTargetSelector;

        private bool jumpQueued;

        public PlayerBaseStatsSO BaseStats => baseStats;

        public InputReader InputReader => inputReader;

        public PlayerMotor Motor => motor;

        public PlayerStateMachine StateMachine => stateMachine;

        public PlayerCombatController CombatController => combatController;

        public SkillController SkillController => skillController;

        public PlayerMovementProbe MovementProbe => movementProbe;

        public HealthComponent Health => health;

        public ManaComponent Mana => mana;

        public GaugeComponent Gauges => gauges;

        public Transform CameraTransform => cameraTransform;

        public LockOnTargetSelector LockOnTargetSelector => lockOnTargetSelector;

        private void Awake()
        {
            ResolveInputReader();

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
            }

            if (combatController == null)
            {
                combatController = GetComponent<PlayerCombatController>();
            }

            if (skillController == null)
            {
                skillController = GetComponent<SkillController>();
            }

            if (movementProbe == null)
            {
                movementProbe = GetComponent<PlayerMovementProbe>();
            }

            if (movementProbe == null)
            {
                movementProbe = gameObject.AddComponent<PlayerMovementProbe>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            if (mana == null)
            {
                mana = GetComponent<ManaComponent>();
            }

            if (gauges == null)
            {
                gauges = GetComponent<GaugeComponent>();
            }

            if (stateMachine != null)
            {
                stateMachine.Initialize(this);
            }

            cameraTransform = SceneRuntimeReferenceUtility.ResolveCameraTransform(cameraTransform, this);
            lockOnTargetSelector = SceneRuntimeReferenceUtility.ResolveLockOnTargetSelector(lockOnTargetSelector, this);

            ApplyBaseStats();
        }

        private void OnEnable()
        {
            ResolveInputReader();

            if (inputReader != null)
            {
                inputReader.JumpPressed += QueueJump;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.JumpPressed -= QueueJump;
            }
        }

        private void Update()
        {
            if (motor != null && inputReader != null)
            {
                bool allowsMovement = stateMachine == null || stateMachine.AllowsMovement;
                bool allowsJump = stateMachine == null || stateMachine.AllowsJump;
                Vector2 moveInput = allowsMovement
                    ? inputReader.MoveValue
                    : Vector2.zero;
                bool jumpPressed = allowsJump && ConsumeJumpQueued();

                if (jumpPressed && TryBeginMantle())
                {
                    jumpPressed = false;
                }

                motor.Tick(moveInput, jumpPressed, cameraTransform, allowsMovement);
            }

            if (stateMachine != null)
            {
                stateMachine.Tick(Time.deltaTime);
            }
        }

        public void ApplyBaseStats()
        {
            if (baseStats == null)
            {
                return;
            }

            health?.SetMax(baseStats.MaxHealth, true);
            mana?.SetMax(baseStats.MaxMana, true);
            motor?.ApplyMovementStats(baseStats.MoveSpeed, baseStats.RotationSpeed, baseStats.JumpHeight);
            motor?.ApplyMovementTuning(
                baseStats.GroundAcceleration,
                baseStats.GroundDeceleration,
                baseStats.LockOnStrafeSpeedScale,
                baseStats.LockOnBackwardSpeedScale);
        }

        public void RestoreFromCheckpoint(Vector3 worldPosition, Quaternion worldRotation, float healthValue, float manaValue)
        {
            jumpQueued = false;
            motor?.WarpTo(worldPosition, worldRotation);
            motor?.ResetMotion();
            health?.SetCurrent(healthValue);
            mana?.SetCurrent(manaValue);
            gauges?.ResetAll();
            combatController?.ResetRuntimeState();
            stateMachine?.SwitchToLocomotion();
        }

        private void QueueJump()
        {
            jumpQueued = true;
        }

        private bool ConsumeJumpQueued()
        {
            bool result = jumpQueued;
            jumpQueued = false;
            return result;
        }

        private void ResolveInputReader()
        {
            inputReader = SceneRuntimeReferenceUtility.ResolveInputReader(inputReader);
        }

        private bool TryBeginMantle()
        {
            if (baseStats == null
                || movementProbe == null
                || stateMachine == null
                || !stateMachine.CanStartMantle)
            {
                return false;
            }

            if (!movementProbe.TryFindMantleTarget(baseStats, transform, out Vector3 mantleTarget))
            {
                return false;
            }

            stateMachine.SwitchToMantle(mantleTarget, baseStats.MantleDurationSeconds);
            return true;
        }
    }
}
