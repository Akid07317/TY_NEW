using CampusRPG.Input;
using UnityEngine;

namespace CampusRPG.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;
        [SerializeField] private bool keepAliveAcrossScenes = true;

        public static GameBootstrap Active { get; private set; }

        public InputReader InputReader => inputReader;

        private void Awake()
        {
            if (Active != null && Active != this)
            {
                Destroy(gameObject);
                return;
            }

            Active = this;

            if (keepAliveAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
            }

            if (inputReader != null)
            {
                inputReader.Initialize();
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
