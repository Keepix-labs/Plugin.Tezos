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

        [KeepixPluginFn("install")]
        public static async Task<bool> OnInstall(InstallInput input)
        {

            dynamic metadata = JObject.Parse(await ProcessService.ExecuteCommand("curl", $" -L  {Configurations.TEZOS_SNAPSHOT}"));
            if (metadata.url == null || metadata.url == "")
                return false;
            using (var client = new HttpClient())
            {

                Console.WriteLine("Start downloading");
                try
                {
                    await client.DownloadFileAsync((string)metadata.url, "/root/tezos-mainnet.rolling");
                }
                catch (Exception ex)
                {
                    // log error
                    Console.WriteLine(ex);
                }
            }


            Console.WriteLine("Start sync");
            await File.WriteAllTextAsync(secretWallet, input.WalletSecretKey);

            await ProcessService.ExecuteCommand("docker","compose up -d node_rolling");
            await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling rm /var/run/tezos/node/data/lock");
            await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling rm -r /var/run/tezos/node/data");
            await ProcessService.ExecuteCommand("docker","compose stop node_rolling");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/context");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/store");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/lock");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/daily_logs/");
            await ProcessService.ExecuteCommand("docker", "compose up import");
            await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling");
            await ProcessService.ExecuteCommand("octez-client", "--endpoint http://localhost:8732 config update");
            await ProcessService.ExecuteCommand("octez-client", $"import secret key {input.WalletName} {input.WalletSecretKey}");


            return true;
        }

        [KeepixPluginFn("start")]
        public static async Task<bool> OnStartFunc()
        {
            await ProcessService.ExecuteCommand("docker", "compose up -d node_rolling");
    
            return true;
        }

        [KeepixPluginFn("stop")]
        public static async Task<bool> OnStopFunc()
        {
            await ProcessService.ExecuteCommand("docker", "stop octez-public-node-rolling");
            return false;
        }

    }
}
