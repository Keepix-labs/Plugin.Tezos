using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Keepix.PluginSystem;
using Newtonsoft.Json;
using Plugin.Tezos.src.Services;

namespace Plugin.Tezos.src.Commands
{
    internal class StateController
    {
        private static PluginStateManager stateManager;

        [KeepixPluginFn("wallet-fetch")]
        public static async Task<string> OnWalletFetch()
        {
            stateManager = PluginStateManager.GetStateManager();
            var isDockerRunning = await SetupService.IsDockerRunning();

            if (!isDockerRunning)
            {
                string? address = stateManager.DB.Retrieve<string>("WalletAddress");
                if (!string.IsNullOrEmpty(address))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Exists = true,
                        Wallet = address
                    });
                }

                LoggerService.Log("Docker is not live on your device, please start it");
                return JsonConvert.SerializeObject(new
                {
                    Exists = false
                });
            }
            var wallet = await FetchNodeWallet();
            if (string.IsNullOrEmpty(wallet))
            {
                LoggerService.Log("You have no wallet loaded yet in your node, please use wallet-import function accordly.");
                return JsonConvert.SerializeObject(new
                {
                    Exists = false
                });
            }
            stateManager.DB.Store("WalletAddress", wallet);
            return JsonConvert.SerializeObject(new
            {
                Exists = true,
                Wallet = wallet
            });
        }

        public static async Task<string> FetchNodeWallet()
        {
            var result = await ProcessService.ExecuteCommand("octez-client", "list known addresses");
            if (!result.Contains("mywallet"))
            {
                return string.Empty;
            }
            return result?.Split(':')[1].Split(' ')[0];
        }

    }
}
