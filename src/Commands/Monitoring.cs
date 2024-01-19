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
            stateManager = PluginStateManager.GetStateManager();
            bool isNodeDelegated = false;
            string DelegatedAddress = "";
            DateTime? DelegatedDate = null;
            double DelegatedBalance = 0.0;

            try
            {
                isNodeDelegated = stateManager.DB.Retrieve<bool>("DELEGATED");
                DelegatedAddress = stateManager.DB.Retrieve<string>("DELEGATED_ADDRESS");
                DelegatedDate = stateManager.DB.Retrieve<DateTime>("DELEGATED_DATE");
                DelegatedBalance = stateManager.DB.Retrieve<double>("DELEGATED_BALANCE");

            }
            catch (Exception) { }
            string exitCode = "'0'";
            try
            {
                exitCode = await ProcessService.ExecuteCommand("docker", "inspect octez-snapshot-import --format='{{.State.ExitCode}}'");
            }
            catch (Exception) { }

            return JsonConvert.SerializeObject(new
            {
                NodeState = stateManager.State.ToString(),
                Alive = await SetupService.IsContainnerRunning(),
                IsSnapshotImportRunning = await SetupService.IsSnapshotImportRunning(),
                SnapshotImportExitCode = exitCode.Trim(),
                IsDelegated = isNodeDelegated,
                DelegatedAddress,
                DelegatedDate,
                DelegatedBalance
            });
        }
    }
}
