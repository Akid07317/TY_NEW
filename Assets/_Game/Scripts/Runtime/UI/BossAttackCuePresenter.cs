using System;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Combat;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossAttackCueLayout
    {
        public BossAttackCueLayout(Rect panelRect, Rect labelRect, Rect attackNameRect, Rect responseHintRect)
        {
            PanelRect = panelRect;
            LabelRect = labelRect;
            AttackNameRect = attackNameRect;
            ResponseHintRect = responseHintRect;
        }

        public Rect PanelRect { get; }

        public Rect LabelRect { get; }

        public Rect AttackNameRect { get; }

        public Rect ResponseHintRect { get; }
    }

    public static class BossAttackCueLayoutUtility
    {
        private const float HorizontalMargin = 12f;
        private const float MaxWidth = 390f;
        private const float MinWidth = 260f;
        private const float Height = 88f;
        private const float TopOffset = 82f;
        private const float VerticalMargin = 12f;
        private const float MinimumHudGap = 10f;

        public static BossAttackCueLayout Build(float screenWidth)
        {
            return Build(screenWidth, 720f);
        }

        public static BossAttackCueLayout Build(float screenWidth, float screenHeight)
        {
            float availableWidth = Mathf.Max(1f, screenWidth - HorizontalMargin * 2f);
            float minimumWidth = Mathf.Min(MinWidth, availableWidth);
            float width = Mathf.Clamp(availableWidth, minimumWidth, MaxWidth);
            SwordArtHudLayout swordArtLayout = SwordArtHudLayoutUtility.Build(screenWidth, screenHeight);
            float safeMaxY = Mathf.Min(
                screenHeight - Height - VerticalMargin,
                swordArtLayout.PanelRect.yMin - Height - MinimumHudGap);
            safeMaxY = Mathf.Max(VerticalMargin, safeMaxY);
            float top = Mathf.Clamp(TopOffset, VerticalMargin, safeMaxY);
            Rect panelRect = new Rect((screenWidth - width) * 0.5f, top, width, Height);

            return new BossAttackCueLayout(
                panelRect,
                new Rect(panelRect.x, panelRect.y + 10f, panelRect.width, 20f),
                new Rect(panelRect.x + 12f, panelRect.y + 26f, panelRect.width - 24f, 28f),
                new Rect(panelRect.x + 16f, panelRect.y + 58f, panelRect.width - 32f, 20f));
        }
    }

    public static class BossAttackCueTextUtility
    {
        private const int MinimumAttackNameCharacterBudget = 8;
        private const int MinimumResponseHintCharacterBudget = 12;
        private const float ApproximateAttackNameCharacterWidthScale = 0.58f;
        private const float ApproximateResponseHintCharacterWidthScale = 0.52f;

        public static string BuildAttackNameLine(string attackName, float rectWidth, int fontSize)
        {
            string normalizedName = NormalizeWhitespace(attackName);

            if (string.IsNullOrEmpty(normalizedName))
            {
                return string.Empty;
            }

            return ClampWithMiddleEllipsis(
                normalizedName,
                CalculateAttackNameCharacterBudget(rectWidth, fontSize));
        }

        public static int CalculateAttackNameCharacterBudget(float rectWidth, int fontSize)
        {
            float safeFontSize = Mathf.Max(1f, fontSize);
            float safeWidth = Mathf.Max(1f, rectWidth);
            return Mathf.Max(
                MinimumAttackNameCharacterBudget,
                Mathf.FloorToInt(safeWidth / (safeFontSize * ApproximateAttackNameCharacterWidthScale)));
        }

        public static string BuildResponseHintLine(string responseHint, float rectWidth, int fontSize)
        {
            string normalizedHint = NormalizeWhitespace(responseHint);

            if (string.IsNullOrEmpty(normalizedHint))
            {
                return string.Empty;
            }

            string compactHint = BuildKnownCompactResponseHint(normalizedHint);

            return ClampWithMiddleEllipsis(
                compactHint,
                CalculateResponseHintCharacterBudget(rectWidth, fontSize));
        }

        public static int CalculateResponseHintCharacterBudget(float rectWidth, int fontSize)
        {
            float safeFontSize = Mathf.Max(1f, fontSize);
            float safeWidth = Mathf.Max(1f, rectWidth);
            return Mathf.Max(
                MinimumResponseHintCharacterBudget,
                Mathf.FloorToInt(safeWidth / (safeFontSize * ApproximateResponseHintCharacterWidthScale)));
        }

        private static string NormalizeWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(
                " ",
                value.Trim().Split(
                    new[] { ' ', '\t', '\n', '\r' },
                    StringSplitOptions.RemoveEmptyEntries));
        }

        private static string ClampWithMiddleEllipsis(string value, int characterBudget)
        {
            if (value.Length <= characterBudget)
            {
                return value;
            }

            if (characterBudget <= 3)
            {
                return value.Substring(0, characterBudget);
            }

            int remainingCharacters = characterBudget - 3;
            int headLength = Mathf.Max(1, remainingCharacters / 2);
            int tailLength = Mathf.Max(1, remainingCharacters - headLength);

            if (headLength + tailLength + 3 > characterBudget)
            {
                tailLength = Mathf.Max(0, characterBudget - 3 - headLength);
            }

            return string.Concat(
                value.Substring(0, headLength),
                "...",
                value.Substring(value.Length - tailLength, tailLength));
        }

        private static string BuildKnownCompactResponseHint(string responseHint)
        {
            switch (responseHint)
            {
                case "Land or guard; avoid air hang":
                    return "Land/guard; avoid air";
                case "Delay dodge; lane catches rolls":
                    return "Delay dodge; lane";
                case "Dodge heavy; guard breaks":
                    return "Dodge; guard breaks";
                case "Sidestep line shot":
                    return "Sidestep shot";
                case "Leave marked impact":
                    return "Leave impact mark";
                case "Interrupt or guard":
                    return "Interrupt/guard";
                default:
                    return responseHint;
            }
        }
    }

    public static class BossAttackCueStyleUtility
    {
        public static GUIStyle BuildCueLabelStyle(GUIStyle baseStyle, Color textColor)
        {
            GUIStyle style = new GUIStyle(baseStyle ?? new GUIStyle())
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            style.normal.textColor = textColor;
            return style;
        }

        public static GUIStyle BuildAttackNameStyle(GUIStyle baseStyle)
        {
            GUIStyle style = new GUIStyle(baseStyle ?? new GUIStyle())
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            style.normal.textColor = Color.white;
            return style;
        }

        public static GUIStyle BuildResponseHintStyle(GUIStyle baseStyle)
        {
            GUIStyle style = new GUIStyle(baseStyle ?? new GUIStyle())
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            style.normal.textColor = new Color(0.88f, 0.92f, 0.94f);
            return style;
        }
    }

    [DisallowMultipleComponent]
    public sealed class BossAttackCuePresenter : MonoBehaviour
    {
        private static readonly Color FallbackCuePanelBackgroundColor = new Color(0.14f, 0.08f, 0.08f, 0.95f);
        private static readonly Color InitialCueAccentColor = new Color(0.95f, 0.8f, 0.42f);
        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private BossTelegraphStyleSO telegraphStyle;
        [SerializeField] private ThirdPersonCameraController cameraController;
        [SerializeField] private string cueLabel = "Incoming Attack";
        [SerializeField] private float minimumVisibleSeconds = 0.5f;

        private bool isVisible;
        private float visibleTimer;
        private string currentCueLabel = string.Empty;
        private string currentAttackName = string.Empty;
        private string currentResponseHint = string.Empty;
        private Color currentCueAccentColor = InitialCueAccentColor;
        private string lastStateName = string.Empty;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle nameStyle;
        private GUIStyle hintStyle;
        private Texture2D panelTexture;
        private Color appliedPanelColor = Color.clear;

        public bool IsVisible => isVisible;

        public float RemainingVisibleSeconds => visibleTimer;

        public string CurrentCueLabel => currentCueLabel;

        public string CurrentAttackName => currentAttackName;

        public string CurrentResponseHint => currentResponseHint;

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

            BossAttackCueLayout layout = BossAttackCueLayoutUtility.Build(Screen.width, Screen.height);
            labelStyle.normal.textColor = currentCueAccentColor;
            GUI.Box(layout.PanelRect, GUIContent.none, panelStyle);
            GUI.Label(layout.LabelRect, currentCueLabel, labelStyle);
            GUI.Label(
                layout.AttackNameRect,
                BossAttackCueTextUtility.BuildAttackNameLine(
                    currentAttackName,
                    layout.AttackNameRect.width,
                    nameStyle.fontSize),
                nameStyle);
            GUI.Label(
                layout.ResponseHintRect,
                BossAttackCueTextUtility.BuildResponseHintLine(
                    currentResponseHint,
                    layout.ResponseHintRect.width,
                    hintStyle.fontSize),
                hintStyle);
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
            AttackDefinitionSO attack = BossAttackPreviewUtility.PreviewCurrentAttack(bossEnemy);
            BossAttackCuePlan plan = BossAttackCuePlanner.Build(
                bossEnemy,
                telegraphStyle,
                cueLabel,
                minimumVisibleSeconds);
            currentCueLabel = plan.CueLabel;
            currentAttackName = plan.AttackName;
            currentResponseHint = plan.ResponseHint;
            currentCueAccentColor = plan.CueAccentColor;
            visibleTimer = plan.VisibleSeconds;
            isVisible = visibleTimer > 0f;
            TriggerResponseCameraFeedback(attack);
            TriggerResponseAudioFeedback(attack);
        }

        private void ResetRuntimeState()
        {
            isVisible = false;
            visibleTimer = 0f;
            currentCueLabel = cueLabel;
            currentAttackName = string.Empty;
            currentResponseHint = string.Empty;
            currentCueAccentColor = BossAttackCuePlanner.ResolveDefaultCueAccentColor(telegraphStyle);
            lastStateName = string.Empty;
        }

        private Color ResolveCuePanelBackgroundColor()
        {
            return telegraphStyle != null ? telegraphStyle.CuePanelBackgroundColor : FallbackCuePanelBackgroundColor;
        }

        private void TriggerResponseCameraFeedback(AttackDefinitionSO attack)
        {
            cameraController = SceneRuntimeReferenceUtility.ResolveCameraController(cameraController);
            ActionCameraFeedbackUtility.TryRequestImpulse(
                cameraController,
                bossEnemy != null ? bossEnemy.transform : transform,
                ActionCameraFeedbackUtility.ResolveEnemyResponseImpulse(attack));
        }

        private void TriggerResponseAudioFeedback(AttackDefinitionSO attack)
        {
            Transform source = bossEnemy != null ? bossEnemy.transform : transform;
            ProceduralAudioUtility.TryPlayActionCue(
                source.position,
                ProceduralAudioUtility.ResolveEnemyResponseCue(attack));
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
                labelStyle = BossAttackCueStyleUtility.BuildCueLabelStyle(
                    GUI.skin.label,
                    BossAttackCuePlanner.ResolveDefaultCueAccentColor(telegraphStyle));
            }

            if (nameStyle == null)
            {
                nameStyle = BossAttackCueStyleUtility.BuildAttackNameStyle(GUI.skin.label);
            }

            if (hintStyle == null)
            {
                hintStyle = BossAttackCueStyleUtility.BuildResponseHintStyle(GUI.skin.label);
            }
        }
    }
}
