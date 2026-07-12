using System;
using UnityEngine;

namespace CampusRPG.Multiplayer
{
    public static class MultiplayerCommandLineBootstrap
    {
        private static MultiplayerNetworkSessionService clientSession;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ScheduleClientFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (!MultiplayerCommandLineUtility.HasFlag(args, "--multiplayer-client", "-multiplayer-client"))
            {
                return;
            }

            GameObject bootstrap = new GameObject("P1_5_ClientBootstrap");
            UnityEngine.Object.DontDestroyOnLoad(bootstrap);
            bootstrap.AddComponent<MultiplayerClientBootstrapRunner>().Configure(args);
        }

        internal static void StartClientFromCommandLine(string[] args)
        {
            MultiplayerNetworkSessionSettings defaults = MultiplayerNetworkSessionSettings.CreateDefaultClientSettings();
            string connectAddress = MultiplayerCommandLineUtility.ReadStringArgument(
                args,
                defaults.ConnectAddress,
                "--server-address",
                "-serverAddress",
                "--connect-address",
                "-connectAddress");
            int port = MultiplayerCommandLineUtility.ReadIntArgument(
                args,
                defaults.Port,
                "--network-port",
                "-networkPort",
                "--multiplayer-port",
                "-multiplayerPort");
            string playerPrefabResourcePath = MultiplayerCommandLineUtility.ReadStringArgument(
                args,
                defaults.PlayerPrefabResourcePath,
                "--network-player-prefab",
                "-networkPlayerPrefab");
            int quitAfterSeconds = MultiplayerCommandLineUtility.ReadIntArgument(
                args,
                0,
                "--quit-after-seconds",
                "-quitAfterSeconds");
            bool smokeReportEnabled = MultiplayerCommandLineUtility.ReadBoolArgument(
                args,
                false,
                "--multiplayer-smoke-report",
                "-multiplayerSmokeReport");
            string smokeReportLabel = MultiplayerCommandLineUtility.ReadStringArgument(
                args,
                "client",
                "--smoke-report-label",
                "-smokeReportLabel");
            int smokeReportIntervalSeconds = MultiplayerCommandLineUtility.Clamp(
                MultiplayerCommandLineUtility.ReadIntArgument(
                    args,
                    1,
                    "--smoke-report-interval-seconds",
                    "-smokeReportIntervalSeconds"),
                1,
                60);
            bool smokeMoveEnabled = MultiplayerCommandLineUtility.ReadBoolArgument(
                args,
                false,
                "--multiplayer-smoke-move",
                "-multiplayerSmokeMove");
            float smokeMoveX = Mathf.Clamp(
                MultiplayerCommandLineUtility.ReadFloatArgument(
                    args,
                    0f,
                    "--smoke-move-x",
                    "-smokeMoveX"),
                -1f,
                1f);
            float smokeMoveY = Mathf.Clamp(
                MultiplayerCommandLineUtility.ReadFloatArgument(
                    args,
                    0f,
                    "--smoke-move-y",
                    "-smokeMoveY"),
                -1f,
                1f);
            float smokeMoveDurationSeconds = Mathf.Max(
                0f,
                MultiplayerCommandLineUtility.ReadFloatArgument(
                    args,
                    0f,
                    "--smoke-move-duration-seconds",
                    "-smokeMoveDurationSeconds"));
            float smokeMoveDelaySeconds = Mathf.Max(
                0f,
                MultiplayerCommandLineUtility.ReadFloatArgument(
                    args,
                    0f,
                    "--smoke-move-delay-seconds",
                    "-smokeMoveDelaySeconds"));
            bool smokeAttackEnabled = MultiplayerCommandLineUtility.ReadBoolArgument(
                args,
                false,
                "--multiplayer-smoke-attack",
                "-multiplayerSmokeAttack",
                "--multiplayer-smoke-damage",
                "-multiplayerSmokeDamage");
            string smokeAttackId = MultiplayerCommandLineUtility.ReadStringArgument(
                args,
                NetworkServerAttackProfile.Light01AttackId,
                "--smoke-attack-id",
                "-smokeAttackId");
            int smokeAttackDamageAmount = MultiplayerCommandLineUtility.ReadIntArgument(
                args,
                0,
                "--smoke-attack-damage-amount",
                "-smokeAttackDamageAmount",
                "--smoke-damage-amount",
                "-smokeDamageAmount");
            float smokeAttackDelaySeconds = Mathf.Max(
                0f,
                MultiplayerCommandLineUtility.ReadFloatArgument(
                    args,
                    3f,
                    "--smoke-attack-delay-seconds",
                    "-smokeAttackDelaySeconds",
                    "--smoke-damage-delay-seconds",
                    "-smokeDamageDelaySeconds"));
            int smokeAttackCount = MultiplayerCommandLineUtility.Clamp(
                MultiplayerCommandLineUtility.ReadIntArgument(
                    args,
                    1,
                    "--smoke-attack-count",
                    "-smokeAttackCount"),
                0,
                16);
            float smokeAttackIntervalSeconds = Mathf.Max(
                0.05f,
                MultiplayerCommandLineUtility.ReadFloatArgument(
                    args,
                    0.75f,
                    "--smoke-attack-interval-seconds",
                    "-smokeAttackIntervalSeconds"));

            NetworkPlayerAvatar.ConfigureSmokeMoveInput(
                smokeMoveEnabled ? new Vector2(smokeMoveX, smokeMoveY) : Vector2.zero,
                smokeMoveDelaySeconds,
                smokeMoveDurationSeconds);
            NetworkPlayerAvatar.ConfigureSmokeAttackRequest(
                smokeAttackId,
                smokeAttackEnabled ? smokeAttackDamageAmount : 0,
                smokeAttackDelaySeconds,
                smokeAttackCount,
                smokeAttackIntervalSeconds);

            try
            {
                clientSession = new MultiplayerNetworkSessionService();
                bool clientStarted = clientSession.StartClient(
                    new MultiplayerNetworkSessionSettings(
                        defaults.ListenAddress,
                        connectAddress,
                        port,
                        defaults.MaxPlayers,
                        playerPrefabResourcePath,
                        null));

                if (clientStarted && smokeReportEnabled)
                {
                    GameObject smokeReporter = new GameObject("P1_5_ClientSmokeReporter");
                    UnityEngine.Object.DontDestroyOnLoad(smokeReporter);
                    smokeReporter
                        .AddComponent<MultiplayerClientSmokeReporter>()
                        .Configure(smokeReportLabel, smokeReportIntervalSeconds);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[MultiplayerNetwork] Failed to start NGO client: " + exception);
                StopClientSession();
            }

            if (quitAfterSeconds > 0)
            {
                GameObject quitTimer = new GameObject("P1_5_ClientQuitTimer");
                UnityEngine.Object.DontDestroyOnLoad(quitTimer);
                quitTimer.AddComponent<MultiplayerClientQuitTimer>().Configure(quitAfterSeconds);
            }
        }

        internal static void StopClientSession()
        {
            NetworkPlayerAvatar.ConfigureSmokeMoveInput(Vector2.zero);
            NetworkPlayerAvatar.ConfigureSmokeAttackRequest(0, 0f);

            if (clientSession == null)
            {
                return;
            }

            clientSession.Dispose();
            clientSession = null;
        }
    }

    internal sealed class MultiplayerClientBootstrapRunner : MonoBehaviour
    {
        private string[] args;
        private bool started;

        public void Configure(string[] commandLineArgs)
        {
            args = commandLineArgs ?? Array.Empty<string>();
        }

        private void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            MultiplayerCommandLineBootstrap.StartClientFromCommandLine(args);
        }

        private void OnApplicationQuit()
        {
            MultiplayerCommandLineBootstrap.StopClientSession();
        }
    }

    internal sealed class MultiplayerClientQuitTimer : MonoBehaviour
    {
        private float quitAt;

        public void Configure(int quitAfterSeconds)
        {
            quitAt = Time.realtimeSinceStartup + Mathf.Max(1, quitAfterSeconds);
        }

        private void Update()
        {
            if (quitAt <= 0f || Time.realtimeSinceStartup < quitAt)
            {
                return;
            }

            Debug.Log("[MultiplayerNetwork] Client quit timer reached; shutting down.");
            Application.Quit(0);
        }
    }
}
