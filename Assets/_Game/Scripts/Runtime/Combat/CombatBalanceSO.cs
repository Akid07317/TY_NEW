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
        [SerializeField] private float dodgeInvulnerableSeconds = 0.2f;
        [SerializeField] private float dodgeDistance = 2.8f;
        [SerializeField] private float dodgeBackwardDistanceScale = 0.88f;
        [SerializeField] private float guardCounterGaugeGain = 20f;
        [SerializeField] private float dodgeAgilityGaugeGain = 25f;
        [SerializeField] private float defaultHitStopSeconds = 0.05f;

        public float InputBufferSeconds => inputBufferSeconds;

        public float CounterWindowSeconds => counterWindowSeconds;

        public float DodgeFollowUpWindowSeconds => dodgeFollowUpWindowSeconds;

        public float DodgeDurationSeconds => dodgeDurationSeconds;

        public float DodgeInvulnerableSeconds => dodgeInvulnerableSeconds;

        public float DodgeDistance => dodgeDistance;

        public float DodgeBackwardDistanceScale => dodgeBackwardDistanceScale;

        public float GuardCounterGaugeGain => guardCounterGaugeGain;

        public float DodgeAgilityGaugeGain => dodgeAgilityGaugeGain;

        public float DefaultHitStopSeconds => defaultHitStopSeconds;
    }
}
