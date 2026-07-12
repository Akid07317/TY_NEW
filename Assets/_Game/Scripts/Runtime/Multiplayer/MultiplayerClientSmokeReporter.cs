using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CampusRPG.Multiplayer
{
    [DisallowMultipleComponent]
    internal sealed class MultiplayerClientSmokeReporter : MonoBehaviour
    {
        private const int DefaultIntervalSeconds = 1;
        private string label = "client";
        private float intervalSeconds = DefaultIntervalSeconds;
        private float nextReportAt;

        public void Configure(string smokeLabel, int reportIntervalSeconds)
        {
            label = SanitizeToken(string.IsNullOrWhiteSpace(smokeLabel) ? "client" : smokeLabel);
            intervalSeconds = Mathf.Clamp(reportIntervalSeconds, 1, 60);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextReportAt)
            {
                return;
            }

            Report();
            nextReportAt = Time.unscaledTime + intervalSeconds;
        }

        private void Report()
        {
            NetworkPlayerAvatar[] avatars = FindObjectsByType<NetworkPlayerAvatar>(FindObjectsSortMode.None);
            Array.Sort(avatars, (left, right) => left.OwnerClientId.CompareTo(right.OwnerClientId));
            NetworkEnemyAvatar[] enemies = FindObjectsByType<NetworkEnemyAvatar>(FindObjectsSortMode.None);
            Array.Sort(enemies, (left, right) => left.EnemyId.CompareTo(right.EnemyId));

            int avatarCount = 0;
            int ownedCount = 0;
            int remoteCount = 0;
            int enemyCount = 0;
            StringBuilder avatarSummary = new StringBuilder();
            StringBuilder healthSummary = new StringBuilder();
            StringBuilder deathSummary = new StringBuilder();
            StringBuilder enemySummary = new StringBuilder();
            StringBuilder enemyHealthSummary = new StringBuilder();
            StringBuilder enemyDeathSummary = new StringBuilder();
            StringBuilder enemyFormalAttackSummary = new StringBuilder();
            StringBuilder enemyFormalDeathSummary = new StringBuilder();
            StringBuilder enemyFormalDriverSummary = new StringBuilder();
            StringBuilder formalAttackSummary = new StringBuilder();
            StringBuilder formalHitSummary = new StringBuilder();
            StringBuilder formalDeathSummary = new StringBuilder();
            StringBuilder formalDriverSummary = new StringBuilder();

            for (int i = 0; i < avatars.Length; i++)
            {
                NetworkPlayerAvatar avatar = avatars[i];

                if (avatar == null || !avatar.IsSpawned)
                {
                    continue;
                }

                avatarCount++;

                bool isOwner = avatar.IsOwner;
                if (isOwner)
                {
                    ownedCount++;
                }
                else
                {
                    remoteCount++;
                }

                if (avatarSummary.Length > 0)
                {
                    avatarSummary.Append('|');
                }

                Vector3 position = avatar.transform.position;
                avatarSummary
                    .Append(avatar.OwnerClientId)
                    .Append(isOwner ? ":local:" : ":remote:")
                    .Append(position.x.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(position.y.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(position.z.ToString("0.00", CultureInfo.InvariantCulture));

                if (healthSummary.Length > 0)
                {
                    healthSummary.Append('|');
                }

                healthSummary
                    .Append(avatar.OwnerClientId)
                    .Append(isOwner ? ":local:" : ":remote:")
                    .Append(avatar.CurrentHealth.ToString(CultureInfo.InvariantCulture));

                if (deathSummary.Length > 0)
                {
                    deathSummary.Append('|');
                }

                deathSummary
                    .Append(avatar.OwnerClientId)
                    .Append(isOwner ? ":local:" : ":remote:")
                    .Append(avatar.IsDead ? "true" : "false");

                NetworkPlayerPresentationBridge presentationBridge =
                    avatar.GetComponentInChildren<NetworkPlayerPresentationBridge>(true);

                if (formalAttackSummary.Length > 0)
                {
                    formalAttackSummary.Append('|');
                }

                formalAttackSummary
                    .Append(avatar.OwnerClientId)
                    .Append(isOwner ? ":local:" : ":remote:")
                    .Append(presentationBridge != null && presentationBridge.HasObservedFormalAttackPresentation
                        ? "true"
                        : "false");

                if (formalHitSummary.Length > 0)
                {
                    formalHitSummary.Append('|');
                }

                formalHitSummary
                    .Append(avatar.OwnerClientId)
                    .Append(isOwner ? ":local:" : ":remote:")
                    .Append(presentationBridge != null && presentationBridge.HasObservedFormalHitReaction
                        ? "true"
                        : "false");

                if (formalDeathSummary.Length > 0)
                {
                    formalDeathSummary.Append('|');
                }

                formalDeathSummary
                    .Append(avatar.OwnerClientId)
                    .Append(isOwner ? ":local:" : ":remote:")
                    .Append(presentationBridge != null && presentationBridge.IsFormalDeathStateActive ? "true" : "false");

                if (formalDriverSummary.Length > 0)
                {
                    formalDriverSummary.Append('|');
                }

                formalDriverSummary
                    .Append(avatar.OwnerClientId)
                    .Append(isOwner ? ":local:" : ":remote:")
                    .Append(presentationBridge != null && presentationBridge.LocalPlayerDriverSuppressed ? "suppressed" : "active");
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                NetworkEnemyAvatar enemy = enemies[i];

                if (enemy == null || !enemy.IsSpawned)
                {
                    continue;
                }

                enemyCount++;

                if (enemySummary.Length > 0)
                {
                    enemySummary.Append('|');
                }

                Vector3 enemyPosition = enemy.transform.position;
                enemySummary
                    .Append(enemy.EnemyId)
                    .Append(":network:")
                    .Append(enemyPosition.x.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(enemyPosition.y.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(enemyPosition.z.ToString("0.00", CultureInfo.InvariantCulture));

                if (enemyHealthSummary.Length > 0)
                {
                    enemyHealthSummary.Append('|');
                }

                enemyHealthSummary
                    .Append(enemy.EnemyId)
                    .Append(":network:")
                    .Append(enemy.CurrentHealth.ToString(CultureInfo.InvariantCulture));

                if (enemyDeathSummary.Length > 0)
                {
                    enemyDeathSummary.Append('|');
                }

                enemyDeathSummary
                    .Append(enemy.EnemyId)
                    .Append(":network:")
                    .Append(enemy.IsDead ? "true" : "false");

                NetworkEnemyPresentationBridge enemyPresentationBridge =
                    enemy.GetComponentInChildren<NetworkEnemyPresentationBridge>(true);

                if (enemyFormalAttackSummary.Length > 0)
                {
                    enemyFormalAttackSummary.Append('|');
                }

                enemyFormalAttackSummary
                    .Append(enemy.EnemyId)
                    .Append(":network:")
                    .Append(enemyPresentationBridge != null
                            && enemyPresentationBridge.HasObservedFormalAttackPresentation
                        ? "true"
                        : "false");

                if (enemyFormalDeathSummary.Length > 0)
                {
                    enemyFormalDeathSummary.Append('|');
                }

                enemyFormalDeathSummary
                    .Append(enemy.EnemyId)
                    .Append(":network:")
                    .Append(enemyPresentationBridge != null && enemyPresentationBridge.IsFormalDeathStateActive
                        ? "true"
                        : "false");

                if (enemyFormalDriverSummary.Length > 0)
                {
                    enemyFormalDriverSummary.Append('|');
                }

                enemyFormalDriverSummary
                    .Append(enemy.EnemyId)
                    .Append(":network:")
                    .Append(enemyPresentationBridge != null && enemyPresentationBridge.LocalEnemyDriverSuppressed
                        ? "suppressed"
                        : "active");
            }

            Debug.Log(
                "[MultiplayerSmoke]"
                + $" label={label}"
                + $" elapsed={Time.unscaledTime.ToString("0.0", CultureInfo.InvariantCulture)}"
                + $" avatarCount={avatarCount}"
                + $" owned={ownedCount}"
                + $" remote={remoteCount}"
                + $" avatars={avatarSummary}"
                + $" healths={healthSummary}"
                + $" deaths={deathSummary}"
                + $" enemyCount={enemyCount}"
                + $" enemies={enemySummary}"
                + $" enemyHealths={enemyHealthSummary}"
                + $" enemyDeaths={enemyDeathSummary}"
                + $" enemyFormalAttacks={enemyFormalAttackSummary}"
                + $" enemyFormalDeaths={enemyFormalDeathSummary}"
                + $" enemyFormalDrivers={enemyFormalDriverSummary}"
                + $" formalAttacks={formalAttackSummary}"
                + $" formalHits={formalHitSummary}"
                + $" formalDeaths={formalDeathSummary}"
                + $" formalDrivers={formalDriverSummary}");
        }

        private static string SanitizeToken(string value)
        {
            return value.Trim().Replace(' ', '_');
        }
    }
}
