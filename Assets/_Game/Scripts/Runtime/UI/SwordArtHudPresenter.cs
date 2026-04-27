using CampusRPG.Character;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct SwordArtHudLayout
    {
        public SwordArtHudLayout(
            Rect panelRect,
            Rect titleRect,
            Rect statusRect,
            Rect detailRect,
            Rect hintRect,
            Rect progressTrackRect)
        {
            PanelRect = panelRect;
            TitleRect = titleRect;
            StatusRect = statusRect;
            DetailRect = detailRect;
            HintRect = hintRect;
            ProgressTrackRect = progressTrackRect;
        }

        public Rect PanelRect { get; }

        public Rect TitleRect { get; }

        public Rect StatusRect { get; }

        public Rect DetailRect { get; }

        public Rect HintRect { get; }

        public Rect ProgressTrackRect { get; }
    }

    public static class SwordArtHudLayoutUtility
    {
        private const float HorizontalMargin = 16f;
        private const float BottomMargin = 16f;
        private const float MaxWidth = 460f;
        private const float MinWidth = 280f;
        private const float Height = 82f;
        private const float BottomOffset = 92f;
        private const float CompactHeightThreshold = 320f;
        private const float ProgressHeight = 5f;

        public static SwordArtHudLayout Build(float screenWidth, float screenHeight)
        {
            float availableWidth = Mathf.Max(1f, screenWidth - HorizontalMargin * 2f);
            float minimumWidth = Mathf.Min(MinWidth, availableWidth);
            float width = Mathf.Clamp(availableWidth, minimumWidth, MaxWidth);
            float preferredY = screenHeight - BottomOffset - Height;
            float compactY = screenHeight - BottomMargin - Height;
            float panelY = screenHeight < CompactHeightThreshold ? compactY : preferredY;
            Rect panelRect = new Rect(
                (screenWidth - width) * 0.5f,
                Mathf.Max(BottomMargin, panelY),
                width,
                Height);

            return new SwordArtHudLayout(
                panelRect,
                new Rect(panelRect.x + 14f, panelRect.y + 8f, panelRect.width - 142f, 24f),
                new Rect(panelRect.x + panelRect.width - 124f, panelRect.y + 9f, 110f, 22f),
                new Rect(panelRect.x + 14f, panelRect.y + 34f, panelRect.width - 28f, 18f),
                new Rect(panelRect.x + 14f, panelRect.y + 52f, panelRect.width - 28f, 18f),
                new Rect(panelRect.x + 14f, panelRect.y + panelRect.height - 11f, panelRect.width - 28f, ProgressHeight));
        }

        public static Rect BuildProgressFill(Rect trackRect, float progress01)
        {
            float clampedProgress = Mathf.Clamp01(progress01);
            return new Rect(trackRect.x, trackRect.y, trackRect.width * clampedProgress, trackRect.height);
        }
    }

    public sealed class SwordArtHudPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter playerCharacter;

        private GUIStyle titleStyle;
        private GUIStyle statusStyle;
        private GUIStyle detailStyle;
        private GUIStyle hintStyle;

        public void Configure(PlayerCharacter player)
        {
            playerCharacter = player;
        }

        private void OnGUI()
        {
            ResolveReferences();

            SwordArtHudPlan plan = SwordArtHudUtility.Build(playerCharacter != null ? playerCharacter.CombatController : null);

            if (!plan.IsVisible)
            {
                return;
            }

            EnsureStyles();
            Draw(plan);
        }

        private void Draw(SwordArtHudPlan plan)
        {
            SwordArtHudLayout layout = SwordArtHudLayoutUtility.Build(Screen.width, Screen.height);

            Color previousColor = GUI.color;
            GUI.color = ResolvePanelColor(plan.Mode);
            GUI.Box(layout.PanelRect, GUIContent.none);
            GUI.color = previousColor;

            GUI.Label(layout.TitleRect, plan.Title, titleStyle);
            GUI.Label(layout.StatusRect, plan.Status, statusStyle);
            GUI.Label(layout.DetailRect, plan.Detail, detailStyle);
            GUI.Label(layout.HintRect, plan.InputHint, hintStyle);
            DrawProgress(layout.ProgressTrackRect, plan);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                clipping = TextClipping.Clip
            };

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.86f, 0.96f, 1f) },
                clipping = TextClipping.Clip
            };

            detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.86f, 0.9f, 0.92f) },
                clipping = TextClipping.Clip
            };

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.68f, 0.78f, 0.82f) },
                clipping = TextClipping.Clip
            };
        }

        private static Color ResolvePanelColor(SwordArtHudMode mode)
        {
            return mode switch
            {
                SwordArtHudMode.Current => new Color(0.04f, 0.12f, 0.16f, 0.88f),
                SwordArtHudMode.CancelWindow => new Color(0.16f, 0.10f, 0.03f, 0.88f),
                SwordArtHudMode.Preview => new Color(0.04f, 0.14f, 0.10f, 0.84f),
                SwordArtHudMode.Recent => new Color(0.08f, 0.08f, 0.10f, 0.78f),
                _ => new Color(0.05f, 0.05f, 0.06f, 0.82f)
            };
        }

        private static Color ResolveProgressColor(SwordArtHudMode mode)
        {
            return mode switch
            {
                SwordArtHudMode.Current => new Color(0.35f, 0.88f, 1f, 0.94f),
                SwordArtHudMode.CancelWindow => new Color(1f, 0.72f, 0.28f, 0.94f),
                SwordArtHudMode.Preview => new Color(0.42f, 1f, 0.64f, 0.9f),
                SwordArtHudMode.Recent => new Color(0.72f, 0.78f, 0.88f, 0.72f),
                _ => new Color(0.8f, 0.86f, 0.92f, 0.8f)
            };
        }

        private static void DrawProgress(Rect trackRect, SwordArtHudPlan plan)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.42f);
            GUI.DrawTexture(trackRect, Texture2D.whiteTexture);

            GUI.color = ResolveProgressColor(plan.Mode);
            GUI.DrawTexture(SwordArtHudLayoutUtility.BuildProgressFill(trackRect, plan.Progress01), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void ResolveReferences()
        {
            playerCharacter = SceneRuntimeReferenceUtility.ResolvePlayerCharacter(playerCharacter);
        }
    }
}
