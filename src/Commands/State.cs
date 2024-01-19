using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Keepix.PluginSystem;
using Newtonsoft.Json;
using Plugin.Tezos.src.Services;
using System.Text.RegularExpressions;
using System.Diagnostics;

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
            var isContainerRunning = await SetupService.IsContainnerRunning();
            if (!isDockerRunning || !isContainerRunning)
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
            await SetupService.initConfig();
            var wallet = await FetchNodeWallet();
            if (string.IsNullOrEmpty(wallet))
            {
                LoggerService.Log("You have no wallet loaded yet in your node, please use wallet-import function accordly.");
                return JsonConvert.SerializeObject(new
                {
                    Exists = false,
                });
            }
            stateManager.DB.Store("WalletAddress", wallet);
            return JsonConvert.SerializeObject(new
            {
                Exists = true,
                Wallet = wallet
            });
        }
        [KeepixPluginFn("init-state")]
        public static async void OnInitState()
        {
            stateManager = PluginStateManager.GetStateManager();
            stateManager.DB.Store<int>("countSyncState", 0);
            stateManager.DB.Store<DateTime>("lastSyncTimeStamp", default(DateTime));

        }


        [KeepixPluginFn("sync-state")]
        public static async Task<string> OnSyncState()
        {
            var allRulesPassed = await SetupService.ApplyRules(SetupService.IsDockerRunning, SetupService.IsContainnerRunning);
            if (!allRulesPassed)
            {
                return null;
            }

            try
            {
                await SetupService.initConfig();
                var output = await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling octez-client bootstrapped", async (process) =>
                {
                    string lastLine = await process.StandardOutput.ReadLineAsync();
                    if (lastLine.Contains("Node is bootstrapped")) return lastLine;
                    try
                    {
                        lastLine = await process.StandardOutput.ReadLineAsync();
                        lastLine = await process.StandardOutput.ReadLineAsync();
                        return lastLine;
                    }
                    catch
                    {
                        return lastLine;

                    }

                });

                if (output.Contains("Node is bootstrapped"))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSynced = true,
                        ExecutionSyncProgress = 100,
                        ExecutionSyncProgressStepDescription = "",
                    });
                }

                if (output.Contains("Waiting for the node to be bootstrapped"))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSynced = false,
                        ExecutionSyncProgress = 0,
                        ExecutionSyncProgressStepDescription = "Initial sync...",
                    });
                }

                var (currentTimestamp, validation) = ParseTimestamps(output);
                stateManager = PluginStateManager.GetStateManager();

                var lastTimestamp = stateManager.DB.Retrieve<DateTime>("lastSyncTimeStamp");
                var maxCallSyncState = 12;
                var countSyncState = stateManager.DB.Retrieve<int>("countSyncState");
                if (lastTimestamp == default(DateTime) || countSyncState < maxCallSyncState)
                {
                    if (stateManager.DB.Retrieve<int>("countSyncState") == 0)
                        stateManager.DB.Store("lastSyncTimeStamp", currentTimestamp);

                    stateManager.DB.Store("countSyncState", ++countSyncState);
                    return JsonConvert.SerializeObject(new
                    {
                        IsSynced = false,
                        ExecutionSyncProgress = 0,
                        ExecutionSyncProgressStepDescription = "Initial sync...",
                    });
                }


                if (stateManager.DB.Retrieve<int>("countSyncState") == maxCallSyncState)
                {
                    int progressInSeconds = (int)(currentTimestamp - lastTimestamp).TotalSeconds;
                    stateManager.DB.Store<int>("rateOfProgressInSeconds", progressInSeconds);
                    stateManager.DB.Store<DateTime>("firstTimestamp", currentTimestamp);
                    stateManager.DB.Store("countSyncState", ++countSyncState);
                }

                double totalSecondsToValidation = (validation - currentTimestamp).TotalSeconds;
                double ExecutionTimeEstimated = totalSecondsToValidation / stateManager.DB.Retrieve<int>("rateOfProgressInSeconds");


                TimeSpan totalDuration = validation - stateManager.DB.Retrieve<DateTime>("firstTimestamp");
                TimeSpan progressDuration = currentTimestamp - stateManager.DB.Retrieve<DateTime>("firstTimestamp");
                double progressPercentage = Math.Round((double)(progressDuration.TotalSeconds / totalDuration.TotalSeconds) * 100, 1);

                return JsonConvert.SerializeObject(new
                {
                    IsSynced = false,
                    ExecutionSyncProgress = progressPercentage,
                    ExecutionTimeEstimated,
                });
            }
            catch (Exception ex)
            {
                LoggerService.Log($"Error on sync state : ${ex.ToString()}");
                return JsonConvert.SerializeObject(new
                {
                    IsSynced = false,
                    ExecutionSyncProgress = 0,
                    ExecutionSyncProgressStepDescription = "Initial sync...",
                });
            }
        }
        public static (DateTime, DateTime) ParseTimestamps(string output)
        {
            var regex = new Regex(@"timestamp: ([\d-:.TZ]+), validation: ([\d-:.TZ]+)");
            var match = regex.Match(output);

            if (match.Success)
            {
                DateTime timestamp = DateTime.Parse(match.Groups[1].Value);
                DateTime validation = DateTime.Parse(match.Groups[2].Value);

                return (timestamp, validation);
            }
            else
            {
                throw new FormatException("Le format de la chaîne de caractères n'est pas valide : " + output);
            }
        }


        public static async Task<string> FetchNodeWallet()
        {
            var result = await ProcessService.ExecuteCommand("docker", "exec octez-public-node-rolling octez-client list known addresses");
            if (!result.Contains("WalletAddress"))
            {
                return string.Empty;
            }

            return result?.Split(':')[1].Split(' ')[1];
        }

    }
}
