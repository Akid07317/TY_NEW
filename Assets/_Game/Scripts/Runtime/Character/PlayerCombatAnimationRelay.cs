using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Character
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatAnimationRelay : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private Animator animator;
        [SerializeField] private int baseLayerIndex;
        [SerializeField] private float crossFadeSeconds = 0.05f;

        private void Awake()
        {
            if (combatController == null)
            {
                combatController = GetComponent<PlayerCombatController>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        public void PlayAttack(AttackDefinitionSO attackDefinition)
        {
            if (animator == null || attackDefinition == null || string.IsNullOrWhiteSpace(attackDefinition.AnimationStateName))
            {
                return;
            }

            animator.CrossFadeInFixedTime(attackDefinition.AnimationStateName, Mathf.Max(0f, crossFadeSeconds), baseLayerIndex);
        }

        public void AnimationEvent_OpenAttackHitbox()
        {
            combatController?.ActivatePreparedHitboxFromAnimationEvent();
        }

        public void AnimationEvent_CloseAttackHitbox()
        {
            combatController?.ClearPreparedHitboxFromAnimationEvent();
        }
    }
}
