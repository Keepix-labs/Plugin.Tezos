using Keepix.PluginSystem;
using Plugin.Tezos.src.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.src.Commands
{
    public class Informations
    {
        [KeepixPluginFn("balance")]
        public static async Task<string> GetBalance(string accountAddress)
        {
            var tezosService = new TezosService();

            try
            {
                string balance = await tezosService.GetBalanceAsync(accountAddress);
                return balance;
            }
            catch (Exception ex)
            {
                // logs
                Console.WriteLine($"Error: {ex.Message}");
                return "0";
            }
        }
    }
}
