using System;
using UnityEngine;

namespace CampusRPG.Save
{
    [CreateAssetMenu(fileName = "SO_ChapterMapDefinition", menuName = "CampusRPG/Save/Chapter Map Definition")]
    public sealed class ChapterMapDefinitionSO : ScriptableObject
    {
        [SerializeField] private string chapterId = Chapter01Ids.Chapter;
        [SerializeField] private MapZoneDefinition[] zones = Array.Empty<MapZoneDefinition>();
        [SerializeField] private RouteGateDefinition[] routeGates = Array.Empty<RouteGateDefinition>();

        public string ChapterId => chapterId;

        public MapZoneDefinition[] Zones => zones ?? Array.Empty<MapZoneDefinition>();

        public RouteGateDefinition[] RouteGates => routeGates ?? Array.Empty<RouteGateDefinition>();

        public void Configure(
            string chapterId,
            MapZoneDefinition[] zones,
            RouteGateDefinition[] routeGates)
        {
            this.chapterId = string.IsNullOrWhiteSpace(chapterId) ? Chapter01Ids.Chapter : chapterId;
            this.zones = zones ?? Array.Empty<MapZoneDefinition>();
            this.routeGates = routeGates ?? Array.Empty<RouteGateDefinition>();
        }

        public bool TryGetZone(string zoneId, out MapZoneDefinition zone)
        {
            MapZoneDefinition[] configuredZones = Zones;

            for (int i = 0; i < configuredZones.Length; i++)
            {
                if (configuredZones[i] != null && configuredZones[i].ZoneId == zoneId)
                {
                    zone = configuredZones[i];
                    return true;
                }
            }

            zone = null;
            return false;
        }

        public bool TryGetRouteGate(string gateId, out RouteGateDefinition routeGate)
        {
            RouteGateDefinition[] configuredRouteGates = RouteGates;

            for (int i = 0; i < configuredRouteGates.Length; i++)
            {
                if (configuredRouteGates[i] != null && configuredRouteGates[i].GateId == gateId)
                {
                    routeGate = configuredRouteGates[i];
                    return true;
                }
            }

            routeGate = null;
            return false;
        }

        [Serializable]
        public sealed class MapZoneDefinition
        {
            [SerializeField] private string zoneId = string.Empty;
            [SerializeField] private string sceneObjectName = string.Empty;
            [SerializeField] private string displayName = string.Empty;
            [SerializeField] private string areaId = string.Empty;
            [SerializeField] private string objectiveHint = string.Empty;
            [SerializeField] private string primaryEncounterId = string.Empty;
            [SerializeField] private string checkpointId = string.Empty;
            [SerializeField] private string rewardKeyItemId = string.Empty;
            [SerializeField] private bool optionalRoute;
            [SerializeField] private Vector3 center;
            [SerializeField] private Vector3 size = Vector3.one;

            public MapZoneDefinition(
                string zoneId,
                string sceneObjectName,
                string displayName,
                string areaId,
                string objectiveHint,
                string primaryEncounterId,
                string checkpointId,
                string rewardKeyItemId,
                bool optionalRoute,
                Vector3 center,
                Vector3 size)
            {
                this.zoneId = zoneId;
                this.sceneObjectName = sceneObjectName;
                this.displayName = displayName;
                this.areaId = areaId;
                this.objectiveHint = objectiveHint;
                this.primaryEncounterId = primaryEncounterId;
                this.checkpointId = checkpointId;
                this.rewardKeyItemId = rewardKeyItemId;
                this.optionalRoute = optionalRoute;
                this.center = center;
                this.size = size;
            }

            public string ZoneId => zoneId;

            public string SceneObjectName => sceneObjectName;

            public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? zoneId : displayName;

            public string AreaId => areaId;

            public string ObjectiveHint => objectiveHint;

            public string PrimaryEncounterId => primaryEncounterId;

            public string CheckpointId => checkpointId;

            public string RewardKeyItemId => rewardKeyItemId;

            public bool OptionalRoute => optionalRoute;

            public Vector3 Center => center;

            public Vector3 Size => size;
        }

        [Serializable]
        public sealed class RouteGateDefinition
        {
            [SerializeField] private string gateId = string.Empty;
            [SerializeField] private string displayName = string.Empty;
            [SerializeField] private string fromZoneId = string.Empty;
            [SerializeField] private string toZoneId = string.Empty;
            [SerializeField] private string requiredEncounterId = string.Empty;
            [SerializeField] private string requiredKeyItemId = string.Empty;
            [SerializeField] private bool opensShortcut;

            public RouteGateDefinition(
                string gateId,
                string displayName,
                string fromZoneId,
                string toZoneId,
                string requiredEncounterId,
                string requiredKeyItemId,
                bool opensShortcut)
            {
                this.gateId = gateId;
                this.displayName = displayName;
                this.fromZoneId = fromZoneId;
                this.toZoneId = toZoneId;
                this.requiredEncounterId = requiredEncounterId;
                this.requiredKeyItemId = requiredKeyItemId;
                this.opensShortcut = opensShortcut;
            }

            public string GateId => gateId;

            public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gateId : displayName;

            public string FromZoneId => fromZoneId;

            public string ToZoneId => toZoneId;

            public string RequiredEncounterId => requiredEncounterId;

            public string RequiredKeyItemId => requiredKeyItemId;

            public bool OpensShortcut => opensShortcut;
        }
    }
}
