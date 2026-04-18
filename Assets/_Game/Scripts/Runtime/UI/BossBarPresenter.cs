using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossBarPresenter : MonoBehaviour
    {
        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private string bossDisplayName = "Campus Gatekeeper";

        private bool isVisible;
        private float currentFillNormalized;
        private GUIStyle containerStyle;
        private GUIStyle fillStyle;
        private GUIStyle nameStyle;
        private GUIStyle valueStyle;
        private Texture2D containerTexture;
        private Texture2D fillTexture;

        public bool IsVisible => isVisible;

        public float CurrentFillNormalized => currentFillNormalized;

        public void Configure(EnemyBrain configuredBossEnemy, string configuredBossDisplayName)
        {
            bossEnemy = configuredBossEnemy;

            if (!string.IsNullOrWhiteSpace(configuredBossDisplayName))
            {
                bossDisplayName = configuredBossDisplayName;
            }
        }

        private void Update()
        {
            SyncState();
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            EnsureStyles();

            const float width = 640f;
            const float height = 24f;
            Rect containerRect = new Rect((Screen.width - width) * 0.5f, 28f, width, height);
            GUI.Box(containerRect, GUIContent.none, containerStyle);

            float fillWidth = Mathf.Max(0f, (width - 8f) * currentFillNormalized);
            Rect fillRect = new Rect(containerRect.x + 4f, containerRect.y + 4f, fillWidth, height - 8f);
            GUI.Box(fillRect, GUIContent.none, fillStyle);

            GUI.Label(new Rect(containerRect.x, containerRect.y - 34f, width, 28f), bossDisplayName, nameStyle);

            HealthComponent health = bossEnemy != null ? bossEnemy.Health : null;

            if (health != null)
            {
                GUI.Label(
                    new Rect(containerRect.x, containerRect.y + 28f, width, 24f),
                    $"{health.CurrentValue:0} / {health.MaxValue:0}",
                    valueStyle);
            }
        }

        private void OnDestroy()
        {
            if (containerTexture != null)
            {
                Destroy(containerTexture);
                containerTexture = null;
            }

            if (fillTexture != null)
            {
                Destroy(fillTexture);
                fillTexture = null;
            }
        }

        private void SyncState()
        {
            if (bossEnemy == null)
            {
                isVisible = false;
                currentFillNormalized = 0f;
                return;
            }

            if (!bossEnemy.gameObject.activeInHierarchy)
            {
                isVisible = false;
                currentFillNormalized = 0f;
                return;
            }

            if (bossEnemy.Archetype == null || bossEnemy.Archetype.ArchetypeType != EnemyArchetypeType.Boss)
            {
                isVisible = false;
                currentFillNormalized = 0f;
                return;
            }

            HealthComponent health = bossEnemy.Health;

            if (health == null || health.IsDead)
            {
                isVisible = false;
                currentFillNormalized = 0f;
                return;
            }

            isVisible = true;
            currentFillNormalized = health.MaxValue > Mathf.Epsilon
                ? Mathf.Clamp01(health.CurrentValue / health.MaxValue)
                : 0f;
        }

        private void EnsureStyles()
        {
            if (containerStyle == null)
            {
                containerTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                containerTexture.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.1f, 0.95f));
                containerTexture.Apply();

                containerStyle = new GUIStyle(GUI.skin.box);
                containerStyle.normal.background = containerTexture;
                containerStyle.border = new RectOffset(6, 6, 6, 6);
            }

            if (fillStyle == null)
            {
                fillTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                fillTexture.SetPixel(0, 0, new Color(0.72f, 0.12f, 0.14f, 1f));
                fillTexture.Apply();

                fillStyle = new GUIStyle(GUI.skin.box);
                fillStyle.normal.background = fillTexture;
                fillStyle.border = new RectOffset(4, 4, 4, 4);
            }

            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                nameStyle.normal.textColor = new Color(0.96f, 0.94f, 0.85f);
            }

            if (valueStyle == null)
            {
                valueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                valueStyle.normal.textColor = Color.white;
            }
        }
    }
}
