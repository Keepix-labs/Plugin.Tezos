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

namespace Plugin.Tezos.Commands
{
    public class Setup
    {
        [KeepixPluginFn("pre-install")]
        public static async Task<bool> OnPreInstall()
        {
            using(var client = new HttpClient())
            {
                try
                {
                    Console.WriteLine("Start downloading snapshot");
                    await client.DownloadFileAsync(Configurations.TEZOS_SNAPSHOT, "/root/tezos-mainnet.rolling");
                    Console.WriteLine("Finish");
                }
                catch(Exception ex)
                {
                    // log error
                    Console.WriteLine(ex);
                }
            }
            return true;
        }

        [KeepixPluginFn("install")]
        public static async Task<bool> OnInstall()
        {
            await ProcessService.ExecuteCommand("docker","compose up -d node_rolling");
            await ProcessService.ExecuteCommand("docker","exec -it octez-public-node-rolling rm -rf /var/run/tezos/node/data");
            await ProcessService.ExecuteCommand("docker","compose stop node_rolling");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/context");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/store");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/lock");
            await ProcessService.ExecuteCommand("rm", "-rf /var/lib/docker/volumes/mainnet-node/_data/data/daily_logs/");
            await ProcessService.ExecuteCommand("docker", "compose up import");

            return true;
        }


        [KeepixPluginFn("start")]
        public static async Task<bool> OnStartFun()
        {
            using var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

            var config = new CreateContainerParameters
            {
                Image = "tezos/tezos:latest",
                Cmd = new List<string> {  }
            };

            var createdContainer = await client.Containers.CreateContainerAsync(config);

            return await client.Containers.StartContainerAsync(createdContainer.ID, new ContainerStartParameters());
        }

    }
}
