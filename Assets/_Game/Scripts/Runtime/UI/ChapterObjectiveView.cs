using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct ChapterObjectivePlan
    {
        public ChapterObjectivePlan(string heading, string body, bool isVisible, bool shouldHighlightOnChange = true)
        {
            Heading = heading ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
            ShouldHighlightOnChange = shouldHighlightOnChange;
        }

        public string Heading { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public bool ShouldHighlightOnChange { get; }

        public static ChapterObjectivePlan Hidden => new ChapterObjectivePlan(string.Empty, string.Empty, false, false);
    }

    public static class ChapterObjectivePlanner
    {
        public static ChapterObjectivePlan Build(
            ChapterProgressionSO progression,
            string currentAreaId,
            bool hasGateSigil,
            bool gatekeeperCleared,
            bool chapterCompleted)
        {
            if (chapterCompleted)
            {
                return ChapterObjectivePlan.Hidden;
            }

            string effectiveAreaId = ResolveCurrentAreaId(progression, currentAreaId);
            string areaLabel = ResolveAreaLabel(progression, effectiveAreaId);

            if (gatekeeperCleared)
            {
                return new ChapterObjectivePlan(
                    "Ritual Core Ahead",
                    "The gatekeeper is down. Walk forward and pick up the Ritual Core to finish the chapter.",
                    true,
                    false);
            }

            if (hasGateSigil && effectiveAreaId != Chapter01Ids.Areas.Boss)
            {
                return new ChapterObjectivePlan(
                    "Boss Gate Open",
                    "The Gate Sigil unlocked the boss route. Push forward and challenge the gatekeeper.",
                    true);
            }

            switch (effectiveAreaId)
            {
                case Chapter01Ids.Areas.Courtyard:
                    return new ChapterObjectivePlan(
                        areaLabel,
                        "Win the courtyard skirmish, then push into the school interior.",
                        true);
                case Chapter01Ids.Areas.Interior:
                    return new ChapterObjectivePlan(
                        areaLabel,
                        "Clear the sealed room and recover the Gate Sigil.",
                        true);
                case Chapter01Ids.Areas.Boss:
                    return new ChapterObjectivePlan(
                        areaLabel,
                        "Defeat the Campus Gatekeeper and secure the Ritual Core.",
                        true);
                case Chapter01Ids.Areas.Entrance:
                    return new ChapterObjectivePlan(
                        areaLabel,
                        "Finish the tutorial encounter and activate CP01.",
                        true);
                default:
                    return new ChapterObjectivePlan(
                        string.IsNullOrWhiteSpace(areaLabel) ? "Current Objective" : areaLabel,
                        "Push forward and secure the next checkpoint.",
                        true);
            }
        }

        private static string ResolveCurrentAreaId(ChapterProgressionSO progression, string currentAreaId)
        {
            if (!string.IsNullOrWhiteSpace(currentAreaId))
            {
                return currentAreaId;
            }

            return progression != null ? progression.GetFirstAreaId() : string.Empty;
        }

        private static string ResolveAreaLabel(ChapterProgressionSO progression, string areaId)
        {
            if (progression == null || string.IsNullOrWhiteSpace(areaId))
            {
                return areaId ?? string.Empty;
            }

            ChapterAreaProgressionEntry[] areas = progression.Areas;

            for (int i = 0; i < areas.Length; i++)
            {
                if (areas[i] != null && areas[i].AreaId == areaId)
                {
                    return areas[i].DisplayName;
                }
            }

            return areaId;
        }
    }

    public readonly struct ChapterObjectiveLayout
    {
        public ChapterObjectiveLayout(Rect panelRect, Rect titleRect, Rect headingRect, Rect bodyRect)
        {
            PanelRect = panelRect;
            TitleRect = titleRect;
            HeadingRect = headingRect;
            BodyRect = bodyRect;
        }

        public Rect PanelRect { get; }

        public Rect TitleRect { get; }

        public Rect HeadingRect { get; }

        public Rect BodyRect { get; }
    }

    public static class ChapterObjectiveLayoutUtility
    {
        private const float HorizontalMargin = 18f;
        private const float VerticalMargin = 12f;
        private const float TopOffset = 18f;
        private const float MaxWidth = 360f;
        private const float MinWidth = 240f;
        private const float Height = 118f;
        private const float PaddingX = 18f;
        private const float TopPadding = 12f;
        private const float BottomPadding = 10f;

        public static ChapterObjectiveLayout Build(float screenWidth, float screenHeight)
        {
            float availableWidth = Mathf.Max(1f, screenWidth - HorizontalMargin * 2f);
            float minimumWidth = Mathf.Min(MinWidth, availableWidth);
            float width = Mathf.Clamp(availableWidth, minimumWidth, MaxWidth);

            float availableHeight = Mathf.Max(1f, screenHeight - VerticalMargin * 2f);
            float height = Mathf.Min(Height, availableHeight);
            float maxTop = Mathf.Max(VerticalMargin, screenHeight - height - VerticalMargin);
            float top = Mathf.Clamp(TopOffset, VerticalMargin, maxTop);
            float maxLeft = Mathf.Max(0f, screenWidth - width - HorizontalMargin);
            float left = Mathf.Clamp(HorizontalMargin, 0f, maxLeft);
            Rect panelRect = new Rect(left, top, width, height);

            float textX = panelRect.x + PaddingX;
            float textWidth = Mathf.Max(1f, panelRect.width - PaddingX * 2f);
            float titleHeight = Mathf.Min(20f, Mathf.Max(12f, height * 0.17f));
            float headingHeight = Mathf.Min(28f, Mathf.Max(20f, height * 0.24f));
            float titleY = panelRect.y + TopPadding;
            float headingY = titleY + titleHeight + 2f;
            float bodyY = headingY + headingHeight + 4f;
            float bodyHeight = Mathf.Max(1f, panelRect.yMax - BottomPadding - bodyY);

            return new ChapterObjectiveLayout(
                panelRect,
                new Rect(textX, titleY, textWidth, titleHeight),
                new Rect(textX, headingY, textWidth, headingHeight),
                new Rect(textX, bodyY, textWidth, bodyHeight));
        }
    }

    [DisallowMultipleComponent]
    public sealed class ChapterObjectiveView : MonoBehaviour
    {
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private string panelTitle = "Current Objective";
        [SerializeField] private float highlightDurationSeconds = 2.4f;

        private bool isSubscribed;
        private float highlightRemainingSeconds;
        private ChapterObjectivePlan currentPlan;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private Texture2D normalBackground;
        private Texture2D highlightBackground;

        public bool IsVisible => currentPlan.IsVisible;

        public string CurrentHeading => currentPlan.Heading;

        public string CurrentBody => currentPlan.Body;

        public bool IsHighlightActive => highlightRemainingSeconds > 0f;

        private void Awake()
        {
            ResolveReferences();
            RefreshPlan(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshPlan(false);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            DestroyTexture(ref normalBackground);
            DestroyTexture(ref highlightBackground);
        }

        private void Update()
        {
            if (highlightRemainingSeconds <= 0f)
            {
                return;
            }

            highlightRemainingSeconds = Mathf.Max(0f, highlightRemainingSeconds - Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (!currentPlan.IsVisible)
            {
                return;
            }

            EnsureStyles();

            ChapterObjectiveLayout layout = ChapterObjectiveLayoutUtility.Build(Screen.width, Screen.height);

            GUI.Box(layout.PanelRect, GUIContent.none, panelStyle);
            GUI.Label(layout.TitleRect, panelTitle, titleStyle);
            GUI.Label(layout.HeadingRect, currentPlan.Heading, headingStyle);
            GUI.Label(layout.BodyRect, currentPlan.Body, bodyStyle);
        }

        private void HandleProgressChanged()
        {
            RefreshPlan(true);
        }

        private void Subscribe()
        {
            if (isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.ProgressChanged += HandleProgressChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.ProgressChanged -= HandleProgressChanged;
            isSubscribed = false;
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void RefreshPlan(bool allowHighlight)
        {
            ChapterObjectivePlan nextPlan = BuildPlan();
            bool changed = currentPlan.IsVisible != nextPlan.IsVisible
                || currentPlan.Heading != nextPlan.Heading
                || currentPlan.Body != nextPlan.Body;

            currentPlan = nextPlan;

            if (!currentPlan.IsVisible)
            {
                highlightRemainingSeconds = 0f;
                return;
            }

            if (!changed)
            {
                return;
            }

            if (allowHighlight && currentPlan.ShouldHighlightOnChange)
            {
                highlightRemainingSeconds = Mathf.Max(0f, highlightDurationSeconds);
                return;
            }

            highlightRemainingSeconds = 0f;
        }

        private ChapterObjectivePlan BuildPlan()
        {
            if (chapterProgressService == null)
            {
                return ChapterObjectivePlan.Hidden;
            }

            return ChapterObjectivePlanner.Build(
                chapterProgressService.Progression,
                chapterProgressService.CurrentAreaId,
                chapterProgressService.HasKeyItem(Chapter01Ids.KeyItems.GateSigil),
                chapterProgressService.IsEncounterCleared(Chapter01Ids.Encounters.Gatekeeper),
                chapterProgressService.IsChapterCompleted);
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }

            if (normalBackground == null)
            {
                normalBackground = CreateBackground(new Color(0.08f, 0.11f, 0.16f, 0.88f));
            }

            if (highlightBackground == null)
            {
                highlightBackground = CreateBackground(new Color(0.16f, 0.18f, 0.09f, 0.92f));
            }

            panelStyle.normal.background = highlightRemainingSeconds > 0f ? highlightBackground : normalBackground;

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                titleStyle.normal.textColor = new Color(0.72f, 0.82f, 0.96f);
            }

            if (headingStyle == null)
            {
                headingStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                headingStyle.normal.textColor = new Color(0.96f, 0.9f, 0.65f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                bodyStyle.normal.textColor = Color.white;
            }
        }

        private static Texture2D CreateBackground(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            Object.Destroy(texture);
            texture = null;
        }
    }
}
