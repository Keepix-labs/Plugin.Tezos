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

        private static PluginStateManager stateManager;


        [KeepixPluginFn("balance")]
        public static async Task<string> GetBalance(string accountAddress)
        {

            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return "";
            }

            var balance = await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client get balance for {accountAddress}");
            return balance;
        }

        [KeepixPluginFn("wallet-balance")]
        public static async Task<double> GetWalletBalance()
        {

            // todo: verify if bootstraped
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return 0;
            }

            var balance = await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling octez-client get balance for WalletAddress");
            return double.Parse(balance.Replace('ꜩ', ' '));
        }

        [KeepixPluginFn("transfer")]
        public async Task<string> TransferFunds(string sourceAlias, string destinationAddress, decimal amount, decimal fee)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return "";
            }

            var command = $"transfer {amount} from {sourceAlias} to {destinationAddress} --fee {fee}";
            var result = await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client {command}");

            if (string.IsNullOrEmpty(result) || result.Contains("error") || result.Contains("failed"))
            {
                throw new Exception($"Failed to transfer funds. Result: {result}");
            }
            return result;
        }

        [KeepixPluginFn("delegating-bakery")]
        public static async Task<string> DelegateToBakery(DelegatingInput input)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return "";
            }

            stateManager = PluginStateManager.GetStateManager();
            if (stateManager.DB.Retrieve<bool>("DELEGATED")) return "failed";
            var balance = await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling octez-client get balance for WalletAddress");
            stateManager.DB.Store("DELEGATED_BALANCE", double.Parse(balance.Replace('ꜩ', ' ')));
            var result = await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client set delegate for WalletAddress to {input.Address}");
            if (result.Contains("failed")) return "failed";
            stateManager.DB.Store("DELEGATED", true);
            stateManager.DB.Store("DELEGATED_ADDRESS", input.Address);
            stateManager.DB.Store("DELEGATED_DATE", DateTime.UtcNow);
            return result;
        }

        [KeepixPluginFn("withdraw-bakery")]
        public static async Task<string> WithdrawToBakery()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return "";
            }

            stateManager = PluginStateManager.GetStateManager();
            var result = await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client withdraw delegate from WalletAddress");
            stateManager.DB.Store("DELEGATED", false);
            stateManager.DB.Store("DELEGATED_ADDRESS", "");
            return result;
        }

        [KeepixPluginFn("register")]
        public static async Task<string> RegisterAsDelegate(string alias)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return "";
            }

            var result = await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client register key {alias} as delegate");
            if (!result.Contains("Operation successfully injected") && !result.Contains("already registered as delegate"))
            {
                throw new Exception("Failed to register as delegate. Please check the output for details.");
            }
            return result;
        }

        [KeepixPluginFn("wallet-import")]
        public static async Task<bool> OnWalletImport(WalletInput input)
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return false;
            }

            stateManager.DB.Store("SECRET_WALLET", input.WalletSecretKey);
            await ProcessService.ExecuteCommand("docker", $"exec octez-public-node-rolling octez-client import secret key WalletAddress unencrypted:{input.WalletSecretKey}");
            return true;
        }

    }

    //todo: refactor after
    public class DelegatingInput
    {
        public string Address { get; set; }
    }
}
