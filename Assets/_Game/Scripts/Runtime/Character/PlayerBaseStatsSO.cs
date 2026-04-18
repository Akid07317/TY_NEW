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

        public float MaxHealth => maxHealth;

        public float MaxMana => maxMana;

        public float Attack => attack;

        public float Defense => defense;

        public float MoveSpeed => moveSpeed;

        public float RotationSpeed => rotationSpeed;

        public float JumpHeight => jumpHeight;
    }
}
