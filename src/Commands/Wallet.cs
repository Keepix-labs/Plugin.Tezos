using Keepix.PluginSystem;
using Plugin.Tezos.src.DTO;
using Plugin.Tezos.src.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.src.Commands
{
    public class Wallet
    {
        [KeepixPluginFn("balance")]
        public static async Task<string> GetBalance(string accountAddress)
        {

            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning, SetupService.IsCliInstalled);
            if (allRulesPassed)
            {
                return "";
            }

            var balance = await ProcessService.ExecuteCommand("octez-client", $"get balance for {accountAddress}");
            return balance;
        }

        [KeepixPluginFn("transfer")]
        public async Task<string> TransferFunds(string sourceAlias, string destinationAddress, decimal amount, decimal fee)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning, SetupService.IsCliInstalled);
            if (!allRulesPassed)
            {
                return "";
            }

            var command = $"transfer {amount} from {sourceAlias} to {destinationAddress} --fee {fee}";
            var result = await ProcessService.ExecuteCommand("octez-client", command);

            if (string.IsNullOrEmpty(result) || result.Contains("error") || result.Contains("failed"))
            {
                throw new Exception($"Failed to transfer funds. Result: {result}");
            }
            return result;
        }


        [KeepixPluginFn("register")]
        public static async Task<string> RegisterAsDelegate(string alias)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning, SetupService.IsCliInstalled);
            if (!allRulesPassed)
            {
                return "";
            }

            var result = await ProcessService.ExecuteCommand("octez-client", $"register key {alias} as delegate");
            if (!result.Contains("Operation successfully injected") && !result.Contains("already registered as delegate"))
            {
                throw new Exception("Failed to register as delegate. Please check the output for details.");
            }
            return result;
        }

        [KeepixPluginFn("wallet-import")]
        public static async Task<bool> OnWalletImport(WalletInput input)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning, SetupService.IsCliInstalled);
            if (!allRulesPassed)
            {
                return false;
            }
            await ProcessService.ExecuteCommand("octez-client", $"import secret key mywallet unencrypted:{input.WalletSecretKey}");
            return true;
        }


    }
}
