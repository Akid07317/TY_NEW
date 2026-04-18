using CampusRPG.Interaction;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct ChapterRouteBlockHintPlan
    {
        public ChapterRouteBlockHintPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static ChapterRouteBlockHintPlan Hidden => new ChapterRouteBlockHintPlan(string.Empty, string.Empty, false);
    }

    public static class ChapterRouteBlockHintPlanner
    {
        public static ChapterRouteBlockHintPlan Build(DoorRequirementHintRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.RequiredKeyItemId))
            {
                switch (request.RequiredKeyItemId)
                {
                    case Chapter01Ids.KeyItems.GateSigil:
                        return new ChapterRouteBlockHintPlan(
                            "Boss Gate Sealed",
                            "Recover the Gate Sigil from the interior room to open this route.",
                            true);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.RequiredEncounterId))
            {
                switch (request.RequiredEncounterId)
                {
                    case Chapter01Ids.Encounters.EntranceTutorial:
                        return new ChapterRouteBlockHintPlan(
                            "Training Gate Locked",
                            "Clear the tutorial enemies before you move into the courtyard.",
                            true);
                    case Chapter01Ids.Encounters.Courtyard:
                        return new ChapterRouteBlockHintPlan(
                            "Courtyard Route Locked",
                            "Win the courtyard skirmish before you push into the school interior.",
                            true);
                    case Chapter01Ids.Encounters.Gatekeeper:
                        return new ChapterRouteBlockHintPlan(
                            "Ritual Core Sealed",
                            "Defeat the Campus Gatekeeper before the Ritual Core route will open.",
                            true);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.RequiredAreaId))
            {
                return new ChapterRouteBlockHintPlan(
                    "Route Locked",
                    "Push deeper into the chapter before this route opens.",
                    true);
            }

            return ChapterRouteBlockHintPlan.Hidden;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ChapterRouteBlockHintView : MonoBehaviour
    {
        [SerializeField] private float visibleDurationSeconds = 2.1f;
        [SerializeField] private string panelTitle = "Route Blocked";

        private bool isVisible;
        private float visibleTimer;
        private string currentTitle = string.Empty;
        private string currentBody = string.Empty;
        private GUIStyle panelStyle;
        private GUIStyle panelTitleStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Texture2D panelTexture;

        public bool IsVisible => isVisible;

        public string CurrentTitle => currentTitle;

        public string CurrentBody => currentBody;

        private void OnEnable()
        {
            DoorRequirementHintTrigger.BlockedRouteReached += HandleBlockedRouteReached;
        }

        private void OnDisable()
        {
            DoorRequirementHintTrigger.BlockedRouteReached -= HandleBlockedRouteReached;
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
            const float height = 88f;
            Rect panelRect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 26f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 10f, panelRect.width - 36f, 18f), panelTitle, panelTitleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 28f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 52f, panelRect.width - 36f, 24f), currentBody, bodyStyle);
        }

        private void HandleBlockedRouteReached(DoorRequirementHintRequest request)
        {
            Show(ChapterRouteBlockHintPlanner.Build(request));
        }

        private void Show(ChapterRouteBlockHintPlan plan)
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
                panelTexture.SetPixel(0, 0, new Color(0.12f, 0.09f, 0.08f, 0.95f));
                panelTexture.Apply();

                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (panelTitleStyle == null)
            {
                panelTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                panelTitleStyle.normal.textColor = new Color(0.93f, 0.78f, 0.65f);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                titleStyle.normal.textColor = new Color(0.98f, 0.9f, 0.68f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                bodyStyle.normal.textColor = Color.white;
            }
        }
    }
}
