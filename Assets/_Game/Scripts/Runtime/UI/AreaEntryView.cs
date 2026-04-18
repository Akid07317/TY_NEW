using System;
using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct AreaEntryPlan
    {
        public AreaEntryPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static AreaEntryPlan Hidden => new AreaEntryPlan(string.Empty, string.Empty, false);
    }

    public static class AreaEntryPlanner
    {
        public static AreaEntryPlan Build(ChapterProgressionSO progression, string areaId)
        {
            string areaLabel = ResolveAreaLabel(progression, areaId);

            switch (areaId)
            {
                case Chapter01Ids.Areas.Entrance:
                    return new AreaEntryPlan(
                        areaLabel,
                        "Warm up here. Learn the controls, clear the drill, and lock in CP01.",
                        true);
                case Chapter01Ids.Areas.Courtyard:
                    return new AreaEntryPlan(
                        areaLabel,
                        "Mixed enemies ahead. Clear the courtyard to reach the school interior.",
                        true);
                case Chapter01Ids.Areas.Interior:
                    return new AreaEntryPlan(
                        areaLabel,
                        "Rooms tighten up here. Break the seal, manage resources, and recover the Gate Sigil.",
                        true);
                case Chapter01Ids.Areas.Boss:
                    return new AreaEntryPlan(
                        areaLabel,
                        "Final exam ahead. Use the checkpoint, read the gatekeeper, and secure the Ritual Core.",
                        true);
                default:
                    return string.IsNullOrWhiteSpace(areaId)
                        ? AreaEntryPlan.Hidden
                        : new AreaEntryPlan(
                            string.IsNullOrWhiteSpace(areaLabel) ? "New Area" : areaLabel,
                            "Push forward and secure the next checkpoint.",
                            true);
            }
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

    [DisallowMultipleComponent]
    public sealed class AreaEntryView : MonoBehaviour
    {
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private SaveService saveService;
        [SerializeField] private float visibleDurationSeconds = 2.2f;

        private bool isSubscribed;
        private bool pendingInitialRefresh = true;
        private bool suppressInitialPresentationFromResume;
        private bool isVisible;
        private float visibleTimer;
        private string lastPresentedAreaId = string.Empty;
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
            pendingInitialRefresh = true;
            suppressInitialPresentationFromResume = HasChapterSaveReady();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            pendingInitialRefresh = true;
            suppressInitialPresentationFromResume = HasChapterSaveReady();
        }

        private void OnDisable()
        {
            Unsubscribe();
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
            if (pendingInitialRefresh)
            {
                pendingInitialRefresh = false;
                RefreshAreaEntry();
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

            const float width = 430f;
            const float height = 92f;
            Rect panelRect = new Rect((Screen.width - width) * 0.5f, 18f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 42f, panelRect.width - 36f, 32f), currentBody, bodyStyle);
        }

        private void HandleProgressChanged()
        {
            RefreshAreaEntry();
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

            if (saveService == null)
            {
                saveService = GetComponent<SaveService>();
            }

            if (saveService == null)
            {
                saveService = FindAnyObjectByType<SaveService>();
            }
        }

        private void RefreshAreaEntry()
        {
            if (chapterProgressService == null || chapterProgressService.IsChapterCompleted)
            {
                return;
            }

            string currentAreaId = chapterProgressService.CurrentAreaId;

            if (suppressInitialPresentationFromResume)
            {
                suppressInitialPresentationFromResume = false;
                lastPresentedAreaId = currentAreaId ?? string.Empty;
                return;
            }

            if (string.Equals(lastPresentedAreaId, currentAreaId, StringComparison.Ordinal))
            {
                return;
            }

            lastPresentedAreaId = currentAreaId ?? string.Empty;
            Show(AreaEntryPlanner.Build(chapterProgressService.Progression, currentAreaId));
        }

        private bool HasChapterSaveReady()
        {
            return saveService != null
                && saveService.TryLoad(out ChapterSaveData saveData)
                && saveData != null
                && saveData.chapterId == Chapter01Ids.Chapter;
        }

        private void Show(AreaEntryPlan plan)
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
                panelTexture.SetPixel(0, 0, new Color(0.08f, 0.12f, 0.18f, 0.95f));
                panelTexture.Apply();

                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                titleStyle.normal.textColor = new Color(0.84f, 0.92f, 1f);
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
