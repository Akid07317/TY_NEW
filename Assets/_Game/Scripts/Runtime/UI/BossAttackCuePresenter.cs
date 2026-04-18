using CampusRPG.AI;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossAttackCuePresenter : MonoBehaviour
    {
        private static readonly Color FallbackCuePanelBackgroundColor = new Color(0.14f, 0.08f, 0.08f, 0.95f);
        private static readonly Color InitialCueAccentColor = new Color(0.95f, 0.8f, 0.42f);
        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private BossTelegraphStyleSO telegraphStyle;
        [SerializeField] private string cueLabel = "Incoming Attack";
        [SerializeField] private float minimumVisibleSeconds = 0.5f;

        private bool isVisible;
        private float visibleTimer;
        private string currentCueLabel = string.Empty;
        private string currentAttackName = string.Empty;
        private Color currentCueAccentColor = InitialCueAccentColor;
        private string lastStateName = string.Empty;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle nameStyle;
        private Texture2D panelTexture;
        private Color appliedPanelColor = Color.clear;

        public bool IsVisible => isVisible;

        public float RemainingVisibleSeconds => visibleTimer;

        public string CurrentCueLabel => currentCueLabel;

        public string CurrentAttackName => currentAttackName;

        public Color CurrentCueAccentColor => currentCueAccentColor;

        public void Configure(EnemyBrain configuredBossEnemy, BossTelegraphStyleSO configuredTelegraphStyle = null)
        {
            bossEnemy = configuredBossEnemy;

            if (configuredTelegraphStyle != null || telegraphStyle == null)
            {
                telegraphStyle = configuredTelegraphStyle;
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

            const float width = 360f;
            const float height = 72f;
            Rect panelRect = new Rect((Screen.width - width) * 0.5f, 82f, width, height);
            labelStyle.normal.textColor = currentCueAccentColor;
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 10f, panelRect.width, 20f), currentCueLabel, labelStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 26f, panelRect.width, 28f), currentAttackName, nameStyle);
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
            if (!BossPresentationRules.IsBossEligible(bossEnemy))
            {
                ResetRuntimeState();
                return;
            }

            string currentStateName = bossEnemy.StateMachine != null ? bossEnemy.StateMachine.CurrentStateName : string.Empty;

            if (currentStateName != lastStateName)
            {
                if (currentStateName == nameof(EnemyAttackState))
                {
                    ShowCueForCurrentAttack();
                }

                lastStateName = currentStateName;
            }

            if (!isVisible)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Mathf.Max(0f, deltaTime));
            isVisible = visibleTimer > 0f;
        }

        private void ShowCueForCurrentAttack()
        {
            BossAttackCuePlan plan = BossAttackCuePlanner.Build(
                bossEnemy,
                telegraphStyle,
                cueLabel,
                minimumVisibleSeconds);
            currentCueLabel = plan.CueLabel;
            currentAttackName = plan.AttackName;
            currentCueAccentColor = plan.CueAccentColor;
            visibleTimer = plan.VisibleSeconds;
            isVisible = visibleTimer > 0f;
        }

        private void ResetRuntimeState()
        {
            isVisible = false;
            visibleTimer = 0f;
            currentCueLabel = cueLabel;
            currentAttackName = string.Empty;
            currentCueAccentColor = BossAttackCuePlanner.ResolveDefaultCueAccentColor(telegraphStyle);
            lastStateName = string.Empty;
        }

        private Color ResolveCuePanelBackgroundColor()
        {
            return telegraphStyle != null ? telegraphStyle.CuePanelBackgroundColor : FallbackCuePanelBackgroundColor;
        }

        private void EnsureStyles()
        {
            Color panelColor = ResolveCuePanelBackgroundColor();

            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (panelTexture != null && panelColor != appliedPanelColor)
            {
                panelTexture.SetPixel(0, 0, panelColor);
                panelTexture.Apply();
                appliedPanelColor = panelColor;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = BossAttackCuePlanner.ResolveDefaultCueAccentColor(telegraphStyle);
            }

            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                nameStyle.normal.textColor = Color.white;
            }
        }
    }
}
