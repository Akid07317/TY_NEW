using CampusRPG.Composition;
using CampusRPG.Interaction;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class KeyItemBeaconView : MonoBehaviour
    {
        [SerializeField] private KeyItemPickup keyItemPickup;
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private string requiredEncounterId = string.Empty;
        [SerializeField] private float verticalOffset = 1.9f;
        [SerializeField] private float hoverAmplitude = 0.14f;
        [SerializeField] private float hoverSpeed = 2.6f;
        [SerializeField] private float pulseAmplitude = 0.1f;
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private Vector3 baseScale = new Vector3(0.22f, 0.72f, 0.22f);
        [SerializeField] private Color beaconColor = new Color(1f, 0.84f, 0.35f, 1f);
        [SerializeField] private float revealPulseGroundOffset = 0.08f;
        [SerializeField] private float revealPulseDurationSeconds = 0.85f;
        [SerializeField] private float revealPulseMaxHeight = 0.42f;
        [SerializeField] private float revealPulseMaxRadius = 1.3f;
        [SerializeField] private Color revealPulseColor = new Color(1f, 0.76f, 0.3f, 1f);

        private GameObject beaconVisual;
        private Renderer beaconRenderer;
        private Material beaconMaterial;
        private GameObject currentVisualTemplate;
        private Material currentMaterialTemplate;
        private GameObject revealPulseVisual;
        private Renderer revealPulseRenderer;
        private Material revealPulseMaterial;
        private GameObject currentRevealPulseTemplate;
        private Material currentRevealPulseMaterialTemplate;
        private float hoverTime;
        private float revealPulseRemainingSeconds;
        private Vector3 currentWorldPosition;
        private Vector3 currentRevealPulseBasePosition;

        public bool IsVisible => beaconVisual != null && beaconVisual.activeSelf;

        public Vector3 CurrentWorldPosition => currentWorldPosition;

        public bool IsRevealPulseVisible => revealPulseVisual != null && revealPulseVisual.activeSelf;

        public Vector3 CurrentRevealPulseBasePosition => currentRevealPulseBasePosition;

        private void Awake()
        {
            ResolveReferences();
            EnsureVisual();
            EnsureRevealPulseVisual();
            HideBeacon();
            HideRevealPulse();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnDestroy()
        {
            BossTelegraphVisualUtility.DestroyVisualAndMaterial(
                ref beaconVisual,
                ref beaconRenderer,
                ref currentVisualTemplate,
                ref beaconMaterial,
                ref currentMaterialTemplate);
            BossTelegraphVisualUtility.DestroyVisualAndMaterial(
                ref revealPulseVisual,
                ref revealPulseRenderer,
                ref currentRevealPulseTemplate,
                ref revealPulseMaterial,
                ref currentRevealPulseMaterialTemplate);
        }

        private void Tick(float deltaTime)
        {
            ResolveReferences();

            if (!ShouldShow())
            {
                HideBeacon();
                HideRevealPulse();
                return;
            }

            EnsureVisual();
            hoverTime += Mathf.Max(0f, deltaTime);
            UpdateVisual();

            if (!IsVisible && beaconVisual != null)
            {
                ApplyBeaconMaterial();
                beaconVisual.SetActive(true);
                ShowRevealPulse();
            }

            TickRevealPulse(deltaTime);
        }

        private bool ShouldShow()
        {
            if (keyItemPickup == null
                || chapterProgressService == null
                || !gameObject.activeInHierarchy
                || chapterProgressService.IsChapterCompleted)
            {
                return false;
            }

            string keyItemId = keyItemPickup.KeyItemId;

            if (string.IsNullOrWhiteSpace(keyItemId) || chapterProgressService.HasKeyItem(keyItemId))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(requiredEncounterId)
                || chapterProgressService.IsEncounterCleared(requiredEncounterId);
        }

        private void EnsureVisual()
        {
            BossTelegraphVisualUtility.EnsureVisual(
                transform,
                "KeyItemBeaconVisual",
                null,
                ref beaconVisual,
                ref beaconRenderer,
                ref currentVisualTemplate,
                ref beaconMaterial,
                ref currentMaterialTemplate);
        }

        private void ApplyBeaconMaterial()
        {
            BossTelegraphVisualUtility.ApplyRuntimeMaterial(
                beaconRenderer,
                null,
                beaconColor,
                ref beaconMaterial,
                ref currentMaterialTemplate);
        }

        private void EnsureRevealPulseVisual()
        {
            BossTelegraphVisualUtility.EnsureVisual(
                transform,
                "KeyItemRevealPulseVisual",
                null,
                ref revealPulseVisual,
                ref revealPulseRenderer,
                ref currentRevealPulseTemplate,
                ref revealPulseMaterial,
                ref currentRevealPulseMaterialTemplate);
        }

        private void ApplyRevealPulseMaterial()
        {
            BossTelegraphVisualUtility.ApplyRuntimeMaterial(
                revealPulseRenderer,
                null,
                revealPulseColor,
                ref revealPulseMaterial,
                ref currentRevealPulseMaterialTemplate);
        }

        private void UpdateVisual()
        {
            if (beaconVisual == null)
            {
                return;
            }

            float hoverOffset = Mathf.Sin(hoverTime * hoverSpeed) * hoverAmplitude;
            float scaleMultiplier = 1f + Mathf.Sin(hoverTime * pulseSpeed) * pulseAmplitude;
            currentWorldPosition = transform.position + Vector3.up * (verticalOffset + hoverOffset);

            beaconVisual.transform.position = currentWorldPosition;
            beaconVisual.transform.rotation = Quaternion.identity;
            beaconVisual.transform.localScale = baseScale * scaleMultiplier;
        }

        private void TickRevealPulse(float deltaTime)
        {
            if (!IsRevealPulseVisible)
            {
                return;
            }

            revealPulseRemainingSeconds = Mathf.Max(0f, revealPulseRemainingSeconds - Mathf.Max(0f, deltaTime));

            if (revealPulseRemainingSeconds <= 0f)
            {
                HideRevealPulse();
                return;
            }

            ApplyRevealPulsePlan(
                KeyItemRevealPulsePlanner.BuildRuntimePlan(
                    transform.position,
                    revealPulseGroundOffset,
                    revealPulseDurationSeconds,
                    revealPulseMaxHeight,
                    revealPulseMaxRadius,
                    revealPulseRemainingSeconds));
        }

        private void ShowRevealPulse()
        {
            EnsureRevealPulseVisual();

            if (revealPulseVisual == null)
            {
                return;
            }

            ApplyRevealPulseMaterial();
            ApplyRevealPulsePlan(
                KeyItemRevealPulsePlanner.CreateActivationPlan(
                    transform.position,
                    revealPulseGroundOffset,
                    revealPulseDurationSeconds,
                    revealPulseMaxHeight,
                    revealPulseMaxRadius));
            revealPulseVisual.SetActive(true);
        }

        private void ApplyRevealPulsePlan(KeyItemRevealPulsePlan plan)
        {
            revealPulseRemainingSeconds = plan.RemainingTime;
            currentRevealPulseBasePosition = plan.BasePosition;

            if (revealPulseVisual == null)
            {
                return;
            }

            revealPulseVisual.transform.position = plan.VisualPosition;
            revealPulseVisual.transform.rotation = Quaternion.identity;
            revealPulseVisual.transform.localScale = plan.VisualScale;
        }

        private void HideBeacon()
        {
            hoverTime = 0f;
            currentWorldPosition = Vector3.zero;

            if (beaconVisual != null)
            {
                beaconVisual.SetActive(false);
            }
        }

        private void HideRevealPulse()
        {
            revealPulseRemainingSeconds = 0f;
            currentRevealPulseBasePosition = Vector3.zero;

            if (revealPulseVisual != null)
            {
                revealPulseVisual.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            if (keyItemPickup == null)
            {
                keyItemPickup = GetComponent<KeyItemPickup>();
            }

            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }
    }
}
