using CampusRPG.AI;
using CampusRPG.Interaction;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossPresentationRig : MonoBehaviour
    {
        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private EncounterController bossEncounter;
        [SerializeField] private BossTelegraphStyleSO telegraphStyle;
        [SerializeField] private string encounterLabel = "Boss Encounter";
        [SerializeField] private string bossDisplayName = "Campus Gatekeeper";

        public void Configure(EnemyBrain boss, EncounterController encounter, BossTelegraphStyleSO style = null)
        {
            bossEnemy = boss;
            bossEncounter = encounter;

            if (style != null || telegraphStyle == null)
            {
                telegraphStyle = style;
            }
        }

        private void Awake()
        {
            ApplyConfiguration();
        }

        private void OnEnable()
        {
            ApplyConfiguration();
        }

        public void ApplyConfiguration()
        {
            if (bossEnemy == null)
            {
                return;
            }

            EnsureComponent<BossBarPresenter>().Configure(bossEnemy, bossDisplayName);
            EnsureComponent<BossIntroPresenter>().Configure(bossEnemy, encounterLabel, bossDisplayName);
            EnsureComponent<BossAttackCuePresenter>().Configure(bossEnemy, telegraphStyle);
            EnsureComponent<BossCombatHintView>().Configure(bossEncounter);
            EnsureComponent<BossThreatPulsePresenter>().Configure(bossEnemy, telegraphStyle);
            EnsureComponent<BossGroundTelegraphPresenter>().Configure(bossEnemy, telegraphStyle);
            EnsureComponent<BossImpactMarkerPresenter>().Configure(bossEnemy, telegraphStyle);
            EnsureComponent<BossSpawnFlarePresenter>().Configure(bossEnemy, telegraphStyle);

            if (bossEncounter != null)
            {
                EnsureComponent<BossArenaStatusPresenter>().Configure(bossEncounter);
            }
        }

        private T EnsureComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
