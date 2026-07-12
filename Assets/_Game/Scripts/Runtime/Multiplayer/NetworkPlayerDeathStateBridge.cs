using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerDeathStateBridge : MonoBehaviour
    {
        [SerializeField] private NetworkPlayerAvatar avatar;
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerStateMachine stateMachine;

        private bool appliedReplicatedDeath;

        public bool HasAppliedReplicatedDeath => appliedReplicatedDeath;

        public void Configure(
            NetworkPlayerAvatar networkAvatar,
            HealthComponent healthComponent,
            PlayerStateMachine playerStateMachine)
        {
            avatar = networkAvatar;
            health = healthComponent;
            stateMachine = playerStateMachine;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();

            if (avatar == null)
            {
                return;
            }

            if (!avatar.IsDead)
            {
                appliedReplicatedDeath = false;
                return;
            }

            if (appliedReplicatedDeath)
            {
                return;
            }

            appliedReplicatedDeath = ApplyAuthoritativeDeath(
                health,
                stateMachine,
                transform.position,
                avatar.gameObject);
        }

        public static bool ApplyAuthoritativeDeath(
            HealthComponent health,
            PlayerStateMachine stateMachine,
            Vector3 hitPoint,
            GameObject source)
        {
            bool applied = false;

            if (health != null && !health.IsDead)
            {
                health.ReceiveDamage(float.MaxValue, hitPoint, source);
                applied = true;
            }

            if (stateMachine != null && stateMachine.CurrentState is not PlayerDeathState)
            {
                stateMachine.SwitchToDeath();
                applied = true;
            }

            return applied;
        }

        private void ResolveReferences()
        {
            if (avatar == null)
            {
                avatar = GetComponent<NetworkPlayerAvatar>();
            }

            if (avatar == null)
            {
                avatar = GetComponentInParent<NetworkPlayerAvatar>();
            }

            if (avatar == null)
            {
                avatar = GetComponentInChildren<NetworkPlayerAvatar>(true);
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
        }
    }
}
