using CampusRPG.AI;
using CampusRPG.Combat;

namespace CampusRPG.UI
{
    public static class BossAttackPreviewUtility
    {
        public static AttackDefinitionSO PreviewCurrentAttack(EnemyBrain bossEnemy)
        {
            if (bossEnemy == null || bossEnemy.AttackController == null)
            {
                return null;
            }

            return bossEnemy.AttackController.PreviewAttackForTarget(bossEnemy.CurrentTarget, bossEnemy.Archetype);
        }
    }
}
