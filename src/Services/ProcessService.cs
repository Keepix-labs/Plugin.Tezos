using Docker.DotNet.Models;
using Docker.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Plugin.Tezos.src.Services
{
    public class ProcessService
    {
        public static async Task<string> ExecuteCommand(string command, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string result = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();
                    LoggerService.Log(result);
                    return result;
                }
            }
            catch(Exception ex)
            {
                LoggerService.Log(ex.Message, "error");
                throw ex;
            }

        }
    }
}
