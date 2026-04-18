using System;
using CampusRPG.Character;
using CampusRPG.Composition;
using UnityEngine;

namespace CampusRPG.Save
{
    [DisallowMultipleComponent]
    public sealed class CheckpointRuntimeAnchor : MonoBehaviour
    {
        [SerializeField] private CheckpointDefinitionSO definition;
        [SerializeField] private string checkpointIdOverride = string.Empty;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private bool autoActivateOnTrigger = true;
        [SerializeField] private CheckpointRestoreCoordinator coordinator;

        public string CheckpointId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(checkpointIdOverride))
                {
                    return checkpointIdOverride;
                }

                if (definition != null && !string.IsNullOrWhiteSpace(definition.CheckpointId))
                {
                    return definition.CheckpointId;
                }

                return gameObject.name;
            }
        }

        public Vector3 RespawnPosition
        {
            get
            {
                Transform target = respawnPoint != null ? respawnPoint : transform;
                Vector3 offset = definition != null ? definition.RespawnOffset : Vector3.zero;
                return target.position + offset;
            }
        }

        public Quaternion RespawnRotation => respawnPoint != null ? respawnPoint.rotation : transform.rotation;

        public bool RestoreFullHealth => definition == null || definition.RestoreFullHealth;

        public bool RestoreFullMana => definition == null || definition.RestoreFullMana;

        private void Awake()
        {
            ResolveCoordinator();
            coordinator?.RegisterCheckpoint(this);
        }

        private void OnEnable()
        {
            ResolveCoordinator();
            coordinator?.RegisterCheckpoint(this);
        }

        private void OnDisable()
        {
            coordinator?.UnregisterCheckpoint(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoActivateOnTrigger)
            {
                return;
            }

            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (player == null)
            {
                return;
            }

            ResolveCoordinator();
            coordinator?.ActivateCheckpoint(this);
        }

        private void ResolveCoordinator()
        {
            if (coordinator == null)
            {
                coordinator = GetComponentInParent<CheckpointRestoreCoordinator>();
            }

            if (coordinator == null)
            {
                coordinator = SceneRuntimeReferenceUtility.ResolveCheckpointRestoreCoordinator(coordinator);
            }
        }
    }
}
