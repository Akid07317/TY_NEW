using UnityEngine;

namespace CampusRPG.Combat
{
    [DisallowMultipleComponent]
    public sealed class TransientVisualEffect : MonoBehaviour
    {
        [SerializeField] private float lifetimeSeconds = 0.18f;
        [SerializeField] private Vector3 startScale = new Vector3(0.15f, 0.15f, 0.15f);
        [SerializeField] private Vector3 endScale = new Vector3(0.8f, 0.8f, 0.8f);

        private float elapsedSeconds;

        private void OnEnable()
        {
            elapsedSeconds = 0f;
            transform.localScale = startScale;
        }

        private void Update()
        {
            float duration = Mathf.Max(0.01f, lifetimeSeconds);
            elapsedSeconds += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedSeconds / duration);
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, t);

            if (elapsedSeconds >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
