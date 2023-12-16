using Keepix.PluginSystem;
using Plugin.Tezos.src;
using Plugin.Tezos.src.Services;
using Plugin.Tezos.src.DTO;

namespace Plugin.Tezos.Commands
{
    public class Setup
    {
        private static PluginStateManager stateManager;

        [KeepixPluginFn("install")]
        public static async Task<bool> OnInstall(WalletInput input)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsNotContainnerRunning,
                async () => { return true; });
            if (!allRulesPassed)
            {
                return false;
            }
            {
                stateManager = PluginStateManager.GetStateManager();
                stateManager.DB.Store("STATE", PluginStateEnum.INSTALLING_SNAPSHOT);
                var isSnapshotDownloaded = await SetupService.DownloadSnapshot(Configurations.TEZOS_SNAPSHOT, "/root/tezos-mainnet.rolling");
                if (!isSnapshotDownloaded)
                {
                    LoggerService.Log("Downloaded of the snapshot failed");
                    return false;
                }

                stateManager.DB.Store("SECRET_WALLET", input.WalletSecretKey);
                await SetupService.setupNode(stateManager);
            }

            return true;
        }

        [KeepixPluginFn("start-sync")]
        public static async Task<bool> OnStartSynchronization()
        {

            await ProcessService.ExecuteCommand("docker", "compose up import -d");
            stateManager = PluginStateManager.GetStateManager();
            stateManager.DB.Store("STATE", PluginStateEnum.STARTING_SYNC);
            return true;
        }

        [KeepixPluginFn("setup-node")]
        public static async Task<bool> OnSetupNode()
        {
            stateManager = PluginStateManager.GetStateManager();
            await SetupService.setupNode(stateManager);
            return true;
        }


        [KeepixPluginFn("start-config")]
        public static async Task<bool> OnStartConfiguration()
        {

            await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling");
            Thread.Sleep(1000);
            await SetupService.initConfig();
            stateManager = PluginStateManager.GetStateManager();
            stateManager.DB.Store("STATE", PluginStateEnum.NODE_RUNNING);
            return true;
        }

        [KeepixPluginFn("uninstall")]
        public static async Task<bool> OnUInstall()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning);
            if (!allRulesPassed)
            {
                return false;
            }

            stateManager = PluginStateManager.GetStateManager();

            try
            {
                await ProcessService.ExecuteCommand("docker", "stop octez-public-node-rolling");
                await ProcessService.ExecuteCommand("docker", "rm octez-public-node-rolling");
                Thread.Sleep(1000);

                await ProcessService.ExecuteCommand("docker", "stop octez-snapshot-import");
                await ProcessService.ExecuteCommand("docker", "rm octez-snapshot-import");
                Thread.Sleep(1000);
                await ProcessService.ExecuteCommand("docker", "rmi tezos/tezos");

                Thread.Sleep(1000);
                await ProcessService.ExecuteCommand("docker", "volume rm mainnet-client mainnet-node");
                stateManager.DB.Store("STATE", PluginStateEnum.NO_STATE);

                stateManager.DB.UnStore("WalletAddress");
                stateManager.DB.UnStore("countSyncState");
                stateManager.DB.UnStore("lastSyncTimeStamp");
                stateManager.DB.UnStore("rateOfProgressInSeconds");
                stateManager.DB.UnStore("DELEGATED_BALANCE");
                stateManager.DB.UnStore("DELEGATED");
                stateManager.DB.UnStore("DELEGATED_ADDRESS");
                stateManager.DB.UnStore("DELEGATED_DATE");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        [KeepixPluginFn("start")]
        public static async Task<bool> OnStartFunc()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsNotContainnerRunning);
            if (!allRulesPassed)
            {
                return false;
            }

            try
            {
                stateManager = PluginStateManager.GetStateManager();
                await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling");
                await SetupService.initConfig();
                stateManager.DB.Store("STATE", PluginStateEnum.NODE_RUNNING);
                LoggerService.Log("Smart-node successfully started");
                return true;

            }
            catch
            {
                return false;
            }

        }

        [KeepixPluginFn("stop")]
        public static async Task<bool> OnStopFunc()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return false;
            }

            try
            {
                await ProcessService.ExecuteCommand("docker", "stop octez-public-node-rolling");
                stateManager = PluginStateManager.GetStateManager();
                stateManager.DB.Store("STATE", PluginStateEnum.NODE_STOPPED);
            }
            catch
            {
                return false;
            }
            return true;
        }

        [KeepixPluginFn("is-synchronized")]
        public static async Task<bool> IsSynchronized()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return false;
            }

            var result = await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling octez-client bootstrapped");
            return result.Contains("Node is bootstrapped");
        }
    }
}
