using Plugin.Tezos.src.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.src.Services
{
    public class SetupService
    {
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

        public static async Task<bool> IsCliInstalled()
        {
            try
            {
                var res = await ProcessService.ExecuteCommand("octez-client", "--version");
                return res != string.Empty;
            }
            catch
            {
                LoggerService.Log("octez-client is not installed on your device, please install it");
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


        public static async Task<bool> ApplyRules(params Func<Task<bool>>[] ruleFunctions)
        {
            var tasks = ruleFunctions.Select(rule => rule()).ToArray();
            await Task.WhenAll(tasks);
            return tasks.All(task => task.Result);
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
    }
}
