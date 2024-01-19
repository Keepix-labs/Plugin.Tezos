using Plugin.Tezos.src.DTO;
using Plugin.Tezos.src.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.src.Services
{
    public class SetupService
    {

        private static PluginStateManager stateManager;
        public static async Task<bool> IsDockerRunning()
        {
            try
            {
                var res = await ProcessService.ExecuteCommand("docker", "info");
                return res != string.Empty;
            }
            catch
            {
                LoggerService.Log("Docker is not live on your device, please start it");
                return false;
            }

        }

        public static async Task<bool> IsContainnerRunning()
        {
            try
            {
                var res = await ProcessService.ExecuteCommand("docker", "ps");
                return res.Contains("octez-public-node-rolling");
            }
            catch
            {
                LoggerService.Log("Containner is not activated");
                return false;
            }
        }

        public static async Task<bool> IsSnapshotImportRunning()
        {
            try
            {
                var res = await ProcessService.ExecuteCommand("docker", "ps");
                return res.Contains("octez-snapshot-import");
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> IsNotContainnerRunning()
        {
            try
            {
                var res = await ProcessService.ExecuteCommand("docker", "ps");
                return !res.Contains("octez-public-node-rolling");
            }
            catch
            {
                LoggerService.Log("Containner is already activated");
                return false;
            }
        }


        public async static Task<bool> DownloadSnapshot(string url, string path)
        {
            using (var client = new HttpClient())
            {

                LoggerService.Log("Start downloading tezos snapshot");
                try
                {
                    await client.DownloadFileAsync(url, path);
                    return true;
                }
                catch (Exception ex)
                {
                    LoggerService.Log(ex.ToString());
                    return false;
                }
            }
        }

        public static async Task Synchronize(WalletInput input, PluginStateManager state)
        {
            try
            {
                
                var envVars = new Dictionary<string, string>
                {
                    { "SNAPSHOT_PATH", SetupService.GetSnapshotPath() }
                };
                state.DB.Store("STATE", PluginStateEnum.INSTALLING_NODE);
                await ProcessService.ExecuteCommand("docker", "compose up import", null, envVars);
                state.DB.Store("STATE", PluginStateEnum.NODE_RUNNING);
                await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling", null, envVars);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static async Task initConfig()
        {
            await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client --endpoint http://localhost:8732 config update");
            stateManager = PluginStateManager.GetStateManager();
            var walletSecret = "";
            try { walletSecret = stateManager.DB.Retrieve<string>("WalletAddress"); } catch { }
            if (walletSecret != "")
                await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client import secret key WalletAddress unencrypted:{walletSecret}");
        }

        public static async Task setupNode(PluginStateManager stateManager)
        {

            try
            {
                LoggerService.Log("Starting to setup the tezos container");
                try
                {
                    await ProcessService.ExecuteCommand("docker", "volume rm mainnet-client mainnet-node");

                }
                catch { }
                Thread.Sleep(1000);

                var envVars = new Dictionary<string, string>
                {
                    { "SNAPSHOT_PATH", SetupService.GetSnapshotPath() }
                };

                await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling", null, envVars);
                Thread.Sleep(4000);
                await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling rm /var/run/tezos/node/data/lock");
                await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling rm -r /var/run/tezos/node/data");
                Thread.Sleep(1000);
                await ProcessService.ExecuteCommand("docker", "compose stop node_rolling", null, envVars);
                await Task.Delay(1000);
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/context");
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/store");
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/lock");
                await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/daily_logs");
                stateManager.DB.Store("STATE", PluginStateEnum.INSTALLING_NODE);
            }
            catch
            {
                LoggerService.Log("Installing node failed");
            }
        }

        public static string GetSnapshotPath()
        {
            var username = Environment.UserName;
            var basePath = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string tempPath = System.IO.Path.GetTempPath();
                basePath = Path.Combine(tempPath, "tezos-mainnet.rolling");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                basePath = $"/tmp/tezos-mainnet.rolling"; 
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                basePath = $"/tmp/tezos-mainnet.rolling"; 

            }

            return basePath;

        }

        public static async Task<bool> ApplyRules(params Func<Task<bool>>[] ruleFunctions)
        {
            var tasks = ruleFunctions.Select(rule => rule()).ToArray();
            await Task.WhenAll(tasks);
            return tasks.All(task => task.Result);
        }
    }
}
