using System;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.Interaction
{
    public readonly struct DoorRequirementHintRequest
    {
        public DoorRequirementHintRequest(string requiredAreaId, string requiredEncounterId, string requiredKeyItemId)
        {
            RequiredAreaId = requiredAreaId ?? string.Empty;
            RequiredEncounterId = requiredEncounterId ?? string.Empty;
            RequiredKeyItemId = requiredKeyItemId ?? string.Empty;
        }

        public string RequiredAreaId { get; }

        public string RequiredEncounterId { get; }

        public string RequiredKeyItemId { get; }
    }

    [DisallowMultipleComponent]
    public sealed class DoorRequirementHintTrigger : MonoBehaviour
    {
        [SerializeField] private string requiredAreaId = string.Empty;
        [SerializeField] private string requiredEncounterId = string.Empty;
        [SerializeField] private string requiredKeyItemId = string.Empty;
        [SerializeField] private float retriggerCooldownSeconds = 1.2f;
        [SerializeField] private ChapterProgressService chapterProgressService;

        private float nextAllowedHintTime;

        public static event Action<DoorRequirementHintRequest> BlockedRouteReached;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerCharacter>() == null)
            {
                return;
            }

            ResolveReferences();

            if (chapterProgressService == null)
            {
                return;
            }

            if (chapterProgressService.MeetsRequirements(requiredAreaId, requiredEncounterId, requiredKeyItemId))
            {
                return;
            }

            if (Time.unscaledTime < nextAllowedHintTime)
            {
                return;
            }

            nextAllowedHintTime = Time.unscaledTime + Mathf.Max(0f, retriggerCooldownSeconds);
            BlockedRouteReached?.Invoke(new DoorRequirementHintRequest(requiredAreaId, requiredEncounterId, requiredKeyItemId));
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }
    }
}
