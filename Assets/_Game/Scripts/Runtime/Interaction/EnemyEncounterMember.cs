using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Interaction
{
    [DisallowMultipleComponent]
    public sealed class EnemyEncounterMember : MonoBehaviour
    {
        [SerializeField] private EncounterController ownerEncounter;
        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private HealthComponent health;
        [SerializeField] private bool disableObjectOnDeath = true;

        private bool isDefeated;

        public bool IsDefeated => isDefeated;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        public void BindEncounter(EncounterController encounter)
        {
            ownerEncounter = encounter;
        }

        public void ResetForEncounter(bool activateObject)
        {
            isDefeated = false;

            if (activateObject && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (enemyBrain != null)
            {
                enemyBrain.ResetForCheckpointRestore();
            }
            else
            {
                health?.RestoreFull();
            }

            if (!activateObject)
            {
                gameObject.SetActive(false);
            }
        }

        public void SetClearedState()
        {
            isDefeated = true;

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleDeath()
        {
            if (isDefeated)
            {
                return;
            }

            isDefeated = true;
            ownerEncounter?.NotifyMemberDefeated(this);

            if (disableObjectOnDeath)
            {
                gameObject.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }
    }
}
