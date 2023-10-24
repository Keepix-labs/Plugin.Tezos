using Keepix.PluginSystem;
using Plugin.Tezos.src.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Tezos.Commands
{
    public class Setup
    {
        [KeepixPluginFn("pre-install")]
        public static async Task<bool> OnPreInstall()
        {
            return true;
        }

        [KeepixPluginFn("install")]
        public static async Task<bool> OnInstall()
        {
            return await Docker.ExecuteComposeAsync("up -d ");
        }
    }
}
