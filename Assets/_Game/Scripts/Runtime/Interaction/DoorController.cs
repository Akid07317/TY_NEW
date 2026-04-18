using System;
using CampusRPG.Character;
using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private string requiredAreaId = string.Empty;
        [SerializeField] private string requiredEncounterId = string.Empty;
        [SerializeField] private string requiredKeyItemId = string.Empty;
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private GameObject[] blockersToDisableWhenOpen = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] objectsToEnableWhenOpen = Array.Empty<GameObject>();

        private bool isOpen;

        private void Awake()
        {
            ResolveReferences();
            RefreshState();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged += HandleProgressChanged;
            }

            RefreshState();
        }

        private void OnDisable()
        {
            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged -= HandleProgressChanged;
            }
        }

        private void HandleProgressChanged()
        {
            RefreshState();
        }

        private void RefreshState()
        {
            bool shouldOpen = chapterProgressService != null
                && chapterProgressService.MeetsRequirements(requiredAreaId, requiredEncounterId, requiredKeyItemId);

            if (shouldOpen == isOpen)
            {
                return;
            }

            isOpen = shouldOpen;

            GameObject[] blockers = blockersToDisableWhenOpen != null && blockersToDisableWhenOpen.Length > 0
                ? blockersToDisableWhenOpen
                : new[] { gameObject };

            for (int i = 0; i < blockers.Length; i++)
            {
                if (blockers[i] != null)
                {
                    blockers[i].SetActive(!isOpen);
                }
            }

            for (int i = 0; i < objectsToEnableWhenOpen.Length; i++)
            {
                if (objectsToEnableWhenOpen[i] != null)
                {
                    objectsToEnableWhenOpen[i].SetActive(isOpen);
                }
            }
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }
    }
}
