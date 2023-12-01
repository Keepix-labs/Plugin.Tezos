using Docker.DotNet.Models;
using Docker.DotNet;
using Keepix.PluginSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Tezos.src.Utils;
using Plugin.Tezos.src;
using Plugin.Tezos.src.Services;
using System.Runtime.ConstrainedExecution;
using Plugin.Tezos.src.DTO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Plugin.Tezos.Commands
{
    public class Setup
    {

        private static string secretWallet = "./db.json";
        private static PluginStateManager stateManager;


        [KeepixPluginFn("install")]
        public static async Task<bool> OnInstall(WalletInput input)
        {
            var stateService = PluginStateManager.GetStateManager();
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsNotContainnerRunning, SetupService.IsCliInstalled,
                async () => { return true; });
            if (!allRulesPassed)
            {
                return false;
            }

            dynamic metadata = JObject.Parse(await ProcessService.ExecuteCommand("curl", $" -L  {Configurations.TEZOS_SNAPSHOT}"));
            if (metadata.url == null || metadata.url == "")
            {
                LoggerService.Log("Can't get url of the snapshot");
                return false;
            }

            stateManager = PluginStateManager.GetStateManager();
            stateManager.DB.Store("STATE", PluginStateEnum.INSTALLING_SNAPSHOT);
            var isSnapshotDownloaded = await SetupService.DownloadSnapshot((string)metadata.url, "/root/tezos-mainnet.rolling");
            if (!isSnapshotDownloaded)
            {
                LoggerService.Log("Downloaded of the snapshot failed");
                return false;
            }

            await File.WriteAllTextAsync(secretWallet, input.WalletSecretKey);
            stateManager.DB.Store("STATE", PluginStateEnum.INSTALLING_NODE);

            try
            {
                LoggerService.Log("Starting to setup the tezos container");

                await ProcessService.ExecuteCommand("docker", "volume rm mainnet-client mainnet-node");
                await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling");
                Thread.Sleep(1000);
                await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling rm /var/run/tezos/node/data/lock");
                await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling rm -r /var/run/tezos/node/data");
                Thread.Sleep(1000);
                await ProcessService.ExecuteCommand("docker", "compose stop node_rolling");
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/context");
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/store");
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/lock");
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/daily_logs/");
                Thread.Sleep(1000);
                await SetupService.Synchronize(input, stateManager);
                return true;

            }
            catch
            {
                return false;
            }

            return true;
        }

        [KeepixPluginFn("uninstall")]
        public static async Task<bool> OnUinstall()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning);
            if (!allRulesPassed)
            {
                return false;
            }

            try
            {
                LoggerService.Log("Starting to setup the tezos container");

                await ProcessService.ExecuteCommand("docker", "volume rm mainnet-client mainnet-node");
                Thread.Sleep(1000);

                await ProcessService.ExecuteCommand("docker", "stop octez-public-node-rolling");
                await ProcessService.ExecuteCommand("docker", "rm octez-public-node-rolling");
                Thread.Sleep(1000);

                await ProcessService.ExecuteCommand("docker", "stop octez-snapshot-import");
                await ProcessService.ExecuteCommand("docker", "rm octez-snapshot-import");
                Thread.Sleep(1000);
                await ProcessService.ExecuteCommand("docker", "rmi tezos/tezos");

            }
            catch
            {
                return false;
            }

            return true;
        }

        [KeepixPluginFn("start")]
        public static async Task<bool> OnStartFunc()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsNotContainnerRunning);
            if (!allRulesPassed)
            {
                return false;
            }

            await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling");

            return true;
        }

        [KeepixPluginFn("stop")]
        public static async Task<bool> OnStopFunc()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return false;
            }


            await ProcessService.ExecuteCommand("docker", "stop octez-public-node-rolling");
            return true;
        }

        [KeepixPluginFn("is-synchronized")]
        public static async Task<bool> IsSynchronized()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning, SetupService.IsCliInstalled);
            if (!allRulesPassed)
            {
                return false;
            }

            var result = await ProcessService.ExecuteCommand("octez-client", "bootstrapped");
            return result.Contains("Node is bootstrapped");
        }
    }
}
