using UnityEngine;

namespace CampusRPG.Camera
{
    [DisallowMultipleComponent]
    public sealed class LockOnTarget : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;

        public Transform TargetTransform => targetTransform != null ? targetTransform : transform;
    }
}
