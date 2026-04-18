using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct ChapterResumeContextPlan
    {
        public ChapterResumeContextPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static ChapterResumeContextPlan Hidden => new ChapterResumeContextPlan(string.Empty, string.Empty, false);
    }

    public static class ChapterResumeContextPlanner
    {
        public static ChapterResumeContextPlan Build(ChapterSaveData saveData)
        {
            if (saveData == null || saveData.chapterId != Chapter01Ids.Chapter)
            {
                return ChapterResumeContextPlan.Hidden;
            }

            string progressLabel = DescribeProgress(saveData);
            string effectiveAreaId = ResolveEffectiveAreaId(saveData);
            bool gatekeeperCleared = ContainsProgressFlag(saveData.clearedEncounterIds, Chapter01Ids.Encounters.Gatekeeper);
            bool hasGateSigil = ContainsProgressFlag(saveData.keyItemIds, Chapter01Ids.KeyItems.GateSigil);

            if (saveData.chapterCompleted)
            {
                return new ChapterResumeContextPlan(
                    "Resume: Chapter Complete",
                    "The Ritual Core is already secured. Walk forward to review the ending card.",
                    true);
            }

            if (gatekeeperCleared)
            {
                return new ChapterResumeContextPlan(
                    $"Resume: {progressLabel}",
                    "The gatekeeper is down. Walk forward and pick up the Ritual Core to finish the chapter.",
                    true);
            }

            if (hasGateSigil && effectiveAreaId != Chapter01Ids.Areas.Boss)
            {
                return new ChapterResumeContextPlan(
                    $"Resume: {progressLabel}",
                    "The boss route is open. Push through the gate and challenge the Campus Gatekeeper.",
                    true);
            }

            switch (effectiveAreaId)
            {
                case Chapter01Ids.Areas.Courtyard:
                    return new ChapterResumeContextPlan(
                        $"Resume: {progressLabel}",
                        "Mixed enemies are still ahead. Clear the courtyard, then push into the school interior.",
                        true);
                case Chapter01Ids.Areas.Interior:
                    return new ChapterResumeContextPlan(
                        $"Resume: {progressLabel}",
                        "The sealed room still needs to be cleared. Recover the Gate Sigil and open the boss route.",
                        true);
                case Chapter01Ids.Areas.Boss:
                    return new ChapterResumeContextPlan(
                        $"Resume: {progressLabel}",
                        "Final exam ahead. Read the gatekeeper, win the duel, and secure the Ritual Core.",
                        true);
                case Chapter01Ids.Areas.Entrance:
                    return new ChapterResumeContextPlan(
                        $"Resume: {progressLabel}",
                        "Finish the tutorial drill, activate CP01, and start pushing deeper into the campus.",
                        true);
                default:
                    return new ChapterResumeContextPlan(
                        $"Resume: {progressLabel}",
                        "Push forward and secure the next checkpoint.",
                        true);
            }
        }

        private static string DescribeProgress(ChapterSaveData saveData)
        {
            if (!string.IsNullOrWhiteSpace(saveData.checkpointId))
            {
                switch (saveData.checkpointId)
                {
                    case Chapter01Ids.Checkpoints.Start:
                        return "CP01 / Entrance Tutorial";
                    case Chapter01Ids.Checkpoints.Courtyard:
                        return "CP02 / Outdoor Courtyard";
                    case Chapter01Ids.Checkpoints.Interior:
                        return "CP03 / School Interior";
                }
            }

            switch (saveData.currentAreaId)
            {
                case Chapter01Ids.Areas.Entrance:
                    return "Area01 / Entrance Tutorial";
                case Chapter01Ids.Areas.Courtyard:
                    return "Area02 / Outdoor Courtyard";
                case Chapter01Ids.Areas.Interior:
                    return "Area03 / School Interior";
                case Chapter01Ids.Areas.Boss:
                    return "Area04 / Boss Arena";
                default:
                    return "Current Checkpoint";
            }
        }

        private static string ResolveEffectiveAreaId(ChapterSaveData saveData)
        {
            if (!string.IsNullOrWhiteSpace(saveData.currentAreaId))
            {
                return saveData.currentAreaId;
            }

            switch (saveData.checkpointId)
            {
                case Chapter01Ids.Checkpoints.Start:
                    return Chapter01Ids.Areas.Entrance;
                case Chapter01Ids.Checkpoints.Courtyard:
                    return Chapter01Ids.Areas.Courtyard;
                case Chapter01Ids.Checkpoints.Interior:
                    return Chapter01Ids.Areas.Interior;
                default:
                    return string.Empty;
            }
        }

        private static bool ContainsProgressFlag(string[] values, string expectedValue)
        {
            if (values == null || string.IsNullOrWhiteSpace(expectedValue))
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == expectedValue)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ChapterResumeContextView : MonoBehaviour
    {
        [SerializeField] private SaveService saveService;
        [SerializeField] private float visibleDurationSeconds = 2.6f;

        private bool pendingInitialPresentation;
        private bool isVisible;
        private float visibleTimer;
        private string currentTitle = string.Empty;
        private string currentBody = string.Empty;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Texture2D panelTexture;

        public bool IsVisible => isVisible;

        public string CurrentTitle => currentTitle;

        public string CurrentBody => currentBody;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            pendingInitialPresentation = HasChapterSaveReady();
            isVisible = false;
            visibleTimer = 0f;
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
                panelTexture = null;
            }
        }

        private void Update()
        {
            if (pendingInitialPresentation)
            {
                pendingInitialPresentation = false;
                TryPresentResumeContext();
            }

            if (!isVisible)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Time.unscaledDeltaTime);
            isVisible = visibleTimer > 0f;
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            EnsureStyles();

            const float width = 460f;
            const float height = 92f;
            Rect panelRect = new Rect((Screen.width - width) * 0.5f, 118f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 42f, panelRect.width - 36f, 34f), currentBody, bodyStyle);
        }

        private void ResolveReferences()
        {
            if (saveService == null)
            {
                saveService = GetComponent<SaveService>();
            }

            if (saveService == null)
            {
                saveService = FindAnyObjectByType<SaveService>();
            }
        }

        private bool HasChapterSaveReady()
        {
            return saveService != null
                && saveService.TryLoad(out ChapterSaveData saveData)
                && saveData != null
                && saveData.chapterId == Chapter01Ids.Chapter;
        }

        private void TryPresentResumeContext()
        {
            if (saveService == null || !saveService.TryLoad(out ChapterSaveData saveData))
            {
                return;
            }

            Show(ChapterResumeContextPlanner.Build(saveData));
        }

        private void Show(ChapterResumeContextPlan plan)
        {
            if (!plan.IsVisible)
            {
                return;
            }

            currentTitle = plan.Title;
            currentBody = plan.Body;
            visibleTimer = Mathf.Max(0.1f, visibleDurationSeconds);
            isVisible = true;
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.09f, 0.11f, 0.17f, 0.95f));
                panelTexture.Apply();

                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 21,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                titleStyle.normal.textColor = new Color(0.95f, 0.9f, 0.72f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter
                };
                bodyStyle.normal.textColor = Color.white;
            }
        }
    }
}
