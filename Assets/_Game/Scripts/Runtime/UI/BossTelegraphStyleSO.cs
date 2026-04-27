using UnityEngine;

namespace CampusRPG.UI
{
    [CreateAssetMenu(menuName = "CampusRPG/UI/Boss Telegraph Style", fileName = "SO_BossTelegraphStyle")]
    public sealed class BossTelegraphStyleSO : ScriptableObject
    {
        [Header("Cue")]
        [SerializeField] private Color cuePanelBackgroundColor = new Color(0.14f, 0.08f, 0.08f, 0.95f);
        [SerializeField] private Color defaultCueAccentColor = new Color(0.95f, 0.8f, 0.42f);
        [SerializeField] private Color straightProjectileCueAccentColor = new Color(0.48f, 0.88f, 0.92f);
        [SerializeField] private Color arcProjectileCueAccentColor = new Color(1f, 0.58f, 0.32f);
        [SerializeField] private Color rangedCueAccentColor = new Color(0.78f, 0.88f, 0.56f);
        [SerializeField] private Color antiAirCueAccentColor = new Color(0.42f, 0.72f, 1f);
        [SerializeField] private Color chaseRollCueAccentColor = new Color(1f, 0.42f, 0.24f);

        [Header("Threat Pulse")]
        [SerializeField] private Color encounterPulseColor = new Color(0.95f, 0.54f, 0.14f, 0.22f);
        [SerializeField] private Color attackPulseColor = new Color(0.88f, 0.16f, 0.14f, 0.26f);

        [Header("World Telegraphs")]
        [SerializeField] private GameObject groundTelegraphVisualPrefab;
        [SerializeField] private GameObject impactMarkerVisualPrefab;
        [SerializeField] private GameObject spawnFlareVisualPrefab;
        [SerializeField] private Material engageTelegraphMaterial;
        [SerializeField] private Material attackTelegraphMaterial;
        [SerializeField] private Material impactMarkerMaterial;
        [SerializeField] private Material spawnFlareMaterial;
        [SerializeField] private Color engageTelegraphColor = new Color(0.96f, 0.7f, 0.22f, 1f);
        [SerializeField] private Color attackTelegraphColor = new Color(0.92f, 0.18f, 0.16f, 1f);
        [SerializeField] private Color impactMarkerColor = new Color(0.98f, 0.28f, 0.2f, 1f);
        [SerializeField] private Color spawnFlareColor = new Color(1f, 0.73f, 0.28f, 1f);

        public Color CuePanelBackgroundColor => cuePanelBackgroundColor;

        public Color DefaultCueAccentColor => defaultCueAccentColor;

        public Color StraightProjectileCueAccentColor => straightProjectileCueAccentColor;

        public Color ArcProjectileCueAccentColor => arcProjectileCueAccentColor;

        public Color RangedCueAccentColor => rangedCueAccentColor;

        public Color AntiAirCueAccentColor => antiAirCueAccentColor;

        public Color ChaseRollCueAccentColor => chaseRollCueAccentColor;

        public Color EncounterPulseColor => encounterPulseColor;

        public Color AttackPulseColor => attackPulseColor;

        public GameObject GroundTelegraphVisualPrefab => groundTelegraphVisualPrefab;

        public GameObject ImpactMarkerVisualPrefab => impactMarkerVisualPrefab;

        public GameObject SpawnFlareVisualPrefab => spawnFlareVisualPrefab;

        public Material EngageTelegraphMaterial => engageTelegraphMaterial;

        public Material AttackTelegraphMaterial => attackTelegraphMaterial;

        public Material ImpactMarkerMaterial => impactMarkerMaterial;

        public Material SpawnFlareMaterial => spawnFlareMaterial;

        public Color EngageTelegraphColor => engageTelegraphColor;

        public Color AttackTelegraphColor => attackTelegraphColor;

        public Color ImpactMarkerColor => impactMarkerColor;

        public Color SpawnFlareColor => spawnFlareColor;
    }
}
