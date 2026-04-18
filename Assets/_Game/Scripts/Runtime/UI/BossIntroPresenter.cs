using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossIntroPresenter : MonoBehaviour
    {
        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private string encounterLabel = "Boss Encounter";
        [SerializeField] private string bossDisplayName = "Campus Gatekeeper";
        [SerializeField] private float visibleDurationSeconds = 2.1f;

        private bool isVisible;
        private bool wasBossActiveLastFrame;
        private bool hasShownForCurrentActivation;
        private float visibleTimer;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle nameStyle;
        private Texture2D panelTexture;

        public bool IsVisible => isVisible;

        public float RemainingVisibleSeconds => visibleTimer;

        public void Configure(EnemyBrain configuredBossEnemy, string configuredEncounterLabel, string configuredBossDisplayName)
        {
            bossEnemy = configuredBossEnemy;

            if (!string.IsNullOrWhiteSpace(configuredEncounterLabel))
            {
                encounterLabel = configuredEncounterLabel;
            }

            if (!string.IsNullOrWhiteSpace(configuredBossDisplayName))
            {
                bossDisplayName = configuredBossDisplayName;
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            EnsureStyles();

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, GetCurrentAlpha());

            const float width = 520f;
            const float height = 94f;
            Rect panelRect = new Rect((Screen.width - width) * 0.5f, Mathf.Max(52f, Screen.height * 0.14f), width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);

            GUI.Label(new Rect(panelRect.x, panelRect.y + 16f, panelRect.width, 24f), encounterLabel, labelStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 36f, panelRect.width, 34f), bossDisplayName, nameStyle);

            GUI.color = previousColor;
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
                panelTexture = null;
            }
        }

        private void Tick(float deltaTime)
        {
            bool isBossActive = IsBossActive();

            if (!isBossActive)
            {
                isVisible = false;
                visibleTimer = 0f;
                hasShownForCurrentActivation = false;
                wasBossActiveLastFrame = false;
                return;
            }

            if (!wasBossActiveLastFrame || !hasShownForCurrentActivation)
            {
                hasShownForCurrentActivation = true;
                wasBossActiveLastFrame = true;
                visibleTimer = Mathf.Max(0f, visibleDurationSeconds);
                isVisible = visibleTimer > 0f;
                return;
            }

            wasBossActiveLastFrame = true;

            if (!isVisible)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Mathf.Max(0f, deltaTime));
            isVisible = visibleTimer > 0f;
        }

        private bool IsBossActive()
        {
            if (bossEnemy == null || !bossEnemy.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (bossEnemy.Archetype == null || bossEnemy.Archetype.ArchetypeType != EnemyArchetypeType.Boss)
            {
                return false;
            }

            HealthComponent health = bossEnemy.Health;
            return health != null && !health.IsDead;
        }

        private float GetCurrentAlpha()
        {
            if (visibleDurationSeconds <= Mathf.Epsilon)
            {
                return 0f;
            }

            float fadeWindow = Mathf.Min(0.28f, visibleDurationSeconds * 0.5f);

            if (fadeWindow <= Mathf.Epsilon)
            {
                return 1f;
            }

            float fadeIn = Mathf.Clamp01((visibleDurationSeconds - visibleTimer) / fadeWindow);
            float fadeOut = Mathf.Clamp01(visibleTimer / fadeWindow);
            return Mathf.Min(fadeIn, fadeOut);
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.94f));
                panelTexture.Apply();

                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = new Color(0.92f, 0.73f, 0.34f);
            }

            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                nameStyle.normal.textColor = Color.white;
            }
        }
    }
}
