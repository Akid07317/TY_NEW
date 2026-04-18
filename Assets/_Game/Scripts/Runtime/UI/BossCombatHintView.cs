using CampusRPG.Interaction;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct BossCombatHintPlan
    {
        public BossCombatHintPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static BossCombatHintPlan Hidden => new BossCombatHintPlan(string.Empty, string.Empty, false);
    }

    public static class BossCombatHintPlanner
    {
        public static BossCombatHintPlan Build(string encounterId)
        {
            switch (encounterId)
            {
                case Chapter01Ids.Encounters.Gatekeeper:
                    return new BossCombatHintPlan(
                        "Gatekeeper Tactics",
                        "Block the close strings, dodge the wide shockwaves, and punish the long recovery windows.",
                        true);
                default:
                    return BossCombatHintPlan.Hidden;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class BossCombatHintView : MonoBehaviour
    {
        [SerializeField] private EncounterController bossEncounter;
        [SerializeField] private float visibleDurationSeconds = 2.6f;

        private bool hasInitialized;
        private bool isVisible;
        private bool lastIsActive;
        private bool lastIsCleared;
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

        public void Configure(EncounterController configuredBossEncounter)
        {
            bossEncounter = configuredBossEncounter;
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

            const float width = 404f;
            const float height = 94f;
            Rect panelRect = new Rect(Screen.width - width - 18f, Screen.height - height - 24f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 42f, panelRect.width - 36f, 34f), currentBody, bodyStyle);
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
            if (bossEncounter == null)
            {
                ResetPresentation();
                return;
            }

            bool isActive = bossEncounter.IsActive;
            bool isCleared = bossEncounter.IsCleared;

            if (!hasInitialized)
            {
                hasInitialized = true;
                lastIsActive = isActive;
                lastIsCleared = isCleared;
            }
            else
            {
                if (!lastIsActive && isActive && !isCleared)
                {
                    Show(BossCombatHintPlanner.Build(bossEncounter.EncounterId));
                }

                lastIsActive = isActive;
                lastIsCleared = isCleared;
            }

            if (!isVisible)
            {
                return;
            }

            if (lastIsCleared)
            {
                isVisible = false;
                visibleTimer = 0f;
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Mathf.Max(0f, deltaTime));
            isVisible = visibleTimer > 0f;
        }

        private void Show(BossCombatHintPlan plan)
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

        private void ResetPresentation()
        {
            hasInitialized = false;
            isVisible = false;
            lastIsActive = false;
            lastIsCleared = false;
            visibleTimer = 0f;
            currentTitle = string.Empty;
            currentBody = string.Empty;
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.1f, 0.08f, 0.12f, 0.95f));
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
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = new Color(0.95f, 0.78f, 0.45f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true
                };
                bodyStyle.normal.textColor = Color.white;
            }
        }
    }
}
