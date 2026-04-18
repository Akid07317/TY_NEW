using CampusRPG.AI;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossThreatPulsePresenter : MonoBehaviour
    {
        public enum PulseKind
        {
            None = 0,
            Encounter = 1,
            Attack = 2
        }

        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private BossTelegraphStyleSO telegraphStyle;
        [SerializeField] private float encounterPulseSeconds = 0.44f;
        [SerializeField] private float attackPulseSeconds = 0.24f;
        [SerializeField] private Color encounterPulseColor = new Color(0.95f, 0.54f, 0.14f, 0.22f);
        [SerializeField] private Color attackPulseColor = new Color(0.88f, 0.16f, 0.14f, 0.26f);

        private bool isVisible;
        private bool wasBossActiveLastFrame;
        private float pulseDuration;
        private float pulseRemaining;
        private string lastStateName = string.Empty;
        private Texture2D overlayTexture;
        private PulseKind currentPulseKind;
        private Color currentPulseColor;

        public bool IsVisible => isVisible;

        public float RemainingVisibleSeconds => pulseRemaining;

        public PulseKind CurrentPulseKind => currentPulseKind;

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

            EnsureTexture();

            Color previousColor = GUI.color;
            GUI.color = new Color(
                currentPulseColor.r,
                currentPulseColor.g,
                currentPulseColor.b,
                currentPulseColor.a * EvaluateAlpha());
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);
            GUI.color = previousColor;
        }

        private void OnDestroy()
        {
            if (overlayTexture != null)
            {
                Destroy(overlayTexture);
                overlayTexture = null;
            }
        }

        private void Tick(float deltaTime)
        {
            if (!BossPresentationRules.IsBossEligible(bossEnemy))
            {
                ResetRuntimeState();
                return;
            }

            if (!wasBossActiveLastFrame)
            {
                StartPulse(BossThreatPulsePlanner.CreateEncounterPlan(
                    telegraphStyle,
                    encounterPulseSeconds,
                    encounterPulseColor));
            }

            string currentStateName = bossEnemy.StateMachine != null ? bossEnemy.StateMachine.CurrentStateName : string.Empty;

            if (currentStateName != lastStateName)
            {
                if (currentStateName == nameof(EnemyAttackState))
                {
                    StartPulse(BossThreatPulsePlanner.CreateAttackPlan(
                        telegraphStyle,
                        attackPulseSeconds,
                        attackPulseColor));
                }

                lastStateName = currentStateName;
            }

            wasBossActiveLastFrame = true;

            if (!isVisible)
            {
                return;
            }

            pulseRemaining = Mathf.Max(0f, pulseRemaining - Mathf.Max(0f, deltaTime));
            isVisible = pulseRemaining > 0f;

            if (!isVisible)
            {
                currentPulseKind = PulseKind.None;
            }
        }

        private void StartPulse(BossThreatPulsePlan plan)
        {
            currentPulseKind = plan.Kind;
            currentPulseColor = plan.Color;
            pulseDuration = plan.Duration;
            pulseRemaining = pulseDuration;
            isVisible = true;
        }

        private void ResetRuntimeState()
        {
            isVisible = false;
            wasBossActiveLastFrame = false;
            pulseDuration = 0f;
            pulseRemaining = 0f;
            lastStateName = string.Empty;
            currentPulseKind = PulseKind.None;
            currentPulseColor = default;
        }

        private float EvaluateAlpha()
        {
            return BossThreatPulsePlanner.EvaluateAlpha(pulseDuration, pulseRemaining);
        }

        private void EnsureTexture()
        {
            if (overlayTexture != null)
            {
                return;
            }

            overlayTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            overlayTexture.SetPixel(0, 0, Color.white);
            overlayTexture.Apply();
        }
    }
}
