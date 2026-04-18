using CampusRPG.Interaction;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct EncounterSealPlan
    {
        public EncounterSealPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static EncounterSealPlan Hidden => new EncounterSealPlan(string.Empty, string.Empty, false);
    }

    public static class EncounterSealPlanner
    {
        public static EncounterSealPlan Build(string encounterId)
        {
            switch (encounterId)
            {
                case Chapter01Ids.Encounters.EntranceTutorial:
                    return new EncounterSealPlan(
                        "Training Trial",
                        "Defeat the tutorial enemies before leaving the entrance.",
                        true);
                case Chapter01Ids.Encounters.Courtyard:
                    return new EncounterSealPlan(
                        "Courtyard Sealed",
                        "Clear the mixed squad to reopen the school interior.",
                        true);
                case Chapter01Ids.Encounters.Interior:
                    return new EncounterSealPlan(
                        "Room Sealed",
                        "Defeat every enemy in the room to break the seal and recover the Gate Sigil.",
                        true);
                case Chapter01Ids.Encounters.Gatekeeper:
                    return EncounterSealPlan.Hidden;
                default:
                    return string.IsNullOrWhiteSpace(encounterId)
                        ? EncounterSealPlan.Hidden
                        : new EncounterSealPlan(
                            "Encounter Started",
                            encounterId,
                            true);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class EncounterSealView : MonoBehaviour
    {
        [SerializeField] private float visibleDurationSeconds = 1.8f;

        private bool isSubscribed;
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

        private void OnEnable()
        {
            if (isSubscribed)
            {
                return;
            }

            EncounterController.EncounterActivated += HandleEncounterActivated;
            isSubscribed = true;
        }

        private void OnDisable()
        {
            if (!isSubscribed)
            {
                return;
            }

            EncounterController.EncounterActivated -= HandleEncounterActivated;
            isSubscribed = false;
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

            const float width = 390f;
            const float height = 86f;
            Rect panelRect = new Rect(18f, 244f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 42f, panelRect.width - 36f, 26f), currentBody, bodyStyle);
        }

        private void HandleEncounterActivated(string encounterId)
        {
            Show(EncounterSealPlanner.Build(encounterId));
        }

        private void Show(EncounterSealPlan plan)
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
                panelTexture.SetPixel(0, 0, new Color(0.15f, 0.1f, 0.08f, 0.95f));
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
                    alignment = TextAnchor.MiddleLeft
                };
                titleStyle.normal.textColor = new Color(0.98f, 0.84f, 0.58f);
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
