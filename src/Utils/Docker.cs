using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.src.Utils
{
    public class Docker
    {
        public static async Task<bool> ExecuteComposeAsync(string command)
        {
            var processInfo = new ProcessStartInfo("docker-compose", command)
            {
                WorkingDirectory = "/path/to/your/docker-compose/directory",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };

            try
            {
                bool processStarted = process.Start();

                if (!processStarted)
                {
                    return false;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
