using UnityEngine;

namespace CampusRPG.Combat
{
    [CreateAssetMenu(fileName = "SO_CombatBalance", menuName = "CampusRPG/Combat/Combat Balance")]
    public sealed class CombatBalanceSO : ScriptableObject
    {
        [SerializeField] private float inputBufferSeconds = 0.2f;
        [SerializeField] private float counterWindowSeconds = 0.8f;
        [SerializeField] private float dodgeFollowUpWindowSeconds = 0.8f;
        [SerializeField] private float dodgeDurationSeconds = 0.25f;
        [SerializeField] private float dodgeInvulnerableStartupSeconds = 0.04f;
        [SerializeField] private float dodgeInvulnerableSeconds = 0.2f;
        [SerializeField] private float dodgeDistance = 2.8f;
        [SerializeField] private float dodgeBackwardDistanceScale = 0.88f;
        [SerializeField] private float combatRollDurationSeconds = 0.42f;
        [SerializeField] private float combatRollInvulnerableStartupSeconds = 0.08f;
        [SerializeField] private float combatRollInvulnerableSeconds = 0.18f;
        [SerializeField] private float combatRollDistance = 3.6f;
        [SerializeField] private float airDodgeDurationSeconds = 0.28f;
        [SerializeField] private float airDodgeInvulnerableStartupSeconds = 0.03f;
        [SerializeField] private float airDodgeInvulnerableSeconds = 0.16f;
        [SerializeField] private float airDodgeDistance = 2.35f;
        [SerializeField] private float airDodgeVerticalVelocity = 3.2f;
        [SerializeField] private float guardStartupSeconds = 0.08f;
        [SerializeField] private float guardCounterGaugeGain = 20f;
        [SerializeField] private float dodgeAgilityGaugeGain = 25f;
        [SerializeField] private float defaultHitStopSeconds = 0.05f;

        public float InputBufferSeconds => inputBufferSeconds;

        public float CounterWindowSeconds => counterWindowSeconds;

        public float DodgeFollowUpWindowSeconds => dodgeFollowUpWindowSeconds;

        public float DodgeDurationSeconds => dodgeDurationSeconds;

        public float DodgeInvulnerableStartupSeconds => dodgeInvulnerableStartupSeconds;

        public float DodgeInvulnerableSeconds => dodgeInvulnerableSeconds;

        public float DodgeDistance => dodgeDistance;

        public float DodgeBackwardDistanceScale => dodgeBackwardDistanceScale;

        public float CombatRollDurationSeconds => combatRollDurationSeconds;

        public float CombatRollInvulnerableStartupSeconds => combatRollInvulnerableStartupSeconds;

        public float CombatRollInvulnerableSeconds => combatRollInvulnerableSeconds;

        public float CombatRollDistance => combatRollDistance;

        public float AirDodgeDurationSeconds => airDodgeDurationSeconds;

        public float AirDodgeInvulnerableStartupSeconds => airDodgeInvulnerableStartupSeconds;

        public float AirDodgeInvulnerableSeconds => airDodgeInvulnerableSeconds;

        public float AirDodgeDistance => airDodgeDistance;

        public float AirDodgeVerticalVelocity => airDodgeVerticalVelocity;

        public float GuardStartupSeconds => guardStartupSeconds;

        public float GuardCounterGaugeGain => guardCounterGaugeGain;

        public float DodgeAgilityGaugeGain => dodgeAgilityGaugeGain;

        public float DefaultHitStopSeconds => defaultHitStopSeconds;
    }
}
