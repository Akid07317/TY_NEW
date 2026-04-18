using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossAttackCuePlan
    {
        public BossAttackCuePlan(string cueLabel, string attackName, Color cueAccentColor, float visibleSeconds)
        {
            CueLabel = cueLabel ?? string.Empty;
            AttackName = attackName ?? string.Empty;
            CueAccentColor = cueAccentColor;
            VisibleSeconds = visibleSeconds;
        }

        public string CueLabel { get; }

        public string AttackName { get; }

        public Color CueAccentColor { get; }

        public float VisibleSeconds { get; }
    }

    public static class BossAttackCuePlanner
    {
        private static readonly Color DefaultCueAccentColor = new Color(0.95f, 0.8f, 0.42f);
        private static readonly Color StraightProjectileCueAccentColor = new Color(0.48f, 0.88f, 0.92f);
        private static readonly Color ArcProjectileCueAccentColor = new Color(1f, 0.58f, 0.32f);
        private static readonly Color RangedCueAccentColor = new Color(0.78f, 0.88f, 0.56f);

        public static BossAttackCuePlan Build(
            EnemyBrain bossEnemy,
            BossTelegraphStyleSO telegraphStyle,
            string defaultCueLabel,
            float minimumVisibleSeconds)
        {
            AttackDefinitionSO attack = BossAttackPreviewUtility.PreviewCurrentAttack(bossEnemy);

            return new BossAttackCuePlan(
                ResolveCueLabel(defaultCueLabel, attack),
                ResolveAttackName(attack),
                ResolveCueAccentColor(telegraphStyle, attack),
                attack != null ? Mathf.Max(minimumVisibleSeconds, attack.StartupSeconds) : minimumVisibleSeconds);
        }

        public static Color ResolveDefaultCueAccentColor(BossTelegraphStyleSO telegraphStyle)
        {
            return telegraphStyle != null ? telegraphStyle.DefaultCueAccentColor : DefaultCueAccentColor;
        }

        private static string ResolveCueLabel(string defaultCueLabel, AttackDefinitionSO attack)
        {
            if (attack == null || attack.ProjectilePrefab == null)
            {
                return defaultCueLabel;
            }

            return attack.ProjectileTrajectoryMode switch
            {
                ProjectileTrajectoryMode.Straight => "Line Shot Incoming",
                ProjectileTrajectoryMode.Arc => "Arc Shot Incoming",
                _ => "Ranged Attack Incoming"
            };
        }

        private static string ResolveAttackName(AttackDefinitionSO attack)
        {
            if (attack == null)
            {
                return "Brace";
            }

            if (!string.IsNullOrWhiteSpace(attack.DisplayName))
            {
                return attack.DisplayName;
            }

            return string.IsNullOrWhiteSpace(attack.AttackId) ? "Brace" : attack.AttackId;
        }

        private static Color ResolveCueAccentColor(BossTelegraphStyleSO telegraphStyle, AttackDefinitionSO attack)
        {
            if (attack == null || attack.ProjectilePrefab == null)
            {
                return ResolveDefaultCueAccentColor(telegraphStyle);
            }

            return attack.ProjectileTrajectoryMode switch
            {
                ProjectileTrajectoryMode.Straight => telegraphStyle != null
                    ? telegraphStyle.StraightProjectileCueAccentColor
                    : StraightProjectileCueAccentColor,
                ProjectileTrajectoryMode.Arc => telegraphStyle != null
                    ? telegraphStyle.ArcProjectileCueAccentColor
                    : ArcProjectileCueAccentColor,
                _ => telegraphStyle != null
                    ? telegraphStyle.RangedCueAccentColor
                    : RangedCueAccentColor
            };
        }
    }
}
