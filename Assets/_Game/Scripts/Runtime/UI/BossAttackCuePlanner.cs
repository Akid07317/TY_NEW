using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossAttackCuePlan
    {
        public BossAttackCuePlan(
            string cueLabel,
            string attackName,
            string responseHint,
            Color cueAccentColor,
            float visibleSeconds)
        {
            CueLabel = cueLabel ?? string.Empty;
            AttackName = attackName ?? string.Empty;
            ResponseHint = responseHint ?? string.Empty;
            CueAccentColor = cueAccentColor;
            VisibleSeconds = visibleSeconds;
        }

        public string CueLabel { get; }

        public string AttackName { get; }

        public string ResponseHint { get; }

        public Color CueAccentColor { get; }

        public float VisibleSeconds { get; }
    }

    public static class BossAttackCuePlanner
    {
        private static readonly Color DefaultCueAccentColor = new Color(0.95f, 0.8f, 0.42f);
        private static readonly Color StraightProjectileCueAccentColor = new Color(0.48f, 0.88f, 0.92f);
        private static readonly Color ArcProjectileCueAccentColor = new Color(1f, 0.58f, 0.32f);
        private static readonly Color RangedCueAccentColor = new Color(0.78f, 0.88f, 0.56f);
        private static readonly Color AntiAirCueAccentColor = new Color(0.42f, 0.72f, 1f);
        private static readonly Color ChaseRollCueAccentColor = new Color(1f, 0.42f, 0.24f);

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
                ResolveResponseHint(attack),
                ResolveCueAccentColor(telegraphStyle, attack),
                attack != null ? Mathf.Max(minimumVisibleSeconds, attack.StartupSeconds) : minimumVisibleSeconds);
        }

        public static Color ResolveDefaultCueAccentColor(BossTelegraphStyleSO telegraphStyle)
        {
            return telegraphStyle != null ? telegraphStyle.DefaultCueAccentColor : DefaultCueAccentColor;
        }

        private static string ResolveCueLabel(string defaultCueLabel, AttackDefinitionSO attack)
        {
            if (attack == null)
            {
                return defaultCueLabel;
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.AntiAir)
            {
                return "Anti-Air Incoming";
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.ChaseRoll)
            {
                return "Roll Catch Incoming";
            }

            if (attack.BreaksGuard)
            {
                return "Guard Break Incoming";
            }

            if (attack.ProjectilePrefab == null)
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

        private static string ResolveResponseHint(AttackDefinitionSO attack)
        {
            if (attack == null)
            {
                return "Watch, then answer";
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.AntiAir)
            {
                return "Land or guard; avoid air hang";
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.ChaseRoll)
            {
                return "Delay dodge; lane catches rolls";
            }

            if (attack.BreaksGuard)
            {
                return "Dodge heavy; guard breaks";
            }

            if (attack.ProjectilePrefab != null)
            {
                return attack.ProjectileTrajectoryMode switch
                {
                    ProjectileTrajectoryMode.Straight => "Sidestep line shot",
                    ProjectileTrajectoryMode.Arc => "Leave marked impact",
                    _ => "Move before shot lands"
                };
            }

            return "Block or step out";
        }

        private static Color ResolveCueAccentColor(BossTelegraphStyleSO telegraphStyle, AttackDefinitionSO attack)
        {
            if (attack == null)
            {
                return ResolveDefaultCueAccentColor(telegraphStyle);
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.AntiAir)
            {
                return telegraphStyle != null ? telegraphStyle.AntiAirCueAccentColor : AntiAirCueAccentColor;
            }

            if (attack.EnemyTargetResponse == EnemyTargetResponseType.ChaseRoll)
            {
                return telegraphStyle != null ? telegraphStyle.ChaseRollCueAccentColor : ChaseRollCueAccentColor;
            }

            if (attack.ProjectilePrefab == null)
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
