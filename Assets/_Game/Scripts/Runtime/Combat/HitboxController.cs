using UnityEngine;

namespace CampusRPG.Combat
{
    public sealed class HitboxController : MonoBehaviour
    {
        [SerializeField] private AttackExecutor attackExecutor;

        private AttackDefinitionSO currentAttack;
        private float currentAttackPower;
        private GameObject currentSource;
        private bool activationWindowOpen;
        private bool hasExecutedCurrentAttack;

        private void Awake()
        {
            if (attackExecutor == null)
            {
                attackExecutor = GetComponent<AttackExecutor>();
            }
        }

        public void Prepare(AttackDefinitionSO attack, float attackPower, GameObject source)
        {
            currentAttack = attack;
            currentAttackPower = attackPower;
            currentSource = source;
            activationWindowOpen = false;
            hasExecutedCurrentAttack = false;
        }

        public void OpenActivationWindow()
        {
            if (currentAttack == null)
            {
                return;
            }

            activationWindowOpen = true;
        }

        public void CloseActivationWindow()
        {
            activationWindowOpen = false;
        }

        public bool Activate()
        {
            if (hasExecutedCurrentAttack || currentAttack == null || attackExecutor == null || !activationWindowOpen)
            {
                return false;
            }

            hasExecutedCurrentAttack = true;
            attackExecutor.Execute(currentAttack, currentAttackPower, currentSource);
            return true;
        }

        public void Clear()
        {
            currentAttack = null;
            currentAttackPower = 0f;
            currentSource = null;
            activationWindowOpen = false;
            hasExecutedCurrentAttack = false;
        }
    }
}
