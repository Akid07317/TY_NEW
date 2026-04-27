using UnityEngine;

namespace CampusRPG.Save
{
    [DisallowMultipleComponent]
    public sealed class ChapterMapZoneMarker : MonoBehaviour
    {
        [SerializeField] private ChapterMapDefinitionSO mapDefinition;
        [SerializeField] private string zoneId = string.Empty;

        public ChapterMapDefinitionSO MapDefinition => mapDefinition;

        public string ZoneId => zoneId;

        public void Configure(ChapterMapDefinitionSO mapDefinition, string zoneId)
        {
            this.mapDefinition = mapDefinition;
            this.zoneId = zoneId;
        }

        public bool TryGetDefinition(out ChapterMapDefinitionSO.MapZoneDefinition zone)
        {
            if (mapDefinition == null)
            {
                zone = null;
                return false;
            }

            return mapDefinition.TryGetZone(zoneId, out zone);
        }
    }
}
