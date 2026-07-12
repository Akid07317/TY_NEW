using System;
using UnityEngine;

namespace CampusRPG.Multiplayer
{
    public readonly struct NetworkServerAttackProfile
    {
        public const string Light01AttackId = "Light_01";

        public NetworkServerAttackProfile(
            string attackId,
            int damage,
            float range,
            float halfAngleDegrees,
            float cooldownSeconds)
        {
            AttackId = string.IsNullOrWhiteSpace(attackId) ? Light01AttackId : attackId;
            Damage = Mathf.Max(0, damage);
            Range = Mathf.Max(0f, range);
            HalfAngleDegrees = Mathf.Clamp(halfAngleDegrees, 0f, 180f);
            CooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        }

        public string AttackId { get; }

        public int Damage { get; }

        public float Range { get; }

        public float HalfAngleDegrees { get; }

        public float CooldownSeconds { get; }

        public static NetworkServerAttackProfile Light01 =>
            new NetworkServerAttackProfile(Light01AttackId, 25, 2.25f, 100f, 0.4f);

        public static bool TryResolve(string attackId, out NetworkServerAttackProfile profile)
        {
            if (string.Equals(attackId, Light01AttackId, StringComparison.OrdinalIgnoreCase))
            {
                profile = Light01;
                return true;
            }

            profile = default;
            return false;
        }

        public static int ResolveServerDamage(string attackId, int requestedDamage)
        {
            if (requestedDamage <= 0 || !TryResolve(attackId, out NetworkServerAttackProfile profile))
            {
                return 0;
            }

            return profile.Damage;
        }
    }
}
