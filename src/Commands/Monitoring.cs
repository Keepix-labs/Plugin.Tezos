using Keepix.PluginSystem;
using Newtonsoft.Json;
using Plugin.Tezos.src.DTO;
using Plugin.Tezos.src.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.Commands
{
    public class Monitoring
    {

        private static PluginStateManager stateManager;

        [KeepixPluginFn("status")]
        public static async Task<string> OnStatus(WalletInput input)
        {
            bool isNodeRegistered = false;
            stateManager = PluginStateManager.GetStateManager();
            try { isNodeRegistered = stateManager.DB.Retrieve<bool>("REGISTERED"); } catch (Exception) { }
            return JsonConvert.SerializeObject(new
            {
                NodeState = stateManager.State.ToString(),
                Alive = await SetupService.IsContainnerRunning(),
                IsRegistered = isNodeRegistered
            });
        }
    }
}
