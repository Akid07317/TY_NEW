using UnityEngine;

namespace CampusRPG.Character
{
    [CreateAssetMenu(fileName = "SO_PlayerBaseStats", menuName = "CampusRPG/Character/Player Base Stats")]
    public sealed class PlayerBaseStatsSO : ScriptableObject
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float attack = 20f;
        [SerializeField] private float defense = 10f;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float groundAcceleration = 24f;
        [SerializeField] private float groundDeceleration = 20f;
        [SerializeField] private float lockOnStrafeSpeedScale = 0.92f;
        [SerializeField] private float lockOnBackwardSpeedScale = 0.82f;
        [SerializeField] private float mantleDurationSeconds = 0.22f;
        [SerializeField] private float mantleMinHeight = 0.5f;
        [SerializeField] private float mantleMaxHeight = 1.25f;
        [SerializeField] private float mantleForwardDistance = 0.8f;

        public float MaxHealth => maxHealth;

        public float MaxMana => maxMana;

        public float Attack => attack;

        public float Defense => defense;

        public float MoveSpeed => moveSpeed;

        public float RotationSpeed => rotationSpeed;

        public float JumpHeight => jumpHeight;

        public float GroundAcceleration => groundAcceleration;

        public float GroundDeceleration => groundDeceleration;

        public float LockOnStrafeSpeedScale => lockOnStrafeSpeedScale;

        public float LockOnBackwardSpeedScale => lockOnBackwardSpeedScale;

        public float MantleDurationSeconds => mantleDurationSeconds;

        public float MantleMinHeight => mantleMinHeight;

        public float MantleMaxHeight => mantleMaxHeight;

        public float MantleForwardDistance => mantleForwardDistance;
    }
}
