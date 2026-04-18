using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct CheckpointActivationPlan
    {
        public CheckpointActivationPlan(string title, string body)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
        }

        public string Title { get; }

        public string Body { get; }
    }

    public static class CheckpointActivationPlanner
    {
        public static CheckpointActivationPlan Build(string checkpointId)
        {
            switch (checkpointId)
            {
                case Chapter01Ids.Checkpoints.Start:
                    return new CheckpointActivationPlan(
                        "Checkpoint Activated",
                        "Respawn updated to the chapter entrance.");
                case Chapter01Ids.Checkpoints.Courtyard:
                    return new CheckpointActivationPlan(
                        "Courtyard Secured",
                        "Respawn moved forward to the outdoor courtyard.");
                case Chapter01Ids.Checkpoints.Interior:
                    return new CheckpointActivationPlan(
                        "Interior Secured",
                        "Respawn moved forward to the school interior before the boss gate.");
                default:
                    return new CheckpointActivationPlan(
                        "Checkpoint Activated",
                        string.IsNullOrWhiteSpace(checkpointId)
                            ? "Respawn point updated."
                            : $"Respawn updated: {checkpointId}");
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class CheckpointActivationView : MonoBehaviour
    {
        [SerializeField] private CheckpointService checkpointService;
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

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
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

            const float width = 340f;
            const float height = 84f;
            Rect panelRect = new Rect(18f, 148f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 40f, panelRect.width - 36f, 28f), currentBody, bodyStyle);
        }

        private void HandleCheckpointActivated(string checkpointId)
        {
            Show(CheckpointActivationPlanner.Build(checkpointId));
        }

        private void Subscribe()
        {
            if (isSubscribed || checkpointService == null)
            {
                return;
            }

            checkpointService.CheckpointActivated += HandleCheckpointActivated;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || checkpointService == null)
            {
                return;
            }

            checkpointService.CheckpointActivated -= HandleCheckpointActivated;
            isSubscribed = false;
        }

        private void ResolveReferences()
        {
            if (checkpointService == null)
            {
                checkpointService = GetComponent<CheckpointService>();
            }

            if (checkpointService == null)
            {
                checkpointService = FindAnyObjectByType<CheckpointService>();
            }
        }

        private void Show(CheckpointActivationPlan plan)
        {
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
                panelTexture.SetPixel(0, 0, new Color(0.08f, 0.12f, 0.11f, 0.94f));
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
                titleStyle.normal.textColor = new Color(0.78f, 0.95f, 0.72f);
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
