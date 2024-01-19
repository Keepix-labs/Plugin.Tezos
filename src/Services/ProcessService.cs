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
        public static async Task<string> ExecuteCommand(string command, string arguments, Func<Process, Task<string>> execute = null, Dictionary<string, string> envVars = null)
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
                startInfo.RedirectStandardError = true;

                if (envVars != null)
                {
                    foreach (var envVar in envVars)
                    {
                        startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                    }
                }

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    if (execute == null)
                    {
                        string result = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        Console.WriteLine(error);
                        process.WaitForExit();
                        return result;
                    }
                    else
                    {
                        return await execute(process);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log(ex.Message, "error");
                throw ex;
            }

        }
    }
}
